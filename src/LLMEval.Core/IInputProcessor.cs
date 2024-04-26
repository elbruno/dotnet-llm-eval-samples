using LLMEval.Data;

namespace LLMEval.Core;

public interface IInputProcessor
{
    public Task<ModelOutput> Process<T>(T userInput);
    //public Task<ModelOutput> ProcessUserInput(UserInput userInput);
    //public Task<ModelOutput> ProcessQA(QA qa);
}