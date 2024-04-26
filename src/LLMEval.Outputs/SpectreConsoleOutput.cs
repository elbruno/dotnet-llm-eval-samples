#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

using LLMEval.Core;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.TextGeneration;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LLMEval.Output;

public static class SpectreConsoleOutput
{
    public static void DisplayTitle(string title = "LLM Eval Results")
    {
        AnsiConsole.Write(new FigletText(title).Centered().Color(Color.Purple));
    }

    public static void DisplaySubTitle(string subtitle)
    {
        // add a header using === before the subtitle
        AnsiConsole.MarkupLine($"[bold]=== {subtitle} ===[/]");
        AnsiConsole.MarkupLine($"[bold][/]");        
    }

    public static void DisplayKernels(Kernel testKernel, Kernel evalKernel)
    {
        // Create a table
        var table = new Table();

        // Add columns
        table.AddColumn("kernel name");
        table.AddColumn("service");
        table.AddColumn("Key - Value");

        DisplayKernelInfo(testKernel, "Test", table);
        DisplayKernelInfo(evalKernel, "Eval", table);

        // Render the table to the console
        AnsiConsole.Write(table);
    }

    public static void DisplayKernelInfo(Kernel kernel, string kernelName, Table table)
    {
        foreach (var service in kernel.GetAllServices<IChatCompletionService>().ToList())
        {
            AddRow(table, kernelName, "IChatCompletionService", service.Attributes);
        }

        foreach (var service in kernel.GetAllServices<ITextEmbeddingGenerationService>().ToList())
        {
            AddRow(table, kernelName, "ITextEmbeddingGenerationService", service.Attributes);
        }

        foreach (var service in kernel.GetAllServices<ITextGenerationService>().ToList())
        {
            AddRow(table, kernelName, "ITextGenerationService", service.Attributes);
        }
    }

    
    private static void AddRow(Table table, string kernelName, string serviceName, IReadOnlyDictionary<string, object?> services)
    {
        foreach (var atr in services)
        {
            List<Renderable> row = [new Markup($"[bold]= {kernelName} =[/]"), new Text(serviceName), new Text($"{atr.Key} - {atr.Value}")];
            table.AddRow(row.ToArray());
        }
    }

    public static void DisplayResults(LLMEvalResults results)
    {
        // Create a table
        var table = new Table();

        // Add some columns
        table.AddColumn("Input");
        table.AddColumn("Output");
        var first = results.EvalResults.First();
        foreach (var key in first.Results.Keys)
        {
            table.AddColumn(new TableColumn(key).Centered());
        }
        table.Columns[0].PadLeft(1).PadRight(1);
        table.Columns[1].PadLeft(1).PadRight(1);
                
        foreach (var result in results.EvalResults)
        {
            List<Renderable> row = [
                new Text(result.Subject.Input), 
                new Text(result.Subject.Output)];
            
            // add the evaluation results
            foreach (var value in result.Results.Values)
            {
                var intValue = (int)value;
                string color = "white";
                if (intValue <= 1)
                    color = "red";
                else if (intValue <= 3)
                    color = "yellow";
                else if (intValue <= 5)
                    color = "green";

                row.Add(new Markup($"[{color}]{value?.ToString() ?? string.Empty}[/]"));
            }
            table.AddRow(row.ToArray());
        }

        //Render the table to the console
        AnsiConsole.Write(table);
    }
}
