using Notification.Application.Abstractions;

namespace Notification.Application.Templating;

public sealed class PlaceholderTemplateRenderer : ITemplateRenderer
{
    public string Render(string template, IReadOnlyDictionary<string, string> data)
    {
        var result = template ?? string.Empty;
        foreach (var pair in data)
            result = result.Replace("{{" + pair.Key + "}}", pair.Value ?? string.Empty, StringComparison.Ordinal);
        return result;
    }
}