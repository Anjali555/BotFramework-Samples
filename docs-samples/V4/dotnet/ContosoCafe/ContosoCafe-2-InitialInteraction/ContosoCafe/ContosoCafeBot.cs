using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoCafe
{
    public class ContosoCafeBot : IBot
    {
        /// <summary>
        /// Handles incoming activities from a user's channel.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        public async Task OnTurn(ITurnContext context)
        {
            // Choose what to do based on the incoming activity type.
            if (context.Activity.Type is ActivityTypes.Message)
            {
                // Capture any input text, and stub in responses for some of the input we want to handle.
                var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();
                switch (text)
                {
                    case "who are you":
                    case "who are you?":
                        // Stub for answering questions.
                        await context.SendActivity("Hi, I'm the Contoso Cafe bot.");
                        break;

                    case "book table":
                    case "book a table":
                        // Stub for booking a table.
                        var typing = Activity.CreateTypingActivity();
                        var delay = new Activity { Type = "delay", Value = 3000 };
                        await context.SendActivities(
                            new IActivity[]
                            {
                                typing, delay,
                                MessageFactory.Text("Your table is booked. Reference number: #K89HG38SZ")
                            });
                        break;

                    case "help":
                        // Provide some guidance to the user.
                        await context.SendActivity("Type `book a table` to make a reservation.");
                        break;

                    default:
                        // Provide a default response for anything we didn't understand.
                        await context.SendActivity("I'm sorry; I do not understand.");
                        goto case "help";
                }
            }
            else
            {
                await HandleSystemActivity(context);
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
                case ActivityTypes.ConversationUpdate:

                    // If a user is being added to the conversation, send them an initial greeting.
                    var update = context.Activity.AsConversationUpdateActivity();
                    if (update.MembersAdded.Any(member => member.Id != update.Recipient.Id))
                    {
                        await context.SendActivities(
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
