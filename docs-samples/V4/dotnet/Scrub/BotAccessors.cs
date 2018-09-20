namespace AspNetCore_EchoBot_With_State
{
    using Microsoft.Bot.Builder;

    /// <summary>
    /// Class for storing conversation state. 
    /// </summary>
    public class BotAccessors
    {
        public BotAccessors(ConversationState conversationState)
        {
            this.ConversationState = conversationState;
        }

        public IStatePropertyAccessor<int> TurnCountAccessor { get; set; }

        public ConversationState ConversationState { get; }
    }
}
