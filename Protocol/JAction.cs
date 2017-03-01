using Newtonsoft.Json;

namespace Protocol {
    /// <summary>
    /// JSON Action: Jaction
    /// </summary>
    public class JAction {
        private const string NewMemberAction = "newMember";
        private const string DisconnectAction = "disconnect";
        private const string MessageAction = "message";

        [JsonConstructor]
        protected JAction(string action, string result) {
            Action = action;
            Result = result;
        }

        [JsonProperty("action")]
        public string Action { get; }

        [JsonProperty("result")]
        public string Result { get; }

        //This could be one method using enum.
        public static Message Message(string message, string sender)
            => new Message(sender, MessageAction, message);

        public static Message ParseToMessage(string json) => JsonConvert.DeserializeObject<Message>(json);

        public static JAction ParseToJAction(string json) => JsonConvert.DeserializeObject<JAction>(json);

        //This could be one method using enum.
        public static JAction MemberJoins(string userName) => new JAction(NewMemberAction, userName);

        //This could be one method using enum.
        public static JAction MemberDisconnects(string userName) => new JAction(DisconnectAction, userName);

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}