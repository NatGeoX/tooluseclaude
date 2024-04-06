using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        await PostMessageAsync();
    }

    static async Task InitializeHttpClient()
    {
        var anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("x-api-key", anthropicApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        client.DefaultRequestHeaders.Add("anthropic-beta", "tools-2024-04-04");
    }

    public static async Task PostMessageAsync()
    {
        await InitializeHttpClient();

        var url = "https://api.anthropic.com/v1/messages";
        var requestBody = CreateRequestBody();

        try
        {
            var response = await client.PostAsync(url, requestBody);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseBody);
                var toolUseResponse = JsonConvert.DeserializeObject<ToolUseResponse>(responseBody);
                HandleToolUseResponse(toolUseResponse?.ToolUse);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request exception: {e.Message}");
        }
    }

    private static StringContent CreateRequestBody()
    {
        var message = new
        {
            model = "claude-3-opus-20240229",
            max_tokens = 1024,
            tools = new[]
            {
                new
                {
                    name = "get_weather",
                    description = "Get the current weather in a given location",
                    input_schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            location = new { type = "string", description = "The city and state, e.g. San Francisco, CA" },
                            unit = new { type = "string", @enum = new[] { "celsius", "fahrenheit" }, description = "The unit of temperature, either 'celsius' or 'fahrenheit'" }
                        },
                        required = new[] { "location" }
                    }
                }
            },
            messages = new[]
            {
                new { role = "user", content = "What is the weather like in Tbilisi, Georgia in unit Celsius?" }
            }
        };

        var json = JsonConvert.SerializeObject(message);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static void HandleToolUseResponse(ToolUse toolUse)
    {
        if (toolUse?.Name == "get_weather")
        {
            var wthRsps = new
            {
                Result = $"The current weather in {toolUse.Input.location} is sunny with a high of 25°C."
            };
            SendRsps(wthRsps);
        }
    }

    private static void SendRsps(object xRsps)
    {
        Console.WriteLine(JsonConvert.SerializeObject(xRsps, Formatting.Indented));
    }

    public class ToolUseResponse
    {
        public ToolUse ToolUse { get; set; }
    }

    public class ToolUse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ToolInput Input { get; set; }
    }

    public class ToolInput
    {
        public string location { get; set; }
        public string unit { get; set; }
    }
}
