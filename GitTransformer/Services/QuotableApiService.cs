using System.Net.Http.Json;

namespace GitTransformer.Services;

public class QuotableApiService([FromKeyedServices("quotable")] HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;

    public async Task<Quote> GetRandomQuote()
    {
        try
        {
            var result = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "random"));

            if (!result.IsSuccessStatusCode)
                return new Quote();

            var response = await result.Content.ReadFromJsonAsync<SingleQuotableResponse>();

            return new Quote(response);
        }
        catch
        {
            return new Quote();
        }
    }
}