using UserStoryGenerator;
using LLMEval.Core;
using Microsoft.SemanticKernel;
using LLMEval.Data;

namespace UserStoryCreator;

public class UserStoryCreator : IInputProcessor
{
    private readonly UserStorySkill userStoryGenerator;

    public UserStoryCreator(Kernel kernel)
    {
        userStoryGenerator = UserStorySkill.Create(kernel);
    }

    public async Task<List<ModelOutput>> ProcessUserInputCollection(List<UserInput> userInputs)
    { 
        var result = new List<ModelOutput>();        
        foreach (var userInput in userInputs)
        {
            var modelOutput = await ProcessUserInput(userInput);
            result.Add(modelOutput);
        }
        return result;
    }

    public async Task<ModelOutput> ProcessUserInput(UserInput userInput)
    {
        var userStory = await userStoryGenerator.GetUserStory(
            userInput.Description,
            userInput.ProjectContext,
            userInput.Persona);

        return new ModelOutput() {
            Input = $"Generate a user story for {userInput.Persona} so it can {userInput.Description}",
            Output = $"{userStory!.Title} - {userStory!.Description}"
        };
    }

    public Task<ModelOutput> ProcessQA(QA qa)
    {
        throw new NotImplementedException();
    }
}