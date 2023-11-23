namespace Microsoft.Extensions.DependencyInjection;

public static class MessageFlowServiceCollectionExtensions {
    public static IServiceCollection AddMessageFlow(
        this IServiceCollection services,
        Action<MessageFlowAPIServiceOption, IServiceProvider>? configure = default
        ) {
        // option
        var optionsBuilder = services.AddOptions<MessageFlowAPIServiceOption>();
        if (optionsBuilder is not null && configure is not null) {
            _ = optionsBuilder.Configure(configure);
        }

        // service
        services.AddSingleton<IMessageFlowAPIService, MessageFlowAPIService>();
        services.Configure<JsonOptions>(options => {
            options.SerializerOptions.Converters.Add(new NodeIdentifierJsonConverter());
        });
        services.Configure<SwaggerGenOptions>((swaggerGenOptions) => {
            swaggerGenOptions.MapType<NodeIdentifier>(() => {
                return new Microsoft.OpenApi.Models.OpenApiSchema() { Type = "string" };
            });
        });
        return services;
    }
}
