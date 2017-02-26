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
        public static ChatProtcol Message(string message, string userName)
            => new UserAction(userName, "message", message);

        //This could be one method using enum.
        public static ChatProtcol MemberJoins(string userName) => new ChatProtcol(NewMember, userName);

        //This could be one method using enum.
        public static ChatProtcol MemberDisconnects(string userName) => new ChatProtcol(Disconnect, userName);

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}