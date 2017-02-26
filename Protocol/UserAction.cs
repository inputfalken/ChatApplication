using Newtonsoft.Json;

namespace Protocol {
    internal class UserAction : ChatProtcol {
        [JsonConstructor]
        public UserAction(string userName, string action, string result) : base(action, result) {
            UserName = userName;
        }

        [JsonProperty("user")]
        private string UserName { get; }
    }
}