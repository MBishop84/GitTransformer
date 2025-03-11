using System.Net.Http.Json;

namespace GitTransformer.Services;

public class QuotableApiService(
    [FromKeyedServices("quotable")] HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;

    public async Task<Quote> GetRandomQuote()
    {
        try
        {
            return new Quote(
                await HttpClient.GetFromJsonAsync<SingleQuotableResponse>("random"));
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return new Quote();
        }
    }
}