namespace LLMEval.Core;

public class BatchEvalResults
{
    public IList<BatchEvalPromptOutput> EvalResults { get; set; } = 
        new List<BatchEvalPromptOutput>();
}