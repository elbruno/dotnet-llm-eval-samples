using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;
using System.Text;

namespace BatchEval.Core;

public class BatchEval<T>
{
    IList<IEvaluator<int>> intEvaluators = new List<IEvaluator<int>>();

    IList<IEvaluator<bool>> boolEvaluators = new List<IEvaluator<bool>>();

    string? fileName;

    bool showConsoleOutput = false;

    IInputProcessor? inputProcessor;

    IOutputProcessor? outputProcessor;

    public string? OtlpEndpoint { get; set; } = default!;
    public string meterId { get; set; }

    public BatchEval<T> ShowConsoleOutput(bool showConsoleOutput)
    {
        this.showConsoleOutput = showConsoleOutput;
        return this;
    }


    public BatchEval<T> WithInputProcessor(IInputProcessor inputProcessor)
    {
        this.inputProcessor = inputProcessor;
        return this;
    }

    public BatchEval<T> WithOutputProcessor(IOutputProcessor outputProcessor)
    {
        this.outputProcessor = outputProcessor;
        return this;
    }

    public BatchEval<T> WithCsvOutputProcessor(string filename)
    {
        return WithOutputProcessor(new CsvOutputProcessor(filename));
    }

    public BatchEval<T> AddEvaluator(IEvaluator<int> evaluator)
    {
        intEvaluators.Add(evaluator);
        return this;
    }

    public BatchEval<T> AddEvaluator(IEvaluator<bool> evaluator)
    {
        boolEvaluators.Add(evaluator);
        return this;
    }

    public async Task<BatchEvalResults> Run()
    {
        return await ProcessUserInputFile();
    }

    public BatchEval<T> WithJsonl(string fileName)
    {
        this.fileName = fileName;
        return this;
    }

    private async Task<BatchEvalResults> ProcessUserInputFile()
    {
        // if meterid is empty, use meterId = "llm-eval";
        if (string.IsNullOrEmpty(meterId))
        {
            meterId = "llm-eval";
        }

        var meter = CreateMeter(meterId);

        const int BufferSize = 128;
        using (var fileStream = File.OpenRead(fileName!))
        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
        {
            var results = await ProcessFileLines(streamReader, meter);
            return results;
        }
    }

    private EvalMetrics InitCounters(Meter meter)
    {
        var evalMetrics = new EvalMetrics()
        {
            PromptCounter = meter.CreateCounter<int>($"prompt.counter")
        };

        foreach (var evaluator in intEvaluators)
        {
            var histogram = meter.CreateHistogram<int>($"{evaluator.Id.ToLowerInvariant()}.score");
            evalMetrics.ScoreHistograms.Add(evaluator.Id, histogram);
        }

        foreach (var evaluator in boolEvaluators)
        {
            evalMetrics.BooleanCounters.Add(
                $"{evaluator.Id.ToLowerInvariant()}.failure",
                meter.CreateCounter<int>($"{evaluator.Id.ToLowerInvariant()}.failure"));

            evalMetrics.BooleanCounters.Add(
                $"{evaluator.Id.ToLowerInvariant()}.success",
                meter.CreateCounter<int>($"{evaluator.Id.ToLowerInvariant()}.success"));
        }

        return evalMetrics;
    }

    private async Task<BatchEvalResults> ProcessFileLines(
       StreamReader streamReader,
       Meter meter)
    {
        outputProcessor?.Init();

        var results = new BatchEvalResults();

        string? line;
        while ((line = await streamReader.ReadLineAsync()) != null)
        {
            var userInput = System.Text.Json.JsonSerializer.Deserialize<Data.UserInput>(line);

            var singleResults = await ProcessSingle(meter, userInput, inputProcessor, outputProcessor);
            foreach (var evalResult in singleResults.EvalResults)
            {
                results.EvalResults.Add(evalResult);
            }
        }

        return results;
    }

    public async Task<BatchEvalResults> ProcessSingle(
       Meter meter, Data.UserInput userInput, IInputProcessor? inputProcessor, IOutputProcessor? outputProcessor = null)
    {
        var evalMetrics = InitCounters(meter);

        outputProcessor?.Init();

        var results = new BatchEvalResults();

        var modelOutput = await inputProcessor!.Process(userInput!);

        var evalOutput = new BatchEvalPromptOutput()
        {
            Subject = modelOutput
        };

        if (showConsoleOutput)
        {
            Console.WriteLine($"=====================================");
            Console.WriteLine($"Processing Question");
            Console.WriteLine($"Q: {modelOutput.Input}");
            Console.WriteLine($"A: {modelOutput.Output}");
        }

        evalMetrics.PromptCounter.Add(1);

        foreach (var evaluator in intEvaluators)
        {
            var score = await evaluator.Eval(modelOutput);

            if (showConsoleOutput)
                Console.WriteLine($"EVAL: {evaluator.Id.ToLowerInvariant()} SCORE: {score}");

            evalMetrics.ScoreHistograms[evaluator.Id.ToLowerInvariant()].Record(score);
            evalOutput.Results.Add(evaluator.Id.ToLowerInvariant(), score);
        }

        foreach (var evaluator in boolEvaluators)
        {
            var evalResult = await evaluator.Eval(modelOutput);

            if (showConsoleOutput)

                Console.WriteLine($"EVAL: {evaluator.Id.ToLowerInvariant()} RESULT: {evalResult}");

            evalOutput.Results.Add(evaluator.Id.ToLowerInvariant(), evalResult);

            if (evalResult)
            {
                evalMetrics.BooleanCounters[$"{evaluator.Id.ToLowerInvariant()}.success"].Add(1);
            }
            else
            {
                evalMetrics.BooleanCounters[$"{evaluator.Id.ToLowerInvariant()}.failure"].Add(1);
            }
        }

        outputProcessor?.Process(evalOutput);

        results.EvalResults.Add(evalOutput);
        if (showConsoleOutput)
        {
            Console.WriteLine($"=====================================");
            Console.WriteLine();
        }

        return results;
    }

    private Meter CreateMeter(string meterId)
    {
        var builder = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterId);

        foreach (var evaluator in intEvaluators)
        {
            builder.AddView(
                instrumentName: $"{evaluator.Id.ToLowerInvariant()}.score",
                new ExplicitBucketHistogramConfiguration { Boundaries = new double[] { 1, 2, 3, 4, 5 } });
        }

        if (string.IsNullOrEmpty(OtlpEndpoint))
        {
            builder.AddConsoleExporter();
        }
        else
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(OtlpEndpoint);
            });
        }

        builder.AddMeter("Microsoft.SemanticKernel*");

        builder.Build();

        return new Meter(meterId);
    }
}