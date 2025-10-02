using System.Collections.Concurrent;
using System.Xml;

namespace DartWing.Web.Logging.Log4Net;

[ProviderAlias("Papertrail")]
public class Log4NetProvider : ILoggerProvider
{
    private readonly string _log4NetConfigFile;
    private readonly IPapertrailSettings _settings;

    private readonly ConcurrentDictionary<string, Log4NetLogger> _loggers = new();

    public Log4NetProvider(string log4NetConfigFile, IPapertrailSettings settings)
    {
        _log4NetConfigFile = log4NetConfigFile;
        _settings = settings;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private Log4NetLogger CreateLoggerImplementation(string name)
    {
        return new Log4NetLogger(name, Parselog4NetConfigFile(_log4NetConfigFile, _settings));
    }

    private static XmlElement Parselog4NetConfigFile(string filename, IPapertrailSettings settings)
    {
        var configXml = File.ReadAllText(filename);
        configXml = configXml.Replace("{HostName}", settings.HostName).Replace("{AppName}", settings.AppName);

        var log4NetConfig = new XmlDocument();
        log4NetConfig.LoadXml(configXml);
        return log4NetConfig["log4net"];
    }
}