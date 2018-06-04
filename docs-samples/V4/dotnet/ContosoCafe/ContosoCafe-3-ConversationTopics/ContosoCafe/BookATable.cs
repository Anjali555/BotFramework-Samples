using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using ChoiceFactory = Microsoft.Bot.Builder.Prompts.Choices.ChoiceFactory;
using DateTimeResult = Microsoft.Bot.Builder.Prompts.DateTimeResult;
using FoundChoice = Microsoft.Bot.Builder.Prompts.Choices.FoundChoice;

namespace ContosoCafe
{
    public class BookATable : DialogSet
    {
        /// <summary>
        /// The names of the prompts in this dialog.
        /// </summary>
        /// <remarks>We'll store the information gathered using these same names.</remarks>
        public struct Prompts
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
            this.Add(Prompts.Location, new ChoicePrompt(Culture.English));
            this.Add(Prompts.DateTime, new DateTimePrompt(Culture.English));
            this.Add(Prompts.Guests, new NumberPrompt<int>(Culture.English));
            this.Add(Prompts.Name, new TextPrompt());
            this.Add(Prompts.Confirm, new ConfirmPrompt(Culture.English));

            // Define and add the waterfall steps for our dialog.
            this.Add(nameof(BookATable), new WaterfallStep[]
            {
                // Begin booking a table.
                async (dc, args, next) =>
                {
                    // Initialize state.
                    dc.ActiveDialog.State = new Dictionary<string,object>();

                    // Query for location.
                    var retryPrompt = MessageFactory.SuggestedActions(
                        Locations.ToList(), text: "Please select one of our locations.") as Activity;

                    await dc.Prompt(Prompts.Location,
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
                    dc.ActiveDialog.State[Prompts.Location] = answer.Value;

                    // Query for date and time.
                    await dc.Prompt(Prompts.DateTime,
                        "When will the reservation be for?", new PromptOptions
                        {
                            RetryPromptString = "Please enter a date and time for the reservation.",
                        });
                },
                async (dc, args, next) =>
                {
                    // Update state with the date and time.
                    var answer = args["Resolution"] as List<DateTimeResult.DateTimeResolution>;
                    dc.ActiveDialog.State[Prompts.DateTime] = answer[0].Value;

                    // Query for the number of guests.
                    await dc.Prompt(Prompts.Guests,
                        "How many guests?", new PromptOptions
                        {
                            RetryPromptString = "Please enter the number of people that the reservation is for.",
                        });
                },
                // What name should I book the table under?
                // Ok. Should I go ahead and book a table for 3 at seattle for tomorrow at 3PM for Vishwac?
                // Decide what to do if they say no at this point.
                // Finish up booking a table.
                async (dc, args, next) =>
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
            });
        }
    }
}
