namespace Notification.Infrastructure.Messaging;

public sealed class MassTransitOptions
{
    public const string SectionName = "MassTransit";

    public string Transport { get; set; } = "InMemory";
    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public bool UseRabbitMq =>
        string.Equals(Transport, "RabbitMq", StringComparison.OrdinalIgnoreCase);
}
