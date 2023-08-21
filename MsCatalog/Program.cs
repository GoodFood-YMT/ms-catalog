using Microsoft.EntityFrameworkCore;
using MsCatalog.Data;
using MsCatalog.Services.UriService;
using MsCatalog.Services.RabbitMqService;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var conn = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApiDbContext>(options => options.UseNpgsql(conn));


if (builder.Environment.EnvironmentName.Equals("IntegrationTest"))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("RedisConfiguration"); ;
    });
}


builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IUriService>(o =>
{
    var accessor = o.GetRequiredService<IHttpContextAccessor>();
    var request = accessor.HttpContext.Request;
    var uri = string.Concat(request.Scheme, "://", request.Host.ToUriComponent());
    return new UriService(uri);
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

    if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        Console.WriteLine("Migrating");
        dbContext.Database.Migrate();        
    }
}

if (!builder.Environment.EnvironmentName.Equals("IntegrationTest"))
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Hostname"],
        UserName = builder.Configuration["RabbitMQ:Username"],
        Password = builder.Configuration["RabbitMQ:Password"]
    };
    var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    var scopeRMQ = app.Services.CreateScope();
    var scopedDbContext = scopeRMQ.ServiceProvider.GetRequiredService<ApiDbContext>();

    RabbitMqService srv = new RabbitMqService(scopedDbContext);

    channel.QueueDeclare("catalog.ingredients.stock", exclusive: false);
    Console.WriteLine("Listening catalog.ingredients.stock queue");
    var consumerStock = new EventingBasicConsumer(channel);
    consumerStock.Received += (model, eventArgs) =>
    {
        var scope = app.Services.CreateScope();
        var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        RabbitMqService srv = new RabbitMqService(scopedDbContext);
        srv.ListenToStockIngredients(eventArgs);
    };
    channel.BasicConsume(queue: "catalog.ingredients.stock", autoAck: true, consumer: consumerStock);

    channel.QueueDeclare("catalog.products.sold", exclusive: false);
    Console.WriteLine("Listening catalog.products.sold queue");
    var consumerSold = new EventingBasicConsumer(channel);
    consumerSold.Received += (model, eventArgs) =>
    {
        srv.ListenToSoldProducts(eventArgs);
    };
    channel.BasicConsume(queue: "catalog.products.sold", autoAck: true, consumer: consumerSold);
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program {}