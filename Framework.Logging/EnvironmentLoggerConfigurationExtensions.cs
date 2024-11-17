using Serilog.Configuration;
using Serilog.Enrichers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog;

public static class EnvironmentLoggerConfigurationExtensions
{
	/// <summary>
	/// Enrich log events with a EnvironmentName property containing the value of the <code>ASPNETCORE_ENVIRONMENT</code>
	/// or <code>DOTNET_ENVIRONMENT</code> environment variable.
	/// </summary>
	/// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
	/// <returns>Configuration object allowing method chaining.</returns>
	public static LoggerConfiguration WithEnvironmentName(
		this LoggerEnrichmentConfiguration enrichmentConfiguration)
	{
		if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));
		return enrichmentConfiguration.With<InvocationContextEnricher>();
	}

}
