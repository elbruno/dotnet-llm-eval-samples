using LLMEval.Core;
using LLMEval.Data;
using Microsoft.SemanticKernel;
using QAGenerator;

namespace QACreator;

public class QACreator : IInputProcessor
{
    private readonly QASkill qaGenerator;

    public QACreator(Kernel kernel)
    {
        qaGenerator = QASkill.Create(kernel);
    }

    public async Task<ModelOutput> ProcessQA(LLMEval.Data.QA sourceQA)
    {
        var modelResponse = await qaGenerator.GetQA(sourceQA.Question, sourceQA.Answer);
    
        return new ModelOutput()
        {
            Input = $@"The model was asked this question: ""{sourceQA.Question}"", expecting this answer: ""{sourceQA.Answer}""",
            Output = $@"The model answer is: ""{modelResponse.Answer}"""
        };
    }

    public Task<ModelOutput> ProcessUserInput(LLMEval.Data.UserInput userInput)
    {
        throw new NotImplementedException();
    }
}