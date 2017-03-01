using Newtonsoft.Json;

namespace Protocol {
    public class Message : JAction {
        [JsonConstructor]
        public Message(string sender, string action, string result) : base(action, result) {
            Sender = sender;
        }

        [JsonProperty("sender")]
        public string Sender { get; }
    }
}