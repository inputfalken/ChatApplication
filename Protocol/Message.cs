using Newtonsoft.Json;

namespace Protocol {
    public enum Action {
        MemberJoin,
        MemberDisconnect,
        Message,
        Status,
        SendMembers,
        MemberMessage
    }

    public class Message {
        [JsonConstructor]
        private Message(Action action, string jsonObject) {
            Action = action;
            JsonObject = jsonObject;
        }

        [JsonProperty("action")]
        public Action Action { get; }

        [JsonProperty("result")]
        public string JsonObject { get; }

        public static Message ParseJAction(string json) => JsonConvert.DeserializeObject<Message>(json);

        public static string Create<T>(Action action, T result)
            => new Message(action, JsonConvert.SerializeObject(result)).ToString();

        public static T Parse<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    public class MemberMessage {
        public string UserName { get; }
        public string Message { get; }

        public MemberMessage(string userName, string message) {
            UserName = userName;
            Message = message;
        }
    }
}