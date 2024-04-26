using System.Text;
using BatchEval.Data;

namespace UserStoryGenerator;

public static class FileProcessor
{
    public static async Task<List<UserInput>> ProcessUserInputFile(string fileName)
    {       
        var results = new List<UserInput>();

        const int BufferSize = 128;
        using (var fileStream = File.OpenRead(fileName!))
        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
        {   
            string? line;
            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                var userInput = System.Text.Json.JsonSerializer.Deserialize<UserInput>(line);
                results.Add(userInput);
            }
        }
        return results;
    }
}
