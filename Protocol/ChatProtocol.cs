using Newtonsoft.Json;

namespace Protocol {
    public class ChatProtcol {
        private const string NewMember = "newMember";
        private const string Disconnect = "disconnect";

        [JsonConstructor]
        protected ChatProtcol(string action, string result) {
            Action = action;
            Result = result;
        }

        [JsonProperty("action")]
        private string Action { get; }

        [JsonProperty("result")]
        private string Result { get; }

        //This could be one method using enum.
        public static Message Message(string message, string sender)
            => new Message(sender, "message", message);

        public static Message ParseToMessage(string json) => JsonConvert.DeserializeObject<Message>(json);

        public static ChatProtcol ParseToAction(string json) => JsonConvert.DeserializeObject<ChatProtcol>(json);

        //This could be one method using enum.
        public static ChatProtcol MemberJoins(string userName) => new ChatProtcol(NewMember, userName);

        //This could be one method using enum.
        public static ChatProtcol MemberDisconnects(string userName) => new ChatProtcol(Disconnect, userName);

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}