using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text;
using BatchEval.Core;
using BatchEval.Test;
using BatchEval.Data;
using System.Threading;

namespace BatchEval;

class Program
{
    private static Kernel CreateAndConfigureKernelTest()
    {
        var customHttpMessageHandler = new CustomHttpMessageHandler();
        customHttpMessageHandler.CustomLlmUrl = "http://localhost:11434";
        HttpClient client = new HttpClient(customHttpMessageHandler);
       
        var builder = Kernel.CreateBuilder();

        #pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
        builder.AddOpenAIChatCompletion(
            modelId: "phi3", 
            endpoint: new Uri("http://localhost:11434"), 
            apiKey:"api");

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
        Console.WriteLine($"Creating kernels ...");

        // create kernels
        var kernelEval = CreateAndConfigureKernelEval();
        var kernelTest = CreateAndConfigureKernelEval();

        // create batcheval and add evaluators
        var kernelEvalFunctions = kernelEval.CreatePluginFromPromptDirectory("Prompts");
        var batchEval = new BatchEval<UserInput>();
        batchEval.meterId = "phi-llm";

        batchEval
            .AddEvaluator(new PromptScoreEval("coherence", kernelEval, kernelEvalFunctions["coherence"]))
            .AddEvaluator(new PromptScoreEval("groundedness", kernelEval, kernelEvalFunctions["groundedness"]))
            .AddEvaluator(new PromptScoreEval("relevance", kernelEval, kernelEvalFunctions["relevance"]))
            .AddEvaluator(new LenghtEval());


        Console.WriteLine("");
        Console.WriteLine($"Processing single user input ...");

        // evaluate a single input
        Meter meter = new Meter("phi-llm");
        IInputProcessor inputProcessor = new UserStoryCreator.UserStoryCreator(kernelTest);
        var userInput = new UserInput
        {
            Description = "Fix a broken appliance",
            ProjectContext = "At home",
            Persona = "Homeowner"
        };
        var resultsSingle = await batchEval.ProcessSingle(meter, userInput, inputProcessor);
        SpectreConsoleOutput.DisplayResults(resultsSingle);


        // evaluate a batch of inputs
        var fileName = "assets/data-02.json";
        Console.WriteLine("");
        Console.WriteLine($"Processing batch of inputs ...");
        Console.WriteLine($"Processing {fileName} ...");

        BatchEvalResults results = await batchEval
            .WithInputProcessor(new UserStoryCreator.UserStoryCreator(kernelTest))
            .WithJsonl(fileName)
            .Run();                
        SpectreConsoleOutput.DisplayResults(results);

        // export results to csv
        Outputs.ExportToCsv.WriteCsv(results, "output.csv");


        Console.WriteLine($"Complete.");
        
    }
}
