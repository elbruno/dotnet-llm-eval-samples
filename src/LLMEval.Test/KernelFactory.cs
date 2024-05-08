#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace LLMEval.Test;

internal static class KernelFactory
{
    /// <summary>
    /// Creates a new instance of the <see cref="Kernel"/> class for testing purposes.
    /// </summary>
    /// <returns>A new instance of the <see cref="Kernel"/> class.</returns>
    public static Kernel CreatKernelTest()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            "phi3",
            endpoint: new Uri("http://w11-eb20asus-docker-desktop-1:11434"),
            "api");
        return builder.Build();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Kernel"/> class for evaluation purposes.
    /// </summary>
    /// <returns>A new instance of the <see cref="Kernel"/> class.</returns>
    public static Kernel CreateKernelEval()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            config["AZURE_OPENAI_MODEL-GPT3.5"],
            config["AZURE_OPENAI_ENDPOINT"],
            config["AZURE_OPENAI_APIKEY"]);

        return builder.Build();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Kernel"/> class with the necessary configuration for generating data.
    /// </summary>
    /// <returns>A new instance of the <see cref="Kernel"/> class.</returns>
    public static Kernel CreateKernelGenerateData()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            config["AZURE_OPENAI_MODEL-GPT3.5"],
            config["AZURE_OPENAI_ENDPOINT"],
            config["AZURE_OPENAI_APIKEY"]);

        return builder.Build();
    }
}