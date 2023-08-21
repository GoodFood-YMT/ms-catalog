using RabbitMQ.Client.Events;

namespace MsCatalog.Services.RabbitMqService
{
    public interface IRabbitMqService
    {

        public void ListenToStockIngredients(BasicDeliverEventArgs eventArgs);
        public void ListenToSoldProducts(BasicDeliverEventArgs eventArgs);
    }
}
