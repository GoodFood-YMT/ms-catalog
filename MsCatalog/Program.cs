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
    Console.WriteLine(builder.Configuration["RabbitMQ:Hostname"]);
    Console.WriteLine(builder.Configuration["RabbitMQ:Username"]);
    Console.WriteLine(builder.Configuration["RabbitMQ:Username"]);
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Hostname"],
        UserName = builder.Configuration["RabbitMQ:Username"],
        Password = builder.Configuration["RabbitMQ:Password"]
    };

    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();
    try {
        var scopeRMQ = app.Services.CreateScope();
        var scopedDbContext = scopeRMQ.ServiceProvider.GetRequiredService<ApiDbContext>();

        RabbitMqService srv = new RabbitMqService(scopedDbContext);

        // Ingredient Stock
        channel.QueueDeclare("catalog.ingredients.stock", durable: true, autoDelete: false);
        Console.WriteLine("Listening catalog.ingredients.stock queue");
        var consumerStock = new EventingBasicConsumer(channel);
        consumerStock.Received += (model, eventArgs) =>
        {
            Console.WriteLine("Received" + eventArgs);
            srv.ListenToStockIngredients(eventArgs);
        };
        channel.BasicConsume(queue: "catalog.ingredients.stock", autoAck: true, consumer: consumerStock);

        // Product Stock
        channel.QueueDeclare("catalog.products.sold", durable: true, autoDelete: false);
        Console.WriteLine("Listening catalog.products.sold queue");
        var consumerSold = new EventingBasicConsumer(channel);
        consumerSold.Received += (model, eventArgs) =>
        {
            Console.WriteLine("Received" + eventArgs);
            srv.ListenToSoldProducts(eventArgs);
        };
        channel.BasicConsume(queue: "catalog.products.sold", autoAck: true, consumer: consumerSold);
    }
    catch(Exception e) {
        Console.WriteLine("Error while doing RabbitMQ stuff : " + e.Message);
    }
    finally {
        if (connection != null)
            connection.Dispose();
        
        if (channel != null)
            channel.Dispose();
    }
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
