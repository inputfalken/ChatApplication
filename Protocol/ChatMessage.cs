﻿namespace Protocol {
    public class ChatMessage {
        public ChatMessage(string userName, string message) {
            UserName = userName;
            Message = message;
        }

        public string UserName { get; }
        public string Message { get; }
    }

    public class PrivateMessage : ChatMessage {
        public PrivateMessage(string userName, string recipent, string message) : base(userName, message) {
            Recipent = recipent;
        }

        public string Recipent { get; }
    }
}