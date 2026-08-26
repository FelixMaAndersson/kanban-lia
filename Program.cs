
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Repositories;
using kanban_lia.Mappings;
using kanban_lia.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DbConnectionFactory>();

builder.Services.AddScoped<IBoardRepository>();
builder.Services.AddScoped<ColumnRepository>();
builder.Services.AddScoped<PlacementRepository>();

builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<IPlacementService, PlacementService>();

builder.Services.AddAutoMapper(typeof(ColumnProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
