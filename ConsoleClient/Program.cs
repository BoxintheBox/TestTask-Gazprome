using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var hubUrl = configuration["SignalR:HubUrl"]
    ?? throw new InvalidOperationException("В appsettings.json не задан SignalR:HubUrl.");

Console.WriteLine($"Hub: {hubUrl}\n");

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .Build();

connection.On<JsonElement>("ProductCreated", (product) =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[SignalR] Продукт создан: {product}");
    Console.ResetColor();
});

connection.On<JsonElement>("ProductUpdated", (product) =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[SignalR] Продукт обновлен: {product}");
    Console.ResetColor();
});

connection.On<Guid>("ProductDeleted", (productId) =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[SignalR] Продукт удален: {productId}");
    Console.ResetColor();
});

try
{
    await connection.StartAsync();
    Console.WriteLine("Подключено к SignalR Hub!\n");
    Console.WriteLine("Ожидание событий... Нажмите любую клавишу для выхода.");
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка подключения: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}
