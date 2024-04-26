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
        SpectreConsoleOutput.DisplayTitleH2($"LLM Kernels");
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
        batchEval.SetMeterId("llama3");


        Console.WriteLine("");
        SpectreConsoleOutput.DisplayTitleH2($"Processing single items: 2 QAs and 1 User Story");

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

        qa = new QA
        {
            Question = "two plus two",
            Answer = "'4' or 'four'"
        };
        processResult = await qaProcessor.Process(qa);
        results = await batchEval.ProcessSingle(processResult);
        results.EvalRunName = "QA Run 2";
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
        results.EvalRunName = "User Story Run 1";
        SpectreConsoleOutput.DisplayResults(results);

        // ========================================
        // evaluate a batch of inputs for User Stories from a file
        // ========================================
        SpectreConsoleOutput.DisplayTitleH2("Processing batch of User Stories");
        var fileName = "assets/data-02.json";
        Console.WriteLine("");
        Console.WriteLine($"Processing {fileName} ...");

        // load the sample data
        var userStoryCreator = new UserStoryCreator.UserStoryCreator(kernelTest);
        var userInputCollection = await UserStoryGenerator.FileProcessor.ProcessUserInputFile(fileName);
        
        var modelOutputCollection = await userStoryCreator.ProcessCollection(userInputCollection);
        results = await batchEval.ProcessCollection(modelOutputCollection);
        results.EvalRunName = "User Story collection from file";
        SpectreConsoleOutput.DisplayResults(results);

        // complete        
        SpectreConsoleOutput.DisplayTitleH2("Complete.");
        
    }
}
