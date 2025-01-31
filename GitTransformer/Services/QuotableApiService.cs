using System.Net.Http.Json;

namespace GitTransformer.Services;

public class QuotableApiService([FromKeyedServices("quotable")] HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;

    public async Task<Quote> GetRandomQuote()
    {
        try
        {
            var result = await HttpClient.GetFromJsonAsync<SingleQuotableResponse>("random");

            return new Quote(result);
        }
        catch
        {
            return new Quote();
        }
    }
}