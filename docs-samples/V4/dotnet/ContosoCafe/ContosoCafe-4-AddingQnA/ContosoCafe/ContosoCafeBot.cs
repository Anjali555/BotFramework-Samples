using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoCafe
{
    public class ContosoCafeBot : IBot
    {
        private static IEnumerable<string> CancelPhrases { get; } = new HashSet<string>
        {
            "cancel", "stop", "start over"
        };

        public async Task OnTurn(ITurnContext context)
        {
            // Choose what to do based on the incoming activity type.
            switch (context.Activity.Type)
            {
                case ActivityTypes.ConversationUpdate:

                    // If a user is being added to the conversation, send the greeting message.
                    var update = context.Activity.AsConversationUpdateActivity();
                    if (update.MembersAdded.Any(member => member.Id != update.Recipient.Id))
                    {
                        await context.SendActivities(
                            new IActivity[]
                            {
                                MessageFactory.Text("Hello, I'm the ContosoCafe bot."),
                                MessageFactory.Text("How can I help you?")
                            });
                    }
                    break;

                case ActivityTypes.Message:

                    // Handle any message the user sends to the bot.
                    await ProcessMessage(context);
                    break;
            }
        }

        /// <summary>
        /// Handle any message the user sends to the bot.
        /// </summary>
        /// <param name="context">The bot's current turn context.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private static async Task ProcessMessage(ITurnContext context)
        {
            // Get the bot's conversation and dialog state.
            var state = ConversationState<ConversationData>.Get(context);
            var dc = BookATable.Singleton.Value.CreateContext(context, state.DialogState);

            // Capture any input text, and check for "cancel" before continuing any active dialog.
            var text = context.Activity.AsMessageActivity()?.Text?.Trim().ToLowerInvariant();

            if (CancelPhrases.Contains(text))
            {
                // If there's no active dialog, this is a no-op.
                dc.EndAll();

                // Send a cancellation completed message and finish turn.
                await context.SendActivity("Sure.. Let's start over");
                return;
            }

            // If there's no active dialog, this is a no-op.
            await dc.Continue();

            // If there were an active dialog, then it should have replied to the user.
            if (!context.Responded)
            {
                switch (text)
                {
                    case "help":

                        // Provide some guidance to the user.
                        await context.SendActivity("Type `book a table` to make a reservation.");
                        break;

                    case "who are you":
                    case "who are you?":

                        // Answer the "who are you" question.
                        await context.SendActivity("Hi, I'm the Contoso Cafe bot.");
                        break;

                    case "book":
                    case "table":
                    case "book table":
                    case "book a table":

                        // Start the "book a table" dialog.
                        await dc.Begin(nameof(BookATable), state.DialogState);
                        break;

                    default:

                        // Provide a default response for anything we didn't understand.
                        await context.SendActivity("I'm sorry; I do not understand.");
                        break;
                }
            }
        }
    }
}
