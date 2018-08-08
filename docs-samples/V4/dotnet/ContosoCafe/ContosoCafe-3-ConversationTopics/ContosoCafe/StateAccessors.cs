// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

namespace ContosoCafe
{
    using Microsoft.Bot.Builder;

    /// <summary>
    /// Contains state property accessors for the bot.
    /// </summary>
    public class StateAccessors
    {
        /// <summary>
        /// Gets the identifier for the conversation data state property for the bot.
        /// </summary>
        public static string ConvDataName { get; } = $"{nameof(StateAccessors)}.{nameof(ConversationData)}";

        /// <summary>
        /// Gets or sets the conversation data state property accessor.
        /// </summary>
        public IStatePropertyAccessor<ConversationData> ConvData { get; set; }
    }
}
