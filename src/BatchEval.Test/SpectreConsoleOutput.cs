#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

using BatchEval.Core;
using Csv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TextGeneration;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BatchEval.Test
{
    internal static class SpectreConsoleOutput
    {
        public static void DisplayKernels(Kernel testKernel, Kernel evalKernel)
        {
            // Create a table
            var table = new Table();

            // Add columns
            table.AddColumn("kernel name");
            table.AddColumn("service");
            table.AddColumn("Key - Value");

            DisplayKernelInfo(testKernel, "test", table);
            DisplayKernelInfo(evalKernel, "eval", table);

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
                List<string> row = new List<string>();
                row.Add(kernelName);
                row.Add(serviceName);
                row.Add($"{atr.Key} - {atr.Value}");
                table.AddRow(row.ToArray());
            }
        }

        public static void DisplayResults(BatchEvalResults results)
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

            // rows 
            foreach (var result in results.EvalResults)
            {
                List<string> row = new List<string>();
                row.Add(result.Subject.Input);
                row.Add(result.Subject.Output);
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
                                        
                    row.Add($"[{color}]{value?.ToString() ?? string.Empty}[/]");
                }

                table.AddRow(row.ToArray());
            }

            // Render the table to the console
            AnsiConsole.Write(table);
        }
    }
}
