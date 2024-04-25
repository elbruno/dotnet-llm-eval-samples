using BatchEval.Data;

namespace BatchEval.Core;

public interface IInputProcessor
{
    public Task<ModelOutput> Process(BatchEval.Data.UserInput userInput);    
}