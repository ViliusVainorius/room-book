using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Configuration;
using RoomBook.Api.Services;
using RoomBook.Infrastructure.Context;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<RoomBookDbContext>(options =>
    options.UseInMemoryDatabase("RoomBookDatabase"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BookingOptions>(
    builder.Configuration.GetSection("Booking"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<RoomUtilizationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<RoomBookDbContext>();

    await DbInitializer.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
