using LLMEval.Data;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace QAGenerator;

public class QALLMGenerator
{
    public static async Task<QA> GenerateQAusingLLM(Kernel kernel) {
        var pluginsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "_prompts");
        var plugins = kernel.CreatePluginFromPromptDirectory(pluginsDirectoryPath);
        var result = await kernel.InvokeAsync(plugins["qagen"]);
        var resultString = result.ToString();

        var qa = new QA();
        try
        {
            qa = JsonSerializer.Deserialize<QA>(resultString, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (JsonException jsonExc)
        {
            qa.Question = "An error occurred while generating the QA. Error descripton";
            qa.Answer = jsonExc.Message.ToString();
        }
        return qa;
    }

}
