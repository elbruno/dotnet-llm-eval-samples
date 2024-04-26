using Microsoft.SemanticKernel;
using LLMEval.Core;
using LLMEval.Data;
using LLMEval.Output;
using LLMEval.Test;
using QAGenerator;

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
        var kernelTest = KernelFactory.CreateAndConfigureKernelEval();
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

        var scenarios = SpectreConsoleOutput.SelectScenarios();

        Console.WriteLine("");
        SpectreConsoleOutput.DisplayTitleH2($"Processing single items: 1 LLM generated QA, 2 QAs and 1 User Story");

        if(scenarios.Contains("1 generated QA using LLM"))
        {
            // ========================================
            // evaluate a random generated Question and Answer
            // ========================================
            var qa = await QALLMGenerator.GenerateQA(kernelTest);
            var qaProcessor = new QACreator.QACreator(kernelTest);
            var processResult = await qaProcessor.Process(qa);
            var results = await batchEval.ProcessSingle(processResult);
            results.EvalRunName = "Auto generated QA using LLM";
            SpectreConsoleOutput.DisplayResults(results);
        }

        if (scenarios.Contains("2 harcoded QAs"))
        {
            // ========================================
            // evaluate 2 Question and Answer
            // ========================================
            var qaProcessor = new QACreator.QACreator(kernelTest);
            var qa = new Data.QA
            {
                Question = "How do you suggest to crack an egg? Suggest the most common way to do this.",
                Answer = "Tap the egg on a flat surface and then crack the shell",
                Topic = "Cooking"
            };
            var processResult = await qaProcessor.Process(qa);
            var results = await batchEval.ProcessSingle(processResult);
            results.EvalRunName = "Harcoded QA 1";
            SpectreConsoleOutput.DisplayResults(results);

            qa = new QA
            {
                Question = "two plus two",
                Answer = "'4' or 'four'",
                Topic = "Math"
            };
            processResult = await qaProcessor.Process(qa);
            results = await batchEval.ProcessSingle(processResult);
            results.EvalRunName = "Harcoded QA 2";
            SpectreConsoleOutput.DisplayResults(results);
        }

        if (scenarios.Contains("1 harcoded User Story"))
        {
            // ========================================
            // evaluate a single User Story
            // ========================================
            var userstoryProcessor = new UserStoryCreator.UserStoryCreator(kernelTest);
            var userInput = new UserStory
            {
                Description = "Fix a broken appliance",
                ProjectContext = "At home",
                Persona = "Homeowner"
            };
            var processResult = await userstoryProcessor.Process(userInput);
            var results = await batchEval.ProcessSingle(processResult);
            results.EvalRunName = "Harcoded User Story Run 1";
            SpectreConsoleOutput.DisplayResults(results);
        }

        if (scenarios.Contains("List of User Stories from a file"))
        {
            // ========================================
            // evaluate a batch of inputs for User Stories from a file
            // ========================================
            SpectreConsoleOutput.DisplayTitleH2("Processing batch of User Stories");
            var fileName = "assets/data-02.json";
            Console.WriteLine($"Processing {fileName} ...");
            Console.WriteLine("");

            // load the sample data
            var userStoryCreator = new UserStoryCreator.UserStoryCreator(kernelTest);
            var userInputCollection = await UserStoryGenerator.FileProcessor.ProcessUserInputFile(fileName);

            var modelOutputCollection = await userStoryCreator.ProcessCollection(userInputCollection);
            var results = await batchEval.ProcessCollection(modelOutputCollection);
            results.EvalRunName = "User Story collection from file";
            SpectreConsoleOutput.DisplayResults(results);
        }

        if (scenarios.Contains("List of QAs generated using a LLM"))
        {
            // ========================================
            // evaluate a batch of generated QAs generated using llm
            // ========================================
            SpectreConsoleOutput.DisplayTitleH2("Processing LLM generated QAs");

            // ask for the number of QAs to generate
            var numberOfQAs = SpectreConsoleOutput.AskForNumber("How many QAs do you want to generate?");

            // generate a collection of QAs using llms
            var llmGenQAs = await QALLMGenerator.GenerateQACollection(kernelTest, numberOfQAs);
            var qaProcessor = new QACreator.QACreator(kernelTest);
            var modelOutputCollection = await qaProcessor.ProcessCollection(llmGenQAs);
            var results = await batchEval.ProcessCollection(modelOutputCollection);
            results.EvalRunName = "LLM generated QAs";
            SpectreConsoleOutput.DisplayResults(results);
        }

        // complete        
        SpectreConsoleOutput.DisplayTitleH2("Complete.");
        
    }
}
