using BatchEval.Data;

namespace BatchEval.Core;

public interface IInputProcessor
{
    public Task<ModelOutput> ProcessUserInput(UserInput userInput);
    public Task<ModelOutput> ProcessQA(QA qa);
}