using api_node_reservas.ApiKeyAuth;
using api_node_reservas.Data;
using api_node_reservas.ExceptionHandling;
using api_node_reservas.Services;
using api_node_reservas.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

/*
================================================================================
                              Application startup
================================================================================
 This file configures the API before it starts:
 - Loads environment variables from .env.
 - Connects to the source database and the knowledge database.
 - Registers controllers, services, Swagger, error handling and API key security.
================================================================================
*/
DotEnvService.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

string oldConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? string.Empty;

string reservasConnectionString = Environment.GetEnvironmentVariable("RESERVAS_DB_CONNECTION_STRING")
    ?? oldConnectionString;

string knowledgeConnectionString = Environment.GetEnvironmentVariable("KB_DB_CONNECTION_STRING")
    ?? oldConnectionString;

builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddDbContext<ReservasDbContext>(options => options.UseSqlServer(reservasConnectionString));
builder.Services.AddDbContext<KnowledgeDbContext>(options => options.UseSqlServer(knowledgeConnectionString));
builder.Services.AddSingleton<MappingRepository>();
builder.Services.AddSingleton<OneNoteMappingRepository>();
builder.Services.AddSingleton<OneNoteTokenStore>();
builder.Services.AddHttpClient<MicrosoftGraphAuthService>();
builder.Services.AddHttpClient<OneNoteImportService>();
builder.Services.AddScoped<KnowledgeProcessingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Reservas Knowledge Base API",
        Version = "v1",
        Description = "API for mapping Reservas and OneNote source data into the knowledge database. The Swagger order follows the beginner flow: mappings first, then login/import, then processing."
    });

    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Write the API key. Example: change-this-secret-key",
        Name = "x-api-key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    options.OrderActionsBy(apiDescription =>
    {
        return SwaggerEndpointOrder.GetActionOrder(apiDescription);
    });

    options.TagActionsBy(apiDescription =>
    {
        return new[] { SwaggerEndpointOrder.GetTag(apiDescription) };
    });

    options.AddSecurityRequirement(openApiDocument => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey", openApiDocument),
            []
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
