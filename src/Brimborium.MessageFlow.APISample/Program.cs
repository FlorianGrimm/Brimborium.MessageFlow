#pragma warning disable IDE0058 // Expression value is never used

using Brimborium.OpenApi.Generator.SwaggerUtils;

namespace Brimborium.MessageFlow.APISample;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        ////builder.Services.AddServiceDefaults();
        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen((swaggerGenOptions) => {
            //swaggerGenOptions.
            swaggerGenOptions.SwaggerDoc("v1", new OpenApiInfo {
                Version = "v1",
                Title = "Brimborium.MessageFlow.API",
                //Description = "An ASP.NET Core Web API for managing ToDo items",
                //TermsOfService = new Uri("https://example.com/terms"),
                //Contact = new OpenApiContact {
                //    Name = "Example Contact",
                //    Url = new Uri("https://example.com/contact")
                //},
                //License = new OpenApiLicense {
                //    Name = "Example License",
                //    Url = new Uri("https://example.com/license")
                //}
            });

            string xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            foreach (var filename in new string[] { xmlFilename, "Brimborium.MessageFlow.xml" }) {
                var xmlFullname = Path.Combine(AppContext.BaseDirectory, filename);
                if (System.IO.File.Exists(xmlFullname)) {
                    swaggerGenOptions.IncludeXmlComments(xmlFullname);
                }
            }

        });
        builder.Services.AddMessageFlow();
        SwaggerDocOptions swaggerDocOptions = new() {
            DocumentName = "v1",
            OutputPath = @"..\Brimborium.MessageFlow.APIServer\OpenApi.json"
        };
        if (Brimborium.OpenApi.Generator.SwaggerUtils.SwaggerGenerator.Generating(builder, swaggerDocOptions)) {
        } else {
            builder.Services.AddHostedService<EngineBackgroundService>();
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseSwagger((swaggerOptions) => {

            });
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapMessageFlow();

        if (Brimborium.OpenApi.Generator.SwaggerUtils.SwaggerGenerator.Generate(app, swaggerDocOptions)) { return; }

        app.Run();
    }
}