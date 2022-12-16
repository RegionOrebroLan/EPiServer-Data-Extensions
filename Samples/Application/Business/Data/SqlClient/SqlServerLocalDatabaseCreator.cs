using System;
using System.Data.Entity;
using System.Data.SqlClient;
using EPiServer.Data;
using EPiServer.Logging;
using RegionOrebroLan.EPiServer.Data;
using RegionOrebroLan.EPiServer.Data.Extensions.Internal;
using RegionOrebroLan.EPiServer.Data.SqlClient.Extensions;

namespace Application.Business.Data.SqlClient
{
	public class SqlServerLocalDatabaseCreator : IDatabaseCreator
	{
		#region Constructors

		public SqlServerLocalDatabaseCreator(ILoggerFactory loggerFactory)
		{
			this.Logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).Create(this.GetType().FullName);
		}

		#endregion

		#region Properties

		protected internal virtual ILogger Logger { get; }

		#endregion

		#region Methods

		public virtual bool EnsureCreated(ConnectionStringOptions options)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			if(!string.Equals(options.ProviderName, ProviderNames.SqlServer, StringComparison.OrdinalIgnoreCase))
			{
				this.Logger.Information($"The provider-name is {options.ProviderName.ToStringRepresentation()}. Ensure created is skipped.");

				return false;
			}

			var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(options.ConnectionString);

			if(!sqlConnectionStringBuilder.IsLocalDatabaseConnectionString())
			{
				this.Logger.Information($"The connection-string {options.Name.ToStringRepresentation()} is not a local-db connection. Ensure created is skipped.");

				return false;
			}

			// ReSharper disable ConvertToUsingDeclaration
			using(var context = new DbContext(options.ConnectionString))
			{
				this.Logger.Information($"Ensuring database for connection-string {options.Name.ToStringRepresentation()} is created.");

				var created = context.Database.CreateIfNotExists();

				this.Logger.Information($"Database for connection-string {options.Name.ToStringRepresentation()} {(created ? null : "already ")}created.");

				return created;
			}
			// ReSharper restore ConvertToUsingDeclaration
		}

		#endregion
	}
}