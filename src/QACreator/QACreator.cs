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

    public async Task<ModelOutput> Process<T>(T source)
    {
        var qa = source as LLMEval.Data.QA;

        var modelResponse = await qaGenerator.GetQA(qa.Question, qa.Answer);
    
        return new ModelOutput()
        {
            Input = $@"The model was asked this question: ""{qa.Question}"", expecting this answer: ""{qa.Answer}""",
            Output = $@"The model answer is: ""{modelResponse.Answer}"""
        };
    }
}