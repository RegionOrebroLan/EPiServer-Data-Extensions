using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using EPiServer.Data;
using EPiServer.Data.SchemaUpdates;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Configuration;
using Moq;
using RegionOrebroLan;
using RegionOrebroLan.Data;
using RegionOrebroLan.Data.Common;
using RegionOrebroLan.EPiServer.Data.SchemaUpdates;

namespace IntegrationTests.Helpers
{
	[InitializableModule]
	public class ServiceConfiguration : IConfigurableModule
	{
		#region Methods

		protected internal virtual void ConfigureConnectionSettings(IServiceConfigurationProvider services)
		{
			if(services == null)
				throw new ArgumentNullException(nameof(services));

			var connectionSettings = new Dictionary<string, ConnectionStringOptions>();
			var configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(Global.ProjectDirectoryPath, "Connections.json")).Build();
			var dataAccessOptions = new DataAccessOptions();

			foreach(var item in configuration.GetSection("Connections").GetChildren())
			{
				var connectionSetting = new ConnectionStringOptions
				{
					ConnectionString = item.GetSection("ConnectionString").Value,
					Name = item.Key,
					ProviderName = item.GetSection("ProviderName").Value
				};

				dataAccessOptions.ConnectionStrings.Add(connectionSetting);
				services.AddSingleton(connectionSetting);
				connectionSettings.Add(item.Key, connectionSetting);
			}

			services.AddSingleton(dataAccessOptions);
			services.AddSingleton<IDictionary<string, ConnectionStringOptions>>(connectionSettings);
		}

		public virtual void ConfigureContainer(ServiceConfigurationContext context)
		{
			if(context == null)
				throw new ArgumentNullException(nameof(context));

			context.Services.AddSingleton(AppDomain.CurrentDomain);
			context.Services.AddSingleton<IApplicationDomain, AppDomainWrapper>();
			context.Services.AddSingleton<IConnectionStringBuilderFactory, ConnectionStringBuilderFactory>();
			context.Services.AddSingleton<IDatabaseManagerFactory, DatabaseManagerFactory>();
			context.Services.AddSingleton<IFileSystem, FileSystem>();
			context.Services.AddSingleton<IProviderFactories, DbProviderFactoriesWrapper>();

			context.Services.RemoveAll<ISchemaUpdater>();
			context.Services.AddTransient<ISchemaUpdater, SchemaUpdater>();

			context.Services.AddSingleton(Mock.Of<IVirtualRoleReplication>());

			this.ConfigureConnectionSettings(context.Services);
		}

		public virtual void Initialize(InitializationEngine context) { }
		public virtual void Uninitialize(InitializationEngine context) { }

		#endregion
	}
}