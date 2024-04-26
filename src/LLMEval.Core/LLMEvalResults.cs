namespace LLMEval.Core;

public class LLMEvalResults
{
    public IList<LLMEvalPromptOutput> EvalResults { get; set; } = 
        new List<LLMEvalPromptOutput>();
}