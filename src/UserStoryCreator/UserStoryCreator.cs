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
            var modelOutput = await Process(userInput);
            result.Add(modelOutput);
        }
        return result;
    }

    public async Task<ModelOutput> Process<T>(T source)
    {
        var userInput = source as UserInput;

        var userStory = await userStoryGenerator.GetUserStory(
            userInput.Description,
            userInput.ProjectContext,
            userInput.Persona);

        return new ModelOutput() {
            Input = $"Generate a user story for {userInput.Persona} so it can {userInput.Description}",
            Output = $"{userStory!.Title} - {userStory!.Description}"
        };
    }
}