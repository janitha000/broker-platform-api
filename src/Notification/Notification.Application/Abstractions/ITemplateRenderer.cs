namespace Notification.Application.Abstractions;

public interface ITemplateRenderer
{
    string Render(string template, IReadOnlyDictionary<string, string> data);
}