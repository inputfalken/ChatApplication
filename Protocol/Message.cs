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

        /// <summary>
        ///     Parses the message into it's basic form.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Message ParseMessage(string json) => JsonConvert.DeserializeObject<Message>(json);

        /// <summary>
        ///     Creates an Message with it's data with as json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static string Create<T>(Action action, T result)
            => new Message(action, JsonConvert.SerializeObject(result)).ToString();

        /// <summary>
        ///     Parses the json supplied to it's object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Parse<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        public T Parse<T>() => JsonConvert.DeserializeObject<T>(JsonObject);

        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    public class MemberMessage {
        public MemberMessage(string userName, string message) {
            UserName = userName;
            Message = message;
        }

        public string UserName { get; }
        public string Message { get; }
    }
}