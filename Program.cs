using api_node_reservas.ApiKeyAuth;
using api_node_reservas.Data;
using api_node_reservas.ExceptionHandling;
using api_node_reservas.Services;
using api_node_reservas.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

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

// ---------------------------------------------------------------------------
// Beginner note: this file wires together everything the application needs.
// Think of it as the "main" that prepares the app and then runs it.
// - Load configuration (.env)
// - Register services (dependency injection)
// - Configure middleware (error handling, authentication)
// - Start listening for HTTP requests (app.Run())
// ---------------------------------------------------------------------------

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

// Run a small, safe migration at startup to copy any existing values from
// the old `parent` column into `location`, then drop the `parent` column.
// This keeps the database consistent with the code change that removed the
// Parent property from the Context model. The SQL runs only if the column
// exists and errors are caught and logged so the application still starts.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        var sql = @"IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'parent' AND Object_ID = OBJECT_ID(N'dbo.[Context]'))
BEGIN
    UPDATE [Context] SET [location] = [parent] WHERE [parent] IS NOT NULL;
    ALTER TABLE [Context] DROP COLUMN [parent];
END";

        db.Database.ExecuteSqlRaw(sql);
        logger.LogInformation("Context: copied parent -> location and dropped parent column if it existed.");
    }
    catch (Exception ex)
    {
        // If anything fails here, we log and continue; the app should not be
        // prevented from starting because of this one-time migration.
        var logger2 = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger2.LogWarning(ex, "Parent->location migration skipped: {Message}", ex.Message);
    }
}

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
