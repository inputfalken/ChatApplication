using Newtonsoft.Json;

namespace Protocol {
    public class Message : ChatProtcol {
        [JsonConstructor]
        public Message(string sender, string action, string result) : base(action, result) {
            Sender = sender;
        }

        [JsonProperty("sender")]
        private string Sender { get; }
    }
}