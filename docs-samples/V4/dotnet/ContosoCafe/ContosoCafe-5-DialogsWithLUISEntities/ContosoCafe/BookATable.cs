// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

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
        private static Task DateTimeValidator(ITurnContext context, DateTimeResult toValidate)
        {
            if (toValidate.Resolution.Count is 0)
            {
                toValidate.Status = PromptStatus.NotRecognized;
                return Task.CompletedTask;
            }

            var candidates = toValidate.Resolution.Select(res => res.Timex).ToList();

            // Find any matches within dates from this week or next (not in the past), and evenings only.
            var resolution = ResolveTime(candidates);
            if (resolution != null)
            {
                toValidate.Resolution.Clear();
                toValidate.Resolution.Add(resolution);
                toValidate.Status = PromptStatus.Recognized;
            }
            else
            {
                toValidate.Status = PromptStatus.NotRecognized;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Compares a set of candidate date time strings against our validation constraints, and
        /// returns a value that matches; or null, if no candidates meet the constraints.
        /// </summary>
        /// <param name="candidates">The candidate strings.</param>
        /// <returns>A value that matches; or null, if no candidates meet the constraints.</returns>
        /// <remarks>Valid dates are evenings within the next 2 weeks.</remarks>
        private static DateTimeResult.DateTimeResolution ResolveTime(IEnumerable<string> candidates)
        {
            // Find any matches within dates from this week or next (not in the past), and evenings only.
            var constraints = new[]
            {
                TimexCreator.NextWeeksFromToday(2),
                TimexCreator.Evening,
            };
            List<TimexProperty> resolutions = null;
            try
            {
                resolutions = TimexRangeResolver.Evaluate(candidates, constraints);
            }
            catch
            {
                return null;
            }

            if (resolutions.Count is 0)
            {
                return null;
            }

            // Use the first recognized value for the reservation time.
            var timex = resolutions[0];
            return new DateTimeResult.DateTimeResolution
            {
                Start = timex.ToNaturalLanguage(DateTime.Now),
                End = timex.ToNaturalLanguage(DateTime.Now),
                Value = timex.ToNaturalLanguage(DateTime.Now),
                Timex = timex.TimexValue,
            };
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
            this.Add(nameof(BookATable), BookATableSteps);
        }

        private static WaterfallStep[] BookATableSteps { get; } =
            new WaterfallStep[]
            {
                // Begin booking a table.
                async (dc, args, next) =>
                {
                    // Initialize state.
                    if (args != null
                        && args.ContainsKey(Keys.LuisArgs)
                        && args[Keys.LuisArgs] is CafeLuisModel._Entities entities)
                    {
                        // Add any LUIS entities to the active dialog state.
                        dc.ActiveDialog.State = await AddLuisArguments(entities, dc.Context);
                    }
                    else
                    {
                        // Begin without any information collected.
                        dc.ActiveDialog.State = new Dictionary<string, object>();
                    }

                    // If we already have a valid location, use it.
                    if (dc.ActiveDialog.State.ContainsKey(Keys.Location)
                        && ValidateLocation(dc.ActiveDialog.State[Keys.Location], out string location))
                    {
                        dc.ActiveDialog.State[Keys.Location] = location;
                        await next();
                    }
                    else
                    {
                        // Otherwise, prompt for location.
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
                    }
                },
                async (dc, args, next) =>
                {
                    // If we prompted for a value in the previous turn.
                    if (args != null && args.ContainsKey("Value") && args["Value"] is FoundChoice answer)
                    {
                        // Update state with the prompt result.
                        dc.ActiveDialog.State[Keys.Location] = answer.Value;
                    }

                    // If we already have a valid reservation date, use it.
                    if (dc.ActiveDialog.State.ContainsKey(Keys.DateTime)
                        && ValidateDate(dc.ActiveDialog.State[Keys.DateTime], out string date))
                    {
                        dc.ActiveDialog.State[Keys.DateTime] = date;
                        await next();
                    }
                    else
                    {
                        // Otherwise, prompt for the reservation date and time.
                        await dc.PromptAsync(
                            Keys.DateTime,
                            "When will the reservation be for?",
                            new PromptOptions
                            {
                                RetryPromptString = "Please enter a date and time for the reservation.\n\n" +
                                "We take reservations within two weeks of today, and evenings only.",
                            });
                    }
                },
                async (dc, args, next) =>
                {
                    // If we prompted for a value in the previous turn.
                    if (args != null && args.ContainsKey("Resolution")
                        && args["Resolution"] is List<DateTimeResult.DateTimeResolution> resolutions)
                    {
                        // Update state with the prompt result.
                        // The prompt can return multiple interpretations of the time entered.
                        // For now, just use the first one.
                        dc.ActiveDialog.State[Keys.DateTime] = resolutions[0].Value;
                    }

                    // If we already have a valid party size, use it.
                    if (dc.ActiveDialog.State.ContainsKey(Keys.Guests)
                        && ValidatePartySize(dc.ActiveDialog.State[Keys.Guests], out int guests))
                    {
                        dc.ActiveDialog.State[Keys.Guests] = guests;
                        await next();
                    }
                    else
                    {
                        // Otherwise, prompt for the party size.
                        await dc.PromptAsync(
                            Keys.Guests,
                            "How many guests?",
                            new PromptOptions
                            {
                                RetryPromptString = "Please enter the number of people that the reservation is for.\n\n" +
                                "We can take reservations for parties of up to 12.",
                            });
                    }
                },
                async (dc, args, next) =>
                {
                    // If we prompted for a value in the previous turn.
                    if (args != null && args.ContainsKey("Value") && args["Value"] is int guests)
                    {
                        // Update state from the prompt result.
                        dc.ActiveDialog.State[Keys.Guests] = guests;
                    }

                    // If we already have a reservation name, use it.
                    if (dc.ActiveDialog.State.ContainsKey(Keys.Name)
                        && ValidateReservationName(dc.ActiveDialog.State[Keys.Name], out string name))
                    {
                        dc.ActiveDialog.State[Keys.Name] = name;
                        await next();
                    }
                    else
                    {
                        // Otherwise, prompt for the reservtion name.
                        await dc.PromptAsync(
                            Keys.Name,
                            "What name should I book the table under?",
                            new PromptOptions
                            {
                                RetryPromptString = "Please enter a name for the reservation.",
                            });
                    }
                },
                async (dc, args, next) =>
                {
                    // If we prompted for a value in the previous turn.
                    if (args != null && args.ContainsKey("Value") && args["Value"] is string name)
                    {
                        // Update state from the prompt result.
                        dc.ActiveDialog.State[Keys.Name] = name;
                    }

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
            };

        /// <summary>
        /// Create a dictionary of potentially valid input from LUIS.
        /// </summary>
        /// <param name="entities">The LUIS entities from the input arguments.</param>
        /// <param name="context">The current turn context.</param>
        /// <returns>A dictionary of the potentially valid entities.</returns>
        private static async Task<Dictionary<string, object>> AddLuisArguments(CafeLuisModel._Entities entities, ITurnContext context)
        {
            var result = new Dictionary<string, object>();
            if (entities is null)
            {
                return result;
            }

            // Check location.
            if (Locations.Contains(entities.location?[0]?[0], StringComparer.InvariantCultureIgnoreCase))
            {
                var name = entities.location[0][0];
                await context.TraceActivityAsync($"Found '{name}' for location.");
                result[Keys.Location] = name;
            }

            // Check time.
            if (entities.datetime?.FirstOrDefault()?.Expressions.Any() is true)
            {
                var dates = entities.datetime[0].Expressions;
                await context.TraceActivityAsync($"Found {{{string.Join(',', dates)}}} for reservation date.");
                result[Keys.DateTime] = dates.ToList();
            }

            // Check number of guests.
            if (entities.partySize?.Any() is true)
            {
                var guests = new List<int>();
                foreach (var entity in entities.partySize)
                {
                    if (int.TryParse(entity, out int value))
                    {
                        guests.Add(value);
                    }
                }

                if (guests.Any())
                {
                    await context.TraceActivityAsync($"Found {{{string.Join(',', guests)}}} for number of guests.");
                    result[Keys.Guests] = guests;
                }
            }

            // Check reservation name.
            if (entities.reservationName?.Any() is true)
            {
                var names = entities.reservationName.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim());
                if (names.Any())
                {
                    await context.TraceActivityAsync($"Found {{{string.Join(',', names)}}} for reservation name.");
                    result[Keys.Name] = names.ToList();
                }
            }

            return result;
        }

        private static bool ValidateLocation(object value, out string location)
        {
            if (value is string name && Locations.Contains(name, StringComparer.InvariantCultureIgnoreCase))
            {
                location = name;
                return true;
            }

            location = null;
            return false;
        }

        private static bool ValidateDate(object value, out string date)
        {
            if (value is IEnumerable<string> dates)
            {
                var resolution = ResolveTime(dates);
                if (resolution != null)
                {
                    date = resolution.Value;
                    return true;
                }
            }

            date = null;
            return false;
        }

        private static bool ValidatePartySize(object value, out int guests)
        {
            if (value is IEnumerable<int> list)
            {
                guests = list.FirstOrDefault(x => x >= 1 && x <= 12);
                return guests > 0;
            }

            guests = 0;
            return false;
        }

        private static bool ValidateReservationName(object value, out string name)
        {
            if (value is IEnumerable<string> list && list.Any())
            {
                name = list.First();
                return true;
            }

            name = null;
            return false;
        }
    }
}