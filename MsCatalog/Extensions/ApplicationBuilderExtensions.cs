using MsCatalog.Listeners.RabbitMQ;

namespace MsCatalog.Extensions;

public static class ApplicationBuilderExtensions
{
    private static IngredientsStockListener _ingredientsStockListener { get; set; }
    private static ProductsSoldListener _productsSoldListener { get; set; }
    
    public static IApplicationBuilder UseRabbitListeners(this IApplicationBuilder app)
    {
        _ingredientsStockListener = app.ApplicationServices.GetService<IngredientsStockListener>();
        _productsSoldListener = app.ApplicationServices.GetService<ProductsSoldListener>();
        
        var lifeTime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        lifeTime.ApplicationStarted.Register(OnStarted);
        lifeTime.ApplicationStopping.Register(OnStopping);

        return app;
    }

    private static void OnStarted()
    {
        _ingredientsStockListener.Register();
        _productsSoldListener.Register();
    }

    private static void OnStopping()
    {
        _ingredientsStockListener.Deregister();
        _productsSoldListener.Deregister();
    }
}