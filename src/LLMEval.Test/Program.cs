#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using LLMEval.Core;
using LLMEval.Data;
using LLMEval.Output;

namespace LLMEval;

class Program
{
    private static Kernel CreateAndConfigureKernelTest()
    {       
        var builder = Kernel.CreateBuilder();
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
        var kernelTest = CreateAndConfigureKernelTest();

        SpectreConsoleOutput.DisplayKernels(kernelTest, kernelEval);

        // create batcheval and add evaluators
        var kernelEvalFunctions = kernelEval.CreatePluginFromPromptDirectory("Prompts");
        var batchEval = new BatchEval();

        batchEval
            .AddEvaluator(new PromptScoreEval("coherence", kernelEval, kernelEvalFunctions["coherence"]))
            .AddEvaluator(new PromptScoreEval("groundedness", kernelEval, kernelEvalFunctions["groundedness"]))
            .AddEvaluator(new PromptScoreEval("relevance", kernelEval, kernelEvalFunctions["relevance"]))
            .AddEvaluator(new LenghtEval());
        batchEval.SetMeterId("phi-3");


        Console.WriteLine("");
        Console.WriteLine($"Processing single QAs ...");

        // evaluate a single qa
        var qaProcessor = new QACreator.QACreator(kernelTest);
        var qa = new QA
        {
            Question = "How do you suggest to crack an egg? Suggest the most common way to do this.",
            Answer = "Tap the egg on a flat surface and then crack the shell"
        };
        var processResult = await qaProcessor.ProcessQA(qa);
        var resultsSingle = await batchEval.ProcessSingle(processResult);
        SpectreConsoleOutput.DisplayResults(resultsSingle);

        // evaluate a single user story
        var userstoryProcessor = new UserStoryCreator.UserStoryCreator(kernelTest);
        var userInput = new UserInput
        {
            Description = "Fix a broken appliance",
            ProjectContext = "At home",
            Persona = "Homeowner"
        };
        processResult = await userstoryProcessor.ProcessUserInput(userInput);
        resultsSingle = await batchEval.ProcessSingle(processResult);
        SpectreConsoleOutput.DisplayResults(resultsSingle);

        // evaluate a batch of inputs for User Stories
        var fileName = "assets/data-02.json";
        Console.WriteLine("");
        Console.WriteLine($"Processing batch of user stories inputs ...");
        Console.WriteLine($"Processing {fileName} ...");

        // load the sample data
        var sampleUserStoryCollection = await UserStoryGenerator.FileProcessor.ProcessUserInputFile(fileName);
        var userStoryCreator = new UserStoryCreator.UserStoryCreator(kernelTest);

        processResult = await userstoryProcessor.
        resultsSingle = await batchEval.ProcessSingle(processResult);
        SpectreConsoleOutput.DisplayResults(resultsSingle);


        BatchEvalResults results = await batchEval
            .AddModelOutputsCollection(sampleUserStoryCollection)
            .Run();

        SpectreConsoleOutput.DisplayResults(results);

        // export results to csv
        Outputs.ExportToCsv.WriteCsv(results, "output.csv");


        Console.WriteLine($"Complete.");
        
    }
}
