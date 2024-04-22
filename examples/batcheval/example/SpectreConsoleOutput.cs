using BatchEval.Core;
using Csv;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchEval.Test
{
    internal static class SpectreConsoleOutput
    {
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
