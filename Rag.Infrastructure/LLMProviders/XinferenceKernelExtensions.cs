using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Rag.Infrastructure.Configuration;

namespace Rag.Infrastructure.LLMProviders;

public static class XinferenceKernelExtensions
{
    public static IKernelBuilder AddXinferenceChatCompletion(this IKernelBuilder builder, XinferenceConfiguration config)
    {
        builder.Services.AddSingleton<IChatCompletionService>(sp =>
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(config.BaseUrl) };
            if (!string.IsNullOrEmpty(config.ApiKey) && config.ApiKey != "EMPTY")
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {config.ApiKey}");
            var logger = sp.GetRequiredService<ILogger<XinferenceChatService>>();
            return new XinferenceChatService(httpClient, config, logger) as IChatCompletionService;
        });
        return builder;
    }
}
