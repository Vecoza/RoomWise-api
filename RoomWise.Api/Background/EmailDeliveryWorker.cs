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

/// <summary>
/// Listens to RabbitMQ "email_notifications" queue and sends emails via SMTP.
/// </summary>
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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => RunConsumer(stoppingToken), stoppingToken);
    }

    private void RunConsumer(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbit.HostName,
            Port = _rabbit.Port,
            UserName = _rabbit.UserName,
            Password = _rabbit.Password,
            DispatchConsumersAsync = true
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
                // do not requeue to avoid infinite loop
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(
            queue: _rabbit.EmailQueueName,
            autoAck: false,
            consumer: consumer);

        // Keep the loop alive until cancellation
        while (!ct.IsCancellationRequested)
        {
            Thread.Sleep(1000);
        }
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
            IsBodyHtml = false
        };
        mail.To.Add(msg.To);

        await client.SendMailAsync(mail, ct);
    }
}
