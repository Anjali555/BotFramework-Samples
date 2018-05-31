using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;

namespace ContosoCafe
{
    public class ContosoCafeBot : IBot
    {
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
            // Capture any input text.
            var text = context.Activity.AsMessageActivity()?.Text?.Trim();
            if (text != null)
            {
                // Echo it back to the user for now.
                await context.SendActivity($"You said, '{text}'.");
            }
            else
            {
                // Always respond to a message.
                await context.SendActivity("I'm sorry; I do not understand.");
            }
        }
    }    
}
