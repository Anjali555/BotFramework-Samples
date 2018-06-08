using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoCafe
{
    public class BookATable : DialogSet
    {
        /// <summary>
        /// The names of the prompts in this dialog.
        /// </summary>
        /// <remarks>We'll store the information gathered using these same names.</remarks>
        public struct Keys
        {
            public const string Location = "location";
            public const string DateTime = "dateTime";
            public const string Guests = "numberOfGuests";
            public const string Name = "reservationName";
            public const string Confirm = "confirmation";
        }

        /// <summary>
        /// The list of store locations.
        /// </summary>
        public static IReadOnlyList<string> Locations { get; } =
            new List<string> { "Bellevue", "Redmond", "Renton", "Seattle" };

        public static Lazy<BookATable> Singleton { get; } = new Lazy<BookATable>(new BookATable());

        private BookATable()
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
                async (dc, args, next) =>
                {
                    // Begin booking a table.

                    // Initialize state.
                    dc.ActiveDialog.State = new Dictionary<string,object>();

                    // Query for location.
                    var retryPrompt = MessageFactory.SuggestedActions(
                        Locations.ToList(), text: "Please select one of our locations.") as Activity;
                    await dc.Prompt(Keys.Location,
                        "Did you have a location in mind?", new ChoicePromptOptions
                        {
                            RetryPromptActivity = retryPrompt,
                            Choices = ChoiceFactory.ToChoices(new List<string>(Locations))
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state with the location.
                    var answer = args["Value"] as FoundChoice;
                    dc.ActiveDialog.State[Keys.Location] = answer.Value;

                    // Query for date and time.
                    await dc.Prompt(Keys.DateTime,
                        "When will the reservation be for?", new PromptOptions
                        {
                            RetryPromptString =
                                "I'm sorry, we only accept reservations for the next two weeks, 4PM-8PM.\n\n" +
                                "Please enter a date and time for the reservation.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state with the date and time.
                    // The prompt can return multiple interpretations of the time entered.
                    // For now, just use the first one.
                    var answer = args["Resolution"] as List<DateTimeResult.DateTimeResolution>;
                    dc.ActiveDialog.State[Keys.DateTime] = answer[0].Start;

                    // Query for the number of guests.
                    await dc.Prompt(Keys.Guests,
                        "How many guests?", new PromptOptions
                        {
                            RetryPromptString = "Please enter the number of people that the reservation is for.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state with the number of guests.
                    var answer = (int)args["Value"];
                    dc.ActiveDialog.State[Keys.Guests] = answer;

                    // Query for a name for the resevation.
                    await dc.Prompt(Keys.Name,
                        "What name should I book the table under?", new PromptOptions
                        {
                            RetryPromptString = "Please enter a name for the reservation.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state with the name for the reservation.
                    var answer = args["Value"] as string;
                    dc.ActiveDialog.State[Keys.Name] = answer;

                    // Confirm the resevation.
                    await dc.Prompt(Keys.Confirm,
                        $"Ok. Should I go ahead and book a table " +
                        $"for {dc.ActiveDialog.State[Keys.Guests]} " +
                        $"at {dc.ActiveDialog.State[Keys.Location]} " +
                        $"for {dc.ActiveDialog.State[Keys.DateTime]} " +
                        $"for {dc.ActiveDialog.State[Keys.Name]}?", new PromptOptions
                        {
                            RetryPromptString = "I'm sorry, should I make the reservation for you? " +
                            "Please enter `yes` or `no`.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Finish up booking a table.
                    var confirmed = (bool)args["Confirmation"];

                    if (confirmed)
                    {
                        // Send a confirmation message.
                        var typing = Activity.CreateTypingActivity();
                        var delay = new Activity { Type = "delay", Value = 3000 };
                        await dc.Context.SendActivities(
                            new IActivity[]
                            {
                                typing, delay,
                                MessageFactory.Text("Your table is booked. Reference number: #K89HG38SZ")
                            });
                    }
                    else
                    {
                        // Decide what to do if they say no at this point.
                        await dc.Context.SendActivity("Okay. We have canceled the reservation.");
                    }
                }
            });
        }

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
                TimexCreator.ThisWeek(),
                TimexCreator.NextWeek(),
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
                await context.TraceActivity($"{nameof(ContosoCafeBot)} Exception", ex);
                toValidate.Status = PromptStatus.NotRecognized;
                return;
            }

            if (resolutions.Count is 0)
            {
                toValidate.Resolution.Clear();
                toValidate.Status = PromptStatus.OutOfRange;
                return;
            }

            // Use the first recognized value for the reservation.
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
    }
}
