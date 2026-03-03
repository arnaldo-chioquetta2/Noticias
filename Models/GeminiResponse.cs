using Newtonsoft.Json;

public class GeminiResponse
{
    [JsonProperty("impactScore")]
    public double impactScore { get; set; }

    [JsonProperty("impactReason")]
    public string impactReason { get; set; }

    [JsonProperty("category")]
    public string category { get; set; }
}