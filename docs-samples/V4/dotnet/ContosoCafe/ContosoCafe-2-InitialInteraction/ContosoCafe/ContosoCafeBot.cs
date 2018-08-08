// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

namespace ContosoCafe
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;

    /// <summary>Defines the Contoso Cafe bot.</summary>
    public class ContosoCafeBot : IBot
    {
        /// <summary>
        /// Handles incoming activities from a user's channel.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        public async Task OnTurnAsync(ITurnContext context, CancellationToken token = default(CancellationToken))
        {
            // Handle message and non-message activities differently.
            if (context.Activity.Type != ActivityTypes.Message)
            {
                // Handle any non-message activity.
                await HandleSystemActivity(context);
            }
            else
            {
                // Capture any input text.
                var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();

                // Handle input from the user.
                switch (text)
                {
                    case "who are you":
                    case "who are you?":
                        // Stub for answering questions.
                        await context.SendActivityAsync("Hi, I'm the Contoso Cafe bot.");
                        break;

                    case "book table":
                    case "book a table":
                        // Stub for booking a table.
                        var typing = Activity.CreateTypingActivity();
                        var delay = new Activity { Type = "delay", Value = 3000 };
                        await context.SendActivitiesAsync(
                            new IActivity[]
                            {
                                typing, delay,
                                MessageFactory.Text("Your table is booked. Reference number: #K89HG38SZ")
                            });
                        break;

                    case "help":
                        // Provide some guidance to the user.
                        await context.SendActivityAsync("Type `book a table` to make a reservation.");
                        break;

                    default:
                        // Provide a default response for anything we didn't understand.
                        await context.SendActivityAsync("I'm sorry; I do not understand.");
                        goto case "help";
                }
            }
        }

        /// <summary>
        /// Handle any non-message activity from the channel.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        private async Task HandleSystemActivity(ITurnContext context)
        {
            switch (context.Activity.Type)
            {
                // Not all channels send a ConversationUpdate activity.
                // However, both the Emulator and WebChat do.
                case ActivityTypes.ConversationUpdate:

                    // If a user is being added to the conversation, send them an initial greeting.
                    var update = context.Activity.AsConversationUpdateActivity();
                    if (update.MembersAdded.Any(member => member.Id != update.Recipient.Id))
                    {
                        await context.SendActivitiesAsync(
                            new IActivity[]
                            {
                                MessageFactory.Text("Hello, I'm the Contoso Cafe bot."),
                                MessageFactory.Text("How can I help you? (Type `book a table` to set up a table reservation.)")
                            });
                    }
                    break;
            }
        }
    }
}
