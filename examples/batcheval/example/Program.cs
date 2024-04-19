using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text;
using BatchEval.Core;

namespace BatchEval;

class Program
{
    private static Kernel CreateAndConfigureKernel()
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
        var kernel = CreateAndConfigureKernel();

        var fileName = "assets/data.jsonl";

        Console.WriteLine($"Reading {fileName}, press a key to continue ...");
        //Console.ReadLine();

        var kernelFunctions = kernel.CreatePluginFromPromptDirectory("Prompts");

        var batchEval = new BatchEval<UserInput>();

        batchEval
            .AddEvaluator(new PromptScoreEval("coherence", kernel, kernelFunctions["coherence"]))
            .AddEvaluator(new PromptScoreEval("groundedness", kernel, kernelFunctions["groundedness"]))
            .AddEvaluator(new PromptScoreEval("relevance", kernel, kernelFunctions["relevance"]))
            .AddEvaluator(new LenghtEval());

        BatchEvalResults results = await batchEval
            .WithInputProcessor(new UserStoryCreator(kernel))
            .WithJsonl(fileName)
            .Run();

        ExportToCsv.WriteCsv(results, "output.csv");        

        Console.WriteLine($"Complete.");
        
    }
}
