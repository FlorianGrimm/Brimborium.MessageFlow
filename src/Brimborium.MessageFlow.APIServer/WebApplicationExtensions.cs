namespace Brimborium.MessageFlow.APIServer;

public static class WebApplicationExtensions {
    public static WebApplication MapMessageFlow(
        this WebApplication app
        ) {
        var group = app.MapGroup("api/messageflow");
        group.MapGet(
            "/names",
            (HttpContext httpContext) => {
                var messageFlowAPIService = httpContext.RequestServices.GetRequiredService<IMessageFlowAPIService>();
                var result = messageFlowAPIService.GetEngineNames();
                return result;
            })
            .WithName("GetListMessageFlowName")
            .WithOpenApi((openApiOperation) => {
                openApiOperation.Description = "Get a List of MessageFlow Names";
                return openApiOperation;
            });

        group.MapGet(
            "/running/{name}/graph",
            Results<Ok<MessageFlowGraph>, NotFound> (string name, HttpContext httpContext) => {
                var messageFlowAPIService = httpContext.RequestServices.GetRequiredService<IMessageFlowAPIService>();
                var result = messageFlowAPIService.GetEngineGraph(name);

                if (result is null) {
                    return TypedResults.NotFound();
                } else {
                    return TypedResults.Ok(result);
                }
            })
            .WithName("GetMessageFlowGraph")
            .WithOpenApi();
        return app;
    }

#if false
    public static IEndpointRouteBuilder MapMessageFlowGraph(
        //this WebApplication app,
        this IEndpointRouteBuilder endpoints,
        //[StringSyntax("Route")] string pattern,
        string name,
        Func<string, IServiceProvider, IMessageEngine?> getEngine
        ) {
        endpoints.MapGet("/graph", (HttpContext httpContext) => {
            var engine = getEngine(name, httpContext.RequestServices);
            if (engine is null) { return Results.NotFound(); }

            var result = engine.ToMessageFlowGraph();
            return Results.Json<MessageFlowGraph>(result ?? new());
        });
        return endpoints;
    }
#endif
}
