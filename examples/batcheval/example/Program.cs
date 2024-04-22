using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text;
using BatchEval.Core;
using BatchEval.Test;

namespace BatchEval;

class Program
{
    private static Kernel CreateAndConfigureKernelTest()
    {
        var customHttpMessageHandler = new CustomHttpMessageHandler();
        customHttpMessageHandler.CustomLlmUrl = "http://localhost:11434";
        HttpClient client = new HttpClient(customHttpMessageHandler);
       
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion("llama3", "api-key", httpClient: client);

        return builder.Build();
    }

    private static Kernel CreateAndConfigureKernelEval()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            config["AZURE_OPENAI_MODEL"],
            config["AZURE_OPENAI_ENDPOINT"],
            config["AZURE_OPENAI_KEY"]);

        return builder.Build();
    }


    static async Task Main()
    {
        var kernelEval = CreateAndConfigureKernelEval();
        var kernelTest = CreateAndConfigureKernelEval();

        var fileName = "assets/data-02.json";

        Console.WriteLine($"Processing {fileName} ...");

        var kernelEvalFunctions = kernelEval.CreatePluginFromPromptDirectory("Prompts");

        var batchEval = new BatchEval<UserInput>();
        batchEval.meterId = "phi-llm";

        batchEval
            .AddEvaluator(new PromptScoreEval("coherence", kernelEval, kernelEvalFunctions["coherence"]))
            .AddEvaluator(new PromptScoreEval("groundedness", kernelEval, kernelEvalFunctions["groundedness"]))
            .AddEvaluator(new PromptScoreEval("relevance", kernelEval, kernelEvalFunctions["relevance"]))
            .AddEvaluator(new LenghtEval());

        BatchEvalResults results = await batchEval
            .WithInputProcessor(new UserStoryCreator(kernelTest))
            .WithJsonl(fileName)
            .Run();

        BatchEval.Outputs.ExportToCsv.WriteCsv(results, "output.csv");
        SpectreConsoleOutput.DisplayResults(results);

        Console.WriteLine($"Complete.");
        
    }
}
