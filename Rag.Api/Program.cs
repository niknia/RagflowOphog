using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Rag.Infrastructure.Extensions;
using Rag.Api.Middleware;
using Rag.Api.Filters;
using Rag.Api.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/rag-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Services
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLoggingFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "RAG Platform API",
        Version = "v1",
        Description = "Enterprise Hybrid RAG (Retrieval-Augmented Generation) platform with multilingual support. Supports vector + keyword hybrid search, Persian/English documents, and Semantic Kernel orchestration.",
        Contact = new OpenApiContact
        {
            Name = "RAG Platform Team",
            Email = "support@ragplatform.local"
        },
        License = new OpenApiLicense
        {
            Name = "Internal Use Only"
        }
    });

    c.OrderActionsBy(a => a.RelativePath);

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

builder.Services.AddSignalR();

builder.Services.AddRagInfrastructure(builder.Configuration);
builder.Services.AddRagApplication();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Rag.Infrastructure.Persistence.RagDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<ProcessingHub>("/hub/progress");

try
{
    Log.Information("Starting RAG Platform API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
