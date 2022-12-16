using System;
using Application.Business.Data.SqlClient;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using RegionOrebroLan.EPiServer.Data;
using RegionOrebroLan.EPiServer.Data.DependencyInjection.Extensions;

namespace Application.Business.Initialization
{
	[ModuleDependency(typeof(EPiServer.Data.DataInitialization))]
	public class ServiceRegistration : IConfigurableModule
	{
		#region Methods

		public virtual void ConfigureContainer(ServiceConfigurationContext context)
		{
			if(context == null)
				throw new ArgumentNullException(nameof(context));

			context.Services.AddData();

			context.Services.AddSingleton<IDatabaseCreator, SqlServerLocalDatabaseCreator>();
		}

		public virtual void Initialize(InitializationEngine context) { }
		public virtual void Uninitialize(InitializationEngine context) { }

		#endregion
	}
}