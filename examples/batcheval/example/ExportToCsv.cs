using BatchEval.Core;
using Csv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace BatchEval
{
    internal static class ExportToCsv
    {
        public static void WriteCsv(BatchEvalResults results, string fileLocation)
        {
            // headers
            var columnNames = new List<string> { "Input", "Input" };
            var first = results.EvalResults.First();
            foreach (var key in first.Results.Keys)
            {
                columnNames.Add(key);
            }

            // rows 
            var rows = new List<string[]>();
            foreach (var result in results.EvalResults)
            {
                var row = new List<string>
                {
                    result.Subject.Input,
                    result.Subject.Output
                };

                foreach (var value in result.Results.Values)
                {
                    row.Add(value?.ToString() ?? string.Empty);
                }

                rows.Add(row.ToArray());
            }

            var csv = CsvWriter.WriteToText(columnNames.ToArray(), rows.ToArray(), ',');
            File.WriteAllText(fileLocation, csv);
        }

        //var output = new StringBuilder();

        //// Headers
        //if (results.EvalResults.Any())
        //{
        //    output.Append("\"Input\",\"Output\",");

        //    var first = results.EvalResults.First();
        //    foreach (var key in first.Results.Keys)
        //    {
        //        output.Append($"\"{EscapeCsvValue(key)}\",");
        //    }
        //    output.Length--; // Remove the trailing comma
        //    output.AppendLine();
        //}

        //// Body
        //foreach (var result in results.EvalResults)
        //{
        //    output.Append($"\"{EscapeCsvValue(result.Subject.Input)}\",");
        //    output.Append($"\"{EscapeCsvValue(result.Subject.Output)}\",");

        //    foreach (var value in result.Results.Values)
        //    {
        //        output.Append($"\"{EscapeCsvValue(value?.ToString() ?? string.Empty)}\",");
        //    }
        //    output.Length--; // Remove the trailing comma
        //    output.AppendLine();
        //}

        //return output.ToString();
        //}
    }
}
