#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace LLMEval.Test;

internal static class KernelFactory
{
    public static Kernel CreateAndConfigureKernelTest()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: "llama3",
            endpoint: new Uri("http://localhost:11434"),
            apiKey: "api");

        return builder.Build();
    }

    public static Kernel CreateAndConfigureKernelEval()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            config["AZURE_OPENAI_MODEL"],
            config["AZURE_OPENAI_ENDPOINT"],
            config["AZURE_OPENAI_KEY"]);

        return builder.Build();
    }

}
