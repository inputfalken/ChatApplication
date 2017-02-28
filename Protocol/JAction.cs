using Newtonsoft.Json;

namespace Protocol {
    /// <summary>
    /// JSON Action: Jaction
    /// </summary>
    public class JAction {
        private const string NewMemberOperation = "newMember";
        private const string DisconnectOperation = "disconnect";
        private const string MessageOperation = "message";

        [JsonConstructor]
        protected JAction(string action, string result) {
            Action = action;
            Result = result;
        }

        [JsonProperty("action")]
        private string Action { get; }

        [JsonProperty("result")]
        private string Result { get; }

        //This could be one method using enum.
        public static Message Message(string message, string sender)
            => new Message(sender, MessageOperation, message);

        public static Message ParseToMessage(string json) => JsonConvert.DeserializeObject<Message>(json);

        public static JAction ParseToOperation(string json) => JsonConvert.DeserializeObject<JAction>(json);

        //This could be one method using enum.
        public static JAction MemberJoins(string userName) => new JAction(NewMemberOperation, userName);

        //This could be one method using enum.
        public static JAction MemberDisconnects(string userName) => new JAction(DisconnectOperation, userName);

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}