using api_node_reservas.ApiKeyAuth;
using api_node_reservas.Data;
using api_node_reservas.ExceptionHandling;
using api_node_reservas.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

/*
================================================================================
|                                  Program                                     |
================================================================================
| Este ficheiro arranca a API.                                                  |
|                                                                              |
| Aqui sao carregadas as configuracoes, registados os servicos, ligados os      |
| DbContexts as bases de dados e ativados o Swagger, autenticacao e controllers.|
================================================================================
*/
DotEnvService.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

/*
================================================================================
|                              Logging                                         |
================================================================================
| Configura onde a API escreve mensagens de log.                                |
================================================================================
*/
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

/*
================================================================================
|                         Connection Strings                                   |
================================================================================
| Le as ligacoes as bases de dados.                                             |
|                                                                              |
| RESERVAS_DB_CONNECTION_STRING aponta para a base de origem.                   |
| KB_DB_CONNECTION_STRING aponta para a base de conhecimento.                   |
================================================================================
*/
string oldConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? string.Empty;

string reservasConnectionString = Environment.GetEnvironmentVariable("RESERVAS_DB_CONNECTION_STRING")
    ?? oldConnectionString;

string knowledgeConnectionString = Environment.GetEnvironmentVariable("KB_DB_CONNECTION_STRING")
    ?? oldConnectionString;

/*
================================================================================
|                           Dependency Injection                               |
================================================================================
| Regista classes que vao ser usadas pelos controllers e services.              |
================================================================================
*/
builder.Services.AddControllers();
builder.Services.AddDbContext<ReservasDbContext>(options => options.UseSqlServer(reservasConnectionString));
builder.Services.AddDbContext<KnowledgeDbContext>(options => options.UseSqlServer(knowledgeConnectionString));
builder.Services.AddSingleton<MappingRepository>();
builder.Services.AddScoped<KnowledgeProcessingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Reservas KB",
        Version = "v1",
        Description = "API para mapear tabelas relacionais de reservas para uma base de conhecimento."
    });

    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Escreve a chave da API. Exemplo: change-this-secret-key",
        Name = "x-api-key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
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

/*
================================================================================
|                              HTTP Pipeline                                   |
================================================================================
| Define a ordem pela qual cada pedido HTTP passa antes de chegar ao controller.|
================================================================================
*/
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
