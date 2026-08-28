using kanban_lia.Endpoints.Boards;
using kanban_lia.Endpoints.Columns;
using kanban_lia.Endpoints.Placements;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.Placements;

using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DbConnectionFactory>();

builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IColumnRepository, ColumnRepository>();
builder.Services.AddScoped<IPlacementRepository, PlacementRepository>();

builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<IPlacementService, PlacementService>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(Program).Assembly);
});

var app = builder.Build();

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
            InvalidDomainException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        await Results.Problem(
            statusCode: statusCode,
            title: exception switch
            {
                BoardNotFoundException => "Board could not be found",
                RootNotFoundException => "Root could not be found",
                RootAlreadyExistsException => "Root already exists on this board",
                ColumnNotFoundException => "Column could not be found",
                InvalidDomainException => "Invalid request",
                _ => "Internal server error"
            },
            detail: statusCode == StatusCodes.Status500InternalServerError
                ? "Ett internt serverfel inträffade."
                : exception?.Message
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

BoardEndpoints.MapBoardEndpoints(app);
ColumnEndpoints.MapColumnEndpoints(app);
PlacementEndpoints.MapPlacementEndpoints(app);

await app.RunAsync();
