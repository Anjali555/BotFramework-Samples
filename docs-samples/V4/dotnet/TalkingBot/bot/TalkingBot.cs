using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Threading.Tasks;

namespace bot
{
    public class TalkingBot : IBot
    {
        public async Task OnTurn(ITurnContext context)
        {
            if (context.Activity.Type is ActivityTypes.Message)
            {
                await context.SendActivity(
                    "This is the text to be displayed.",
                    "This is the text to be spoken.");
            }
        }
    }
}
