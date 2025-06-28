using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class TranslationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string ApiKey = "AIzaSyDWR1DdfRh7hZXHxZdl_l8AcAQjn2PWZVU";
    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    public TranslationController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    [HttpPost("detect")]
public async Task<IActionResult> DetectLanguage([FromBody] DetectLanguageRequest request)
{
    var prompt = $"What is the ISO 639-1 language code (like en, ar, fr) of the following sentence? Just return the code only, nothing else.\n\n\"{request.Text}\"";

    var client = _httpClientFactory.CreateClient();

    var requestBody = new
    {
        contents = new[]
        {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        }
    };

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}?key={ApiKey}");
    httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

    var response = await client.SendAsync(httpRequest);
    var responseText = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        return StatusCode((int)response.StatusCode, new { error = responseText });
    }

    try
    {
        using var doc = JsonDocument.Parse(responseText);
        var candidates = doc.RootElement.GetProperty("candidates");
        var languageCode = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()?.Trim().ToLower();

        return Ok(new { language = languageCode });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = "Parsing failed", details = ex.Message, raw = responseText });
    }
}


    [HttpPost("translate")]
    public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
    {
        var prompt = $"Translate this sentence to {request.To ?? "English"} (only one clear sentence, no explanation): {request.Text}";
        var client = _httpClientFactory.CreateClient();

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}?key={ApiKey}");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(httpRequest);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new { error = responseText });
        }

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var candidates = doc.RootElement.GetProperty("candidates");
            var translation = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            return Ok(new { translatedText = translation });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Parsing failed", details = ex.Message, raw = responseText });
        }
    }
}

public class TranslationRequest
{
    public string Text { get; set; }
    public string To { get; set; } 
}
public class DetectLanguageRequest
{
    public string Text { get; set; }
}
