using System;
using EPiServer.Data;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using RegionOrebroLan.EPiServer.Data;

namespace Application.Business.Initialization
{
	[InitializableModule]
	public class DataInitialization : IInitializableModule
	{
		#region Methods

		public virtual void Initialize(InitializationEngine context)
		{
			if(context == null)
				throw new ArgumentNullException(nameof(context));

			var databaseCreator = context.Locate.Advanced.GetInstance<IDatabaseCreator>();

			if(databaseCreator == null)
				return;

			var dataAccessOptions = context.Locate.Advanced.GetInstance<DataAccessOptions>();

			foreach(var options in dataAccessOptions.ConnectionStrings)
			{
				databaseCreator.EnsureCreated(options);
			}
		}

		public virtual void Uninitialize(InitializationEngine context) { }

		#endregion
	}
}