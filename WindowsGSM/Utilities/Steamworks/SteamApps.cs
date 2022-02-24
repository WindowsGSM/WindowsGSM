using System.Text.Json.Serialization;

namespace WindowsGSM.Utilities.Steamworks
{
    public class SteamApps
    {
        public class UpToDateCheck
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("up_to_date")]
            public bool UpToDate { get; set; }

            [JsonPropertyName("version_is_listable")]
            public bool VersionIsListable { get; set; }

            [JsonPropertyName("required_version")]
            public int? RequiredVersion { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }
    }
}
