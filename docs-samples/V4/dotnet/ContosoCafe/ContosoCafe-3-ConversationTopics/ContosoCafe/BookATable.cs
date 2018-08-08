namespace ContosoCafe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Bot.Schema;
    using Microsoft.Recognizers.Text;
    using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

    /// <summary>
    /// Defines the dialog for booking a table.
    /// </summary>
    public class BookATable : DialogSet
    {
        /// <summary>
        /// The names of the inputs and prompts in this dialog.
        /// </summary>
        /// <remarks>We'll store the information gathered using these same names.</remarks>
        public struct Keys
        {
            /// <summary>Key to use for LUIS entities as input.</summary>
            public const string LuisArgs = "LuisEntities";

            /// <summary>Key to use for location.</summary>
            public const string Location = "location";

            /// <summary>Key to use for reservation date.</summary>
            public const string DateTime = "dateTime";

            /// <summary>Key to use for party size.</summary>
            public const string Guests = "numberOfGuests";

            /// <summary>Key to use for reservation name.</summary>
            public const string Name = "reservationName";

            /// <summary>Key to use for the confirm prompt.</summary>
            public const string Confirm = "confirmation";
        }

        /// <summary>
        /// Gets the list of store locations.
        /// </summary>
        public static IReadOnlyList<string> Locations { get; } =
            new List<string> { "Bellevue", "Redmond", "Renton", "Seattle" };

        /// <summary>
        /// The validatior to use with the reservation date and time prompt.
        /// </summary>
        /// <param name="context">The current turn context.</param>
        /// <param name="toValidate">The input to be validated.</param>
        /// <returns>An updated <paramref name="toValidate"/> value that sets the object's
        /// Prompt status to indicate whether the value validates.</returns>
        /// <remarks>Valid dates are evenings within the next 2 weeks.</remarks>
        private static async Task DateTimeValidator(ITurnContext context, DateTimeResult toValidate)
        {
            if (toValidate.Resolution.Count is 0)
            {
                toValidate.Status = PromptStatus.NotRecognized;
                return;
            }

            // Find any matches within dates from this week or next (not in the past), and evenings only.
            var constraints = new[]
            {
                TimexCreator.NextWeeksFromToday(2),
                TimexCreator.Evening
            };
            List<TimexProperty> resolutions = null;
            var candidates = toValidate.Resolution.Select(res => res.Timex).ToList();
            try
            {
                resolutions = TimexRangeResolver.Evaluate(candidates, constraints);
            }
            catch (Exception ex)
            {
                await context.TraceActivityAsync($"{ex.GetType().Name} in date time validator", ex);
                toValidate.Status = PromptStatus.NotRecognized;
                return;
            }

            if (resolutions.Count is 0)
            {
                toValidate.Resolution.Clear();
                toValidate.Status = PromptStatus.OutOfRange;
                return;
            }

            // Use the first recognized value for the reservation time.
            var timex = resolutions[0];
            DateTimeResult.DateTimeResolution resolution = new DateTimeResult.DateTimeResolution
            {
                Start = timex.ToNaturalLanguage(DateTime.Now),
                End = timex.ToNaturalLanguage(DateTime.Now),
                Value = timex.ToNaturalLanguage(DateTime.Now),
                Timex = timex.TimexValue
            };
            toValidate.Resolution.Clear();
            toValidate.Resolution.Add(resolution);
            toValidate.Status = PromptStatus.Recognized;

            return;
        }

        /// <summary>
        /// The validatior to use with the number of guests prompt.
        /// </summary>
        /// <param name="context">The current turn context.</param>
        /// <param name="toValidate">The input to be validated.</param>
        /// <returns>An updated <paramref name="toValidate"/> value that sets the object's
        /// Prompt status to indicate whether the value validates.</returns>
        /// <remarks>Valid party sizes are 1 through 12.</remarks>
        private static Task GuestsValidator(ITurnContext context, NumberResult<int> toValidate)
        {
            if (toValidate.Value < 1)
            {
                toValidate.Status = PromptStatus.TooSmall;
            }
            else if (toValidate.Value > 12)
            {
                toValidate.Status = PromptStatus.TooBig;
            }
            else
            {
                toValidate.Status = PromptStatus.Recognized;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BookATable"/> class.
        /// </summary>
        public BookATable()
        {
            // Add the prompts we'll be using in our dialog.
            this.Add(Keys.Location, new ChoicePrompt(Culture.English));
            this.Add(Keys.DateTime, new DateTimePrompt(Culture.English, DateTimeValidator));
            this.Add(Keys.Guests, new NumberPrompt<int>(Culture.English, GuestsValidator));
            this.Add(Keys.Name, new TextPrompt());
            this.Add(Keys.Confirm, new ConfirmPrompt(Culture.English));

            // Define and add the waterfall steps for our dialog.
            this.Add(nameof(BookATable), new WaterfallStep[]
            {
                // Begin booking a table.
                async (dc, args, next) =>
                {
                    // Initialize state.
                    dc.ActiveDialog.State = new Dictionary<string, object>();

                    // Prompt for location.
                    var retryPrompt = MessageFactory.SuggestedActions(
                        Locations.ToList(), text: "Please select one of our locations.") as Activity;
                    await dc.PromptAsync(
                        Keys.Location,
                        "Did you have a location in mind?",
                        new ChoicePromptOptions
                        {
                            RetryPromptActivity = retryPrompt,
                            Choices = ChoiceFactory.ToChoices(new List<string>(Locations)),
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state with the prompt result.
                    var answer = args["Value"] as FoundChoice;
                    dc.ActiveDialog.State[Keys.Location] = answer.Value;

                    // Prompt for the reservation date and time.
                    await dc.PromptAsync(
                        Keys.DateTime,
                        "When will the reservation be for?",
                        new PromptOptions
                        {
                            RetryPromptString = "Please enter a date and time for the reservation.\n\n" +
                            "We take reservations within two weeks of today, and evenings only.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state from the prompt result.
                    // The prompt can return multiple interpretations of the time entered.
                    // For now, just use the first one.
                    var answer = args["Resolution"] as List<DateTimeResult.DateTimeResolution>;
                    dc.ActiveDialog.State[Keys.DateTime] = answer[0].Value;

                    // Prompt for the party size.
                    await dc.PromptAsync(
                        Keys.Guests,
                        "How many guests?",
                        new PromptOptions
                        {
                            RetryPromptString = "Please enter the number of people that the reservation is for.\n\n" +
                            "We can take reservations for parties of up to 12.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state from the prompt result.
                    var answer = (int)args["Value"];
                    dc.ActiveDialog.State[Keys.Guests] = answer;

                    // Prompt for the reservtion name.
                    await dc.PromptAsync(
                        Keys.Name,
                        "What name should I book the table under?",
                        new PromptOptions
                        {
                            RetryPromptString = "Please enter a name for the reservation.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state from the prompt result.
                    var answer = args["Value"] as string;
                    dc.ActiveDialog.State[Keys.Name] = answer;

                    // Confirm the reservation.
                    await dc.PromptAsync(
                        Keys.Confirm,
                        $"Ok. Should I go ahead and book a table " +
                            $"for {dc.ActiveDialog.State[Keys.Guests]} " +
                            $"at {dc.ActiveDialog.State[Keys.Location]} " +
                            $"for {dc.ActiveDialog.State[Keys.DateTime]} " +
                            $"for {dc.ActiveDialog.State[Keys.Name]}?",
                        new PromptOptions
                        {
                            RetryPromptString = "I'm sorry, should I make the reservation for you? " +
                            "Please enter `yes` or `no`.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Book the table or cancel the reservation.
                    var confirmed = (bool)args["Confirmation"];
                    if (confirmed)
                    {
                        // Send a confirmation message: the typing activity indicates to the user that
                        // the bot is working on something, the delay simulates a process (filing the reservation)
                        // that takes some time, and the message simulates a confirmation message generated
                        // by the process.
                        var typing = Activity.CreateTypingActivity();
                        var delay = new Activity { Type = "delay", Value = 3000 };
                        await dc.Context.SendActivitiesAsync(
                            new IActivity[]
                            {
                                typing, delay,
                                MessageFactory.Text("Your table is booked. Reference number: #K89HG38SZ"),
                            });

                        // As part of the process to fill the reservation, the relevant data would be persisted
                        // in the reservation system.
                    }
                    else
                    {
                        // Cancel the reservation.
                        await dc.Context.SendActivityAsync("Okay. We have canceled the reservation.");
                    }

                    await dc.EndAsync();
                },
            });
        }
    }
}