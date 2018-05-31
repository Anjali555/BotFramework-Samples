using System.Collections.Generic;

namespace ContosoCafe
{
    /// <summary>
    /// Class for storing conversation state.
    /// </summary>
    public class ConversationData
    {
        /// <summary>
        /// Property for storing dialog state fot the book a table dialog.
        /// </summary>
        public Dictionary<string, object> DialogState { get; set; } = new Dictionary<string, object>();
    }
}
