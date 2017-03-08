namespace Protocol {
    public class ChatMessage {
        public ChatMessage(string userName, string message) {
            UserName = userName;
            Message = message;
        }

        public string UserName { get; }
        public string Message { get; }
    }
}