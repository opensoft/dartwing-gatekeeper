namespace DartWing.Web.Logging;

public interface IPapertrailSettings
{
    string AppName { get; }
    string HostName { get; }
}

public class PapertrailSettings : IPapertrailSettings
{
    public string HostName { get; set; }
    public string AppName { get; set; }
}