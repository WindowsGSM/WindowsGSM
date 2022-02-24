using System.Text.Json.Serialization;

namespace WindowsGSM.Utilities.Steamworks
{
    public class ResponseWrapper<T>
    {
        [JsonPropertyName("response")]
        public T? Response { get; set; }
    }
}
