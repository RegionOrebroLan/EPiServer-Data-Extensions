using System;
using System.Collections.Generic;
using System.IO;
using EPiServer.Data;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Configuration;
using Moq;
using RegionOrebroLan.EPiServer.Data.DependencyInjection.Extensions;
using RegionOrebroLan.EPiServer.Data.Hosting;

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

			context.Services.AddData();

			context.Services.RemoveAll<IHostEnvironment>();
			context.Services.AddSingleton<IHostEnvironment>(new HostEnvironment { ContentRootPath = Global.ProjectDirectoryPath });

			context.Services.AddSingleton(Mock.Of<IVirtualRoleReplication>());

			this.ConfigureConnectionSettings(context.Services);
		}

		public virtual void Initialize(InitializationEngine context) { }
		public virtual void Uninitialize(InitializationEngine context) { }

		#endregion
	}
}