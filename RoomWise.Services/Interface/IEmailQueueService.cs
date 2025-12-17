using RoomWise.Model.Messaging;

namespace RoomWise.Services.Interface;

public interface IEmailQueueService
{
    Task PublishAsync(EmailMessage message, CancellationToken ct = default);
}
