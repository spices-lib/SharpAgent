using Microsoft.Extensions.VectorData;
using System;
using System.Text;
using System.Threading.Tasks;

public class SearchTool(VectorStoreCollection<Guid, VectorEntry> vectorStoreCollection)
{
    public async Task<string> Search(string input)
    {
        StringBuilder mostSimilarKnowledge = new StringBuilder();
        int numberOfSearchResultsWeWantBack = 3;
        await foreach (VectorSearchResult<VectorEntry> searchResult in vectorStoreCollection.SearchAsync(
            searchValue: input,
            top: numberOfSearchResultsWeWantBack
        ))
        {
            string searchResultAsQAndA = $"Q: {searchResult.Record.Question} - A: {searchResult.Record.Answer}";
            Console.WriteLine($"- Search result [Score: {searchResult.Score}] {searchResultAsQAndA}");
            mostSimilarKnowledge.AppendLine(searchResultAsQAndA);
        }

        Console.WriteLine();

        return mostSimilarKnowledge.ToString();
    }
}