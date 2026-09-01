namespace Notification.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseNotificationApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        var corsOrigins = app.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (corsOrigins.Length > 0)
            app.UseCors();

        app.MapControllers();
        return app;
    }
}
