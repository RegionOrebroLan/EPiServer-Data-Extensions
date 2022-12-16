using System;
using System.Data.SqlClient;
using EPiServer.Data;
using EPiServer.Logging;
using RegionOrebroLan.EPiServer.Data.Extensions.Internal;
using RegionOrebroLan.EPiServer.Data.Hosting;
using RegionOrebroLan.EPiServer.Data.SqlClient.Extensions;

namespace RegionOrebroLan.EPiServer.Data
{
	public class ConnectionStringResolver : IConnectionStringResolver
	{
		#region Constructors

		public ConnectionStringResolver(IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
		{
			this.HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			this.Logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).Create(this.GetType().FullName);
		}

		#endregion

		#region Properties

		protected internal virtual IHostEnvironment HostEnvironment { get; }
		protected internal virtual ILogger Logger { get; }

		#endregion

		#region Methods

		public virtual bool Resolve(ConnectionStringOptions options)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			if(!string.Equals(options.ProviderName, ProviderNames.SqlServer, StringComparison.OrdinalIgnoreCase))
			{
				this.Logger.Information($"ProviderName = {options.ProviderName.ToStringRepresentation()}. Skipping resolve.");

				return false;
			}

			var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(options.ConnectionString);

			var resolved = sqlConnectionStringBuilder.Resolve(this.HostEnvironment);

			if(!resolved)
				return false;

			var resolvedConnectionString = sqlConnectionStringBuilder.ConnectionString;

			this.Logger.Information($"Resolved connection-string {options.Name.ToStringRepresentation()}.");

			options.ConnectionString = resolvedConnectionString;

			return true;
		}

		#endregion
	}
}