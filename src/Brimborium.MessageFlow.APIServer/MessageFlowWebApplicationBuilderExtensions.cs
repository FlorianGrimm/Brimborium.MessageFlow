namespace Microsoft.AspNetCore.Builder;

public static class MessageFlowWebApplicationBuilderExtensions {
    public static WebApplicationBuilder AddMessageFlow(
        this WebApplicationBuilder builder,
        Action<MessageFlowAPIServiceOption, IServiceProvider>? configure = default
        ) {
        builder.Services.AddMessageFlow(configure);
        return builder;
    }
}
