using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Configuration;
using RoomBook.Api.DTOs;
using RoomBook.Api.Services;
using RoomBook.Infrastructure.Context;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var violations = context.ModelState
            .SelectMany(entry => entry.Value!.Errors.Select(error => new
            {
                entry.Key,
                Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.Exception?.Message
                    : error.ErrorMessage
            }))
            .Where(violation => !string.IsNullOrWhiteSpace(violation.Message))
            .ToList();

        var message = violations.Count > 0
            ? string.Join(" ", violations.Select(v => $"{v.Key}: {v.Message}"))
            : "The request payload is invalid.";

        return new BadRequestObjectResult(
            new ApiErrorResponse("VALIDATION_ERROR", message));
    };
});

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddDbContext<RoomBookDbContext>(options =>
    options.UseInMemoryDatabase("RoomBookDatabase"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BookingOptions>(
    builder.Configuration.GetSection("Booking"));

builder.Services.Configure<UtilizationReportOptions>(
    builder.Configuration.GetSection("UtilizationReport"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<RoomUtilizationService>();
builder.Services.AddScoped<RoomUsageReportPdfService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<RoomBookDbContext>();

    await DbInitializer.SeedAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.MapControllers();

app.Run();

public partial class Program;
