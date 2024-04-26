using Microsoft.SemanticKernel;
using LLMEval.Core;
using LLMEval.Data;
using LLMEval.Output;
using LLMEval.Test;

namespace LLMEval;

class Program
{
    static async Task Main()
    {
        Console.WriteLine($"Creating kernels ...");

        // create kernels
        var kernelEval = KernelFactory.CreateAndConfigureKernelEval();
        var kernelTest = KernelFactory.CreateAndConfigureKernelTest();

        SpectreConsoleOutput.DisplayKernels(kernelTest, kernelEval);

        // create batcheval and add evaluators
        var kernelEvalFunctions = kernelEval.CreatePluginFromPromptDirectory("Prompts");
        var batchEval = new Core.LLMEval();

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
        var processResult = await qaProcessor.Process(qa);
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
        processResult = await userstoryProcessor.Process(userInput);
        resultsSingle = await batchEval.ProcessSingle(processResult);
        SpectreConsoleOutput.DisplayResults(resultsSingle);

        //// evaluate a batch of inputs for User Stories
        //var fileName = "assets/data-02.json";
        //Console.WriteLine("");
        //Console.WriteLine($"Processing batch of user stories inputs ...");
        //Console.WriteLine($"Processing {fileName} ...");

        //// load the sample data
        //var sampleUserStoryCollection = await UserStoryGenerator.FileProcessor.ProcessUserInputFile(fileName);
        //var userStoryCreator = new UserStoryCreator.UserStoryCreator(kernelTest);

        //processResult = await userstoryProcessor.
        //resultsSingle = await batchEval.ProcessSingle(processResult);
        //SpectreConsoleOutput.DisplayResults(resultsSingle);


        //LLMEvalResults results = await batchEval
        //    .AddModelOutputsCollection(sampleUserStoryCollection)
        //    .Run();

        //SpectreConsoleOutput.DisplayResults(results);

        //// export results to csv
        //Outputs.ExportToCsv.WriteCsv(results, "output.csv");


        Console.WriteLine($"Complete.");
        
    }
}
