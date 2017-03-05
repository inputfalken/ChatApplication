using Newtonsoft.Json;

namespace Protocol {
    /// <summary>
    ///     JSON Action: Jaction
    /// </summary>
    public class JAction {
        public const string NewMemberAction = "newMember";
        private const string DisconnectAction = "disconnect";
        public const string MessageAction = "message";
        public const string StatusAction = "validate";
        public const string Success = "sucess";
        public const string Fail = "fail";
        public const string MembersAction = "members";

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
        public static string Message(string message, string sender)
            => new Message(sender, MessageAction, message).ToString();

        public static Message ParseMessage(string json) => JsonConvert.DeserializeObject<Message>(json);

        public static JAction ParseJAction(string json) => JsonConvert.DeserializeObject<JAction>(json);

        public static string Create<T>(string action, T result)
            => new JAction(action, JsonConvert.SerializeObject(result)).ToString();

        public static T Parse<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        public static string StatusFail() => new JAction(StatusAction, Fail).ToString();

        public static string StatusSucess() => new JAction(StatusAction, Success).ToString();

        //This could be one method using enum.
        public static string MemberJoins(string userName) => new JAction(NewMemberAction, userName).ToString();

        //This could be one method using enum.
        public static string MemberDisconnects(string userName) => new JAction(DisconnectAction, userName).ToString();


        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}