// Composition root of the kanban-lia web API: registers services, configures JSON and exception handling, and maps
// the endpoint groups and the SignalR hub the front-end uses.
//
// This is a top-level-statements file, so it declares no types or members. Repositories and services are registered
// scoped around a singleton DbConnectionFactory; an outbox worker publishes committed integration events to RabbitMQ,
// and AutoMapper profiles map between requests, DTOs and domain objects. The HTTP JSON options register the strongly
// typed identifier converters so ids travel over the API as GUID strings. A global exception handler maps known domain
// exceptions and SQL Server constraint errors to problem responses (for example not-found to 404,
// ColumnNotWritableException to 403 and duplicate-key errors to 409) and everything else to 500. Swagger is enabled in
// development only; CORS allows the local front-end origin; the board hub is served at /hubs/board and the board,
// column, placement and column-edge endpoint groups are mapped before the app runs.
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;

using kanban_lia.Endpoints.Boards;
using kanban_lia.Endpoints.Columns;
using kanban_lia.Endpoints.Placements;
using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.JsonConverters;
using kanban_lia.Infrastructure.Messaging;
using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Outbox;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.IntegrationEvents;
using kanban_lia.Services.Placements;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DbConnectionFactory>();

builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IColumnRepository, ColumnRepository>();
builder.Services.AddScoped<IPlacementRepository, PlacementRepository>();
builder.Services.AddScoped<IColumnEdgeRepository, ColumnEdgeRepository>();

builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<IPlacementService, PlacementService>();
builder.Services.AddScoped<IColumnEdgeService, ColumnEdgeService>();

builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(Program).Assembly);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddSignalR();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new EntityIdJsonConverter());
    options.SerializerOptions.Converters.Add(new BoardIdJsonConverter());
    options.SerializerOptions.Converters.Add(new ColumnIdJsonConverter());
});

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

var app = builder.Build();

var exceptionLogger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("GlobalExceptionHandler");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()
            ?.Error;

        var statusCode = exception switch
        {
            BoardNotFoundException => StatusCodes.Status404NotFound,
            RootNotFoundException => StatusCodes.Status404NotFound,
            RootAlreadyExistsException => StatusCodes.Status400BadRequest,
            ColumnNotFoundException => StatusCodes.Status404NotFound,
            ColumnNotWritableException => StatusCodes.Status403Forbidden,
            ColumnEdgeAlreadyExistsException => StatusCodes.Status400BadRequest,
            InvalidDomainException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            SqlException { Number: 547 } => StatusCodes.Status409Conflict,
            SqlException { Number: 2627 } => StatusCodes.Status409Conflict,
            SqlException { Number: 2601 } => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var isUnexpected = statusCode == StatusCodes.Status500InternalServerError;

        if (isUnexpected)
        {
            exceptionLogger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode = statusCode;

        string? detail = statusCode switch
        {
            StatusCodes.Status500InternalServerError => null,
            _ when exception is SqlException => "The operation could not be completed due to a data conflict.",
            _ => exception?.Message
        };

        await Results.Problem(
            statusCode: statusCode,
            title: exception switch
            {
                BoardNotFoundException => "Board could not be found",
                RootNotFoundException => "Root could not be found",
                RootAlreadyExistsException => "Root already exists on this board",
                ColumnNotFoundException => "Column could not be found",
                ColumnNotWritableException => "Column is not writable",
                ColumnEdgeAlreadyExistsException => "Column edge already exists",
                InvalidDomainException => "Invalid request",
                ArgumentException => "Invalid request parameters",
                SqlException { Number: 547 } => "Operation conflicts with related records",
                SqlException { Number: 2627 } or SqlException { Number: 2601 } => "A record with the same key already exists",
                _ => "An unexpected error occurred"
            },
            detail: detail
        ).ExecuteAsync(context);
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapHub<BoardHub>("/hubs/board");

BoardEndpoints.MapBoardEndpoints(app);
ColumnEndpoints.MapColumnEndpoints(app);
PlacementEndpoints.MapPlacementEndpoints(app);
ColumnEdgeEndpoints.MapColumnEdgeEndpoints(app);

await app.RunAsync();
