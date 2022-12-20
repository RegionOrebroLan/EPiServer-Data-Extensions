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

		public SqlServerLocalDatabaseCreator(IConnectionStringResolver connectionStringResolver, ILoggerFactory loggerFactory)
		{
			this.ConnectionStringResolver = connectionStringResolver ?? throw new ArgumentNullException(nameof(connectionStringResolver));
			this.Logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).Create(this.GetType().FullName);
		}

		#endregion

		#region Properties

		protected internal virtual IConnectionStringResolver ConnectionStringResolver { get; }
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

			// We make a copy. If the connection-string is resolved we only change the copy.
			options = new ConnectionStringOptions
			{
				ConnectionString = string.Copy(options.ConnectionString),
				Name = string.Copy(options.Name),
				ProviderName = string.Copy(options.ProviderName)
			};

			// ReSharper disable ConvertToUsingDeclaration
			using(var context = new DbContext(options.ConnectionString))
			{
				this.Logger.Information($"Ensuring database for connection-string {options.Name.ToStringRepresentation()} is created.");

				this.ConnectionStringResolver.Resolve(options);

				var created = context.Database.CreateIfNotExists();

				this.Logger.Information($"Database for connection-string {options.Name.ToStringRepresentation()} {(created ? null : "already ")}created.");

				return created;
			}
			// ReSharper restore ConvertToUsingDeclaration
		}

		#endregion
	}
}