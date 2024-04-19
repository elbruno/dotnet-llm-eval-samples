using BatchEval.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchEval
{
    internal static class ExportToCsv
    {
        public static string ToCsv(BatchEvalResults results)
        {
            var output = new StringBuilder();

            // Headers
            if (results.EvalResults.Any())
            {
                output.Append("\"Input\",\"Output\",");

                var first = results.EvalResults.First();
                foreach (var key in first.Results.Keys)
                {
                    output.Append($"\"{EscapeCsvValue(key)}\",");
                }
                output.Length--; // Remove the trailing comma
                output.AppendLine();
            }

            // Body
            foreach (var result in results.EvalResults)
            {
                output.Append($"\"{EscapeCsvValue(result.Subject.Input)}\",");
                output.Append($"\"{EscapeCsvValue(result.Subject.Output)}\",");

                foreach (var value in result.Results.Values)
                {
                    output.Append($"\"{EscapeCsvValue(value?.ToString() ?? string.Empty)}\",");
                }
                output.Length--; // Remove the trailing comma
                output.AppendLine();
            }

            return output.ToString();
        }

        private static string EscapeCsvValue(string value)
        {
            // If value contains double quotes, escape them by doubling them
            if (value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
            }

            // If value contains comma, surround it with double quotes
            if (value.Contains(","))
            {
                value = $"\"{value}\"";
            }

            return value;
        }
    }
}
