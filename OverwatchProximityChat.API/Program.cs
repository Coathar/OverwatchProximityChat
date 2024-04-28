
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace OverwatchProximityChat.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<WorkshopLogReader>();
            builder.Services.AddHostedService(provider => provider.GetService<WorkshopLogReader>());
            builder.Services.AddSingleton<VicreoManager>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/get-player/{linkCode}",
                (string linkCode, HttpContext httpContext,
                [FromServices]WorkshopLogReader workshopParser) =>
            {
                Models.Player? player = workshopParser.TryGetPlayer(linkCode);
                if (player == null)
                {
                    return Results.NotFound("Player not found");
                }

                return Results.Json(player);
            })
            .WithName("GetPlayer")
            .WithOpenApi();

            app.Run();


        }
    }
}
