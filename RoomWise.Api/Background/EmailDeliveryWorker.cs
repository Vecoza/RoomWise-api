using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RoomWise.Model.Messaging;
using RoomWise.Model.Options;

namespace RoomWise.Api.Background;


public sealed class EmailDeliveryWorker : BackgroundService
{
    private readonly RabbitMqOptions _rabbit;
    private readonly SmtpOptions _smtp;
    private readonly ILogger<EmailDeliveryWorker> _logger;

    public EmailDeliveryWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<EmailDeliveryWorker> logger)
    {
        _rabbit = rabbitOptions.Value;
        _smtp = smtpOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer crashed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbit.HostName,
            Port = _rabbit.Port,
            UserName = _rabbit.UserName,
            Password = _rabbit.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: _rabbit.EmailQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<EmailMessage>(json);
                if (message is null)
                    throw new InvalidOperationException("Failed to deserialize EmailMessage.");

                await SendEmailAsync(message, ct);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email message.");

                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(
            queue: _rabbit.EmailQueueName,
            autoAck: false,
            consumer: consumer);


        while (!ct.IsCancellationRequested)
            await Task.Delay(1000, ct);
    }

    private async Task SendEmailAsync(EmailMessage msg, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host) ||
            string.IsNullOrWhiteSpace(_smtp.UserName) ||
            string.IsNullOrWhiteSpace(_smtp.Password) ||
            string.IsNullOrWhiteSpace(_smtp.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are not configured.");
        }

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password)
        };

        var mail = new MailMessage
        {
            From = new MailAddress(_smtp.FromEmail, _smtp.FromDisplayName),
            Subject = msg.Subject,
            Body = msg.Body,
            IsBodyHtml = msg.IsHtml
        };
        mail.To.Add(msg.To);

        await client.SendMailAsync(mail, ct);
    }
}
