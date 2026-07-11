using Microsoft.Extensions.Configuration;
using Serilog;

namespace ProjectNamePlaceholder.Infrastructure.Logging;

public static class SerilogConfigurationExtensions
{
    public static LoggerConfiguration AddApplicationSinks(this LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var configuredLogger = loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);

//#if (enableSeq)
        var enableSeq = configuration.GetValue<bool?>("Serilog:EnableSeq") ?? true;
        if (enableSeq)
        {
            configuredLogger = configuredLogger.WriteTo.Seq(configuration["Serilog:SeqServerUrl"] ?? "http://localhost:5341");
        }
//#endif

        return configuredLogger;
    }
}
