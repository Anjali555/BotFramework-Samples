using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace ReferenceBot
{
    public class EchoBot : IBot
    {
        /// <summary>
        /// Gets the state property accessors for the bot.
        /// </summary>
        private StateAccessors Accessors { get; }

        public EchoBot(StateAccessors accessors)
        {
            Accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
        }

        /// <summary>
        /// Every Conversation turn for our EchoBot will call this method. In here
        /// the bot checks the Activty type to verify it's a message, bumps the 
        /// turn conversation 'Turn' count, and then echoes the users typing
        /// back to them. 
        /// </summary>
        /// <param name="turnContext">Turn scoped context containing all the data needed
        /// for processing this conversation turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // This bot is only handling Messages
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Get the conversation state from the turn context.
                var state = await Accessors.PropertyAccessor.GetAsync(turnContext, () => new EchoState());

                // Bump the turn count. 
                state.TurnCount++;

                // Echo back to the user whatever they typed.
                await turnContext.SendActivityAsync($"Turn {state.TurnCount}: You sent '{turnContext.Activity.Text}'");
            }
        }
    }
}
