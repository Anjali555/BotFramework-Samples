// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace AspNetCore_EchoBot_With_State
{
    public class EchoBot : IBot
    {
        private BotAccessors _accessors;

        public EchoBot(BotAccessors accessors)
        {
            _accessors = accessors;
        }

        /// <summary>
        /// Every Conversation turn for our EchoBot will call this method. In here
        /// the bot checks the Activty type to verify it's a message, bumps the 
        /// turn conversation 'Turn' count, and then echoes the users typing
        /// back to them. 
        /// </summary>
        /// <param name="context">Turn scoped context containing all the data needed
        /// for processing this conversation turn. </param>        
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // This bot is only handling Messages
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var count = await _accessors.TurnCountAccessor.GetAsync(turnContext, () => 0, cancellationToken);

                // Bump the turn count. 
                count++;

                // Echo back to the user whatever they typed.
                await turnContext.SendActivityAsync(
                    $"[{count}]: You said '{turnContext.Activity.Text}'",
                    cancellationToken: cancellationToken);

                await _accessors.TurnCountAccessor.SetAsync(turnContext, count, cancellationToken);
                await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            }
        }
    }    
}
