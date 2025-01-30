using System.Net.Http.Json;

namespace GitTransformer.Services;

public class LocalFileService
{
    private readonly List<Quote> _quotes = [];
    private readonly Task<Quote> PopulateQuotes;

    public LocalFileService([FromKeyedServices("local")] HttpClient httpClient)
    {
        PopulateQuotes = Task.Run(async () =>
        {
            await foreach (var quote in httpClient.GetFromJsonAsAsyncEnumerable<Quote>("data/quotes.json"))
                if (quote is not null) _quotes.Add(quote);
            return _quotes[new Random().Next(_quotes.Count)];
        });
    }

    public async Task<Quote> GetRandomQuote()
    {
        if (_quotes.Count > 0)
            return _quotes[new Random().Next(_quotes.Count)];
        else
            return await PopulateQuotes;
    }
}