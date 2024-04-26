using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;

namespace LLMEval.Core;

public class BatchEval
{
    IList<IEvaluator<int>> intEvaluators = new List<IEvaluator<int>>();

    IList<IEvaluator<bool>> boolEvaluators = new List<IEvaluator<bool>>();

    bool showConsoleOutput = false;

    public string? OtlpEndpoint { get; set; } = default!;
    public string meterId { get; set; }
    public Meter meter { get; set; }
    
    public List<ModelOutput> collectionModelOutputs { get; set; }

    public BatchEval ShowConsoleOutput(bool showConsoleOutput)
    {
        this.showConsoleOutput = showConsoleOutput;
        return this;
    }

    public BatchEval AddEvaluator(IEvaluator<int> evaluator)
    {
        intEvaluators.Add(evaluator);
        return this;
    }

    public BatchEval AddEvaluator(IEvaluator<bool> evaluator)
    {
        boolEvaluators.Add(evaluator);
        return this;
    }
    
    public BatchEval AddModelOutputsCollection(List<ModelOutput> collectionModelOutputs)
    {
        this.collectionModelOutputs = collectionModelOutputs;
        return this;
    }

    public async Task<BatchEvalResults> Run()
    {
        return await ProcessCollection(collectionModelOutputs);
    }

    public BatchEval SetMeterId(string meterId)
    {
        this.meterId = meterId;        
        meter = CreateMeter(meterId);        
        return this;
    }

    private async Task<BatchEvalResults> ProcessCollection(List<ModelOutput> collectionModelOutpus)
    {
        var results = new BatchEvalResults();

        foreach (var item in collectionModelOutpus)
        {
            var singleResults = await ProcessSingle(item);
            foreach (var evalResult in singleResults.EvalResults)
            {
                results.EvalResults.Add(evalResult);
            }
        }

        return results;
    }

    public async Task<BatchEvalResults> ProcessSingle(ModelOutput modelOutput)
    {
        var evalMetrics = InitCounters();
        var results = new BatchEvalResults();      

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
                new ExplicitBucketHistogramConfiguration { Boundaries = [1, 2, 3, 4, 5] });
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

    private EvalMetrics InitCounters()
    {
        if (meter == null)
            CreateMeter(meterId);

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
}