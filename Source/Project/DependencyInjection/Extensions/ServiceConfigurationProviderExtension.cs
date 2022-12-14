using System;
using EPiServer.Data.SchemaUpdates;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using RegionOrebroLan.EPiServer.Data.Common;
using RegionOrebroLan.EPiServer.Data.Hosting;
using RegionOrebroLan.EPiServer.Data.SchemaUpdates;

namespace RegionOrebroLan.EPiServer.Data.DependencyInjection.Extensions
{
	public static class ServiceConfigurationProviderExtension
	{
		#region Methods

		public static IServiceConfigurationProvider AddData(this IServiceConfigurationProvider services)
		{
			if(services == null)
				throw new ArgumentNullException(nameof(services));

			services.TryAdd<IDbProviderFactories, DbProviderFactoriesWrapper>(ServiceInstanceScope.Singleton);
			services.TryAdd<IHostEnvironment, HostEnvironment>(ServiceInstanceScope.Singleton);
			services.TryAdd(_ => LogManager.LoggerFactory() ?? new TraceLoggerFactory(), ServiceInstanceScope.Singleton);

			services.RemoveAll<ISchemaUpdater>();
			services.AddTransient<ISchemaUpdater, SchemaUpdater>();

			return services;
		}

		#endregion
	}
}