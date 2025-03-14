using DartWing.Web.Logging.Log4Net;

namespace DartWing.Web.Logging;

public static class LoggingExtensions
{
    public static void AddLogging(this WebApplicationBuilder builder)
    {
        var papertrailSettings = new PapertrailSettings();
        
        builder.Configuration.Bind("PapertrailSettings", papertrailSettings);
        builder.Services.AddSingleton<IPapertrailSettings>(papertrailSettings);
        builder.Logging.AddProvider(new Log4NetProvider("log4net.config", papertrailSettings));
    }
}