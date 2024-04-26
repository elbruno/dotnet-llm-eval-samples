using Microsoft.SemanticKernel;
using System.Text.Json;

namespace QAGenerator;

public class QASkill
{
    private readonly KernelFunction _createQAFunction;

    private readonly Kernel _kernel;

    public static QASkill Create(Kernel kernel)
    {
        string promptTemplate = EmbeddedResource.Read("_prompts.qa.skprompt.txt")!;

        return new QASkill(kernel, kernel.CreateFunctionFromPrompt(promptTemplate));
    }

    public QASkill(Kernel kernel, KernelFunction promptFunction)
    {
        _createQAFunction = promptFunction;
        _kernel = kernel;
    }

    public async Task<QA?> GetQA(string question, string answer)
    {
        var context = new KernelArguments
        {
            { "question", question },
            { "answer", answer }
        };

        var result = await _createQAFunction.InvokeAsync(_kernel, context);

        var qa = JsonSerializer.Deserialize<QA>(result.ToString(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return qa;
    }
}