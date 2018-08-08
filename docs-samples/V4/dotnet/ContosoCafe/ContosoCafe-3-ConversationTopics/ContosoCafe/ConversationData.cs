// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

namespace ContosoCafe
{
    using System.Collections.Generic;

    /// <summary>
    /// Class for storing conversation state.
    /// </summary>
    public class ConversationData
    {
        /// <summary>
        /// Gets or sets dialog state data for the book a table dialog.
        /// </summary>
        public Dictionary<string, object> DialogState { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets information about the user's reservations.
        /// </summary>
        public object ReservationData { get; set; }
    }
}