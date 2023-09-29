using Microsoft.EntityFrameworkCore;
using MsCatalog.Data;
using MsCatalog.Services.UriService;
using MsCatalog.Extensions;
using MsCatalog.Listeners.RabbitMQ;
using MsCatalog.Services;

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

builder.Services.AddSingleton<IngredientsStockListener>();
builder.Services.AddSingleton<ProductsSoldListener>();
builder.Services.AddSingleton<StockService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

//Code rajouter pour le CORS 
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.AllowAnyOrigin()
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        });
});
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
if (!builder.Environment.EnvironmentName.Equals("IntegrationTest"))
{
    app.UseRabbitListeners();
}

app.UseAuthorization();

// Using CORS
app.UseCors();

app.MapControllers();

app.Run();

public partial class Program {}
