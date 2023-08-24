using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MsCatalog.Listeners.RabbitMQ;

public abstract class RabbitMQListener
{
    protected ConnectionFactory _factory;
    protected IConnection _connection;
    protected IModel _channel;

    protected string _queueName;

    public RabbitMQListener(string queueName)
    {
        this._queueName = queueName;
        
        this._factory = new ConnectionFactory
        {
            HostName = "event-bus",
            UserName = "guest",
            Password = "guest"
        };
        this._connection = _factory.CreateConnection();
        this._channel = _connection.CreateModel();
    }

    protected abstract Task Handle(string message);

    public void Register()
    {
        // Create the queue if not exist
        Console.WriteLine($"Creating queue {this._queueName}");
        this._channel.QueueDeclare(queue: this._queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        Console.WriteLine($"Queue {this._queueName} created");

        // Create queue listener
        var consumer = new EventingBasicConsumer(this._channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Message received on {this._queueName}");
            Handle(message);
        };
        this._channel.BasicConsume(queue: this._queueName, autoAck: true, consumer: consumer);
        Console.WriteLine($"Listening on queue {this._queueName}");
    }

    public void Deregister()
    {
        this._connection.Close();
    }

}