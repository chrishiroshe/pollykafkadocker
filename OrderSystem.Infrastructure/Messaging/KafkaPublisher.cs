using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Text;

public class KafkaPublisher
{
    private readonly IProducer<string, string> _producer;

    public KafkaPublisher(IConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync(string topic, string key, string payload)
    {
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = payload
        });
    }
}