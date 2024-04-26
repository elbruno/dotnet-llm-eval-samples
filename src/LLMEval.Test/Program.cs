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
        SpectreConsoleOutput.DisplayTitle();


        // ========================================
        // create kernels
        // ========================================
        SpectreConsoleOutput.DisplaySubTitle($"LLM Kernels");
        var kernelEval = KernelFactory.CreateAndConfigureKernelEval();
        var kernelTest = KernelFactory.CreateAndConfigureKernelTest();
        SpectreConsoleOutput.DisplayKernels(kernelTest, kernelEval);

        // ========================================
        // create LLMEval and add evaluators
        // ========================================
        var kernelEvalFunctions = kernelEval.CreatePluginFromPromptDirectory("Prompts");
        var batchEval = new Core.LLMEval();

        batchEval
            .AddEvaluator(new PromptScoreEval("coherence", kernelEval, kernelEvalFunctions["coherence"]))
            .AddEvaluator(new PromptScoreEval("groundedness", kernelEval, kernelEvalFunctions["groundedness"]))
            .AddEvaluator(new PromptScoreEval("relevance", kernelEval, kernelEvalFunctions["relevance"]))
            .AddEvaluator(new LenghtEval());
        batchEval.SetMeterId("phi-3");


        Console.WriteLine("");
        SpectreConsoleOutput.DisplaySubTitle($"Processing single items: QA and User Story");

        // ========================================
        // evaluate a single Question and Answer
        // ========================================
        var qaProcessor = new QACreator.QACreator(kernelTest);
        var qa = new QA
        {
            Question = "How do you suggest to crack an egg? Suggest the most common way to do this.",
            Answer = "Tap the egg on a flat surface and then crack the shell"
        };
        var processResult = await qaProcessor.Process(qa);
        var results = await batchEval.ProcessSingle(processResult);
        results.EvalRunName = "QA Run 1";
        SpectreConsoleOutput.DisplayResults(results);

        // ========================================
        // evaluate a single User Story
        // ========================================
        var userstoryProcessor = new UserStoryCreator.UserStoryCreator(kernelTest);
        var userInput = new UserInput
        {
            Description = "Fix a broken appliance",
            ProjectContext = "At home",
            Persona = "Homeowner"
        };
        processResult = await userstoryProcessor.Process(userInput);
        results = await batchEval.ProcessSingle(processResult);
        SpectreConsoleOutput.DisplayResults(results);

        // ========================================
        // evaluate a batch of inputs for User Stories from a file
        // ========================================
        SpectreConsoleOutput.DisplaySubTitle("Processing batch of User Stories");
        var fileName = "assets/data-02.json";
        Console.WriteLine("");
        Console.WriteLine($"Processing {fileName} ...");

        // load the sample data
        var userStoryCreator = new UserStoryCreator.UserStoryCreator(kernelTest);
        var userInputCollection = await UserStoryGenerator.FileProcessor.ProcessUserInputFile(fileName);
        
        var modelOutputCollection = await userStoryCreator.ProcessCollection(userInputCollection);
        results = await batchEval.ProcessCollection(modelOutputCollection);
        SpectreConsoleOutput.DisplayResults(results);

        // complete
        SpectreConsoleOutput.DisplaySubTitle("Complete.");
        
    }
}
