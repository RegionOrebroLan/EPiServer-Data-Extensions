using System;
using System.Data.SqlClient;
using System.IO;
using RegionOrebroLan.EPiServer.Data.Extensions.Internal;
using RegionOrebroLan.EPiServer.Data.Hosting;
using RegionOrebroLan.EPiServer.Data.IO.Extensions;

namespace RegionOrebroLan.EPiServer.Data.SqlClient.Extensions
{
	public static class SqlConnectionStringBuilderExtension
	{
		#region Fields

		public const string LocalDatabasePrefix = "(LocalDb)";

		#endregion

		#region Methods

		public static bool IsLocalDatabaseConnectionString(this SqlConnectionStringBuilder sqlConnectionStringBuilder)
		{
			if(sqlConnectionStringBuilder == null)
				throw new ArgumentNullException(nameof(sqlConnectionStringBuilder));

			return sqlConnectionStringBuilder.DataSource.StartsWith(LocalDatabasePrefix, StringComparison.OrdinalIgnoreCase);
		}

		public static bool Resolve(this SqlConnectionStringBuilder sqlConnectionStringBuilder, IHostEnvironment hostEnvironment)
		{
			if(sqlConnectionStringBuilder == null)
				throw new ArgumentNullException(nameof(sqlConnectionStringBuilder));

			if(hostEnvironment == null)
				throw new ArgumentNullException(nameof(hostEnvironment));

			if(!sqlConnectionStringBuilder.IsLocalDatabaseConnectionString())
				return false;

			var attachDbFilename = sqlConnectionStringBuilder.AttachDBFilename;

			if(string.IsNullOrWhiteSpace(attachDbFilename))
				return false;

			string fullAttachDbFilename;

			if(attachDbFilename.StartsWith(DataDirectory.Substitution))
			{
				if(AppDomain.CurrentDomain.GetData(DataDirectory.Key) is not string dataDirectoryPath)
					throw new InvalidOperationException($"The connection-string contains \"{nameof(SqlConnectionStringBuilder.AttachDBFilename)}={attachDbFilename}\" but the AppDomain does not have the {DataDirectory.Key.ToStringRepresentation()}-key set. You need to set the {DataDirectory.Key.ToStringRepresentation()}-key: AppDomain.CurrentDomain.SetData({DataDirectory.Key.ToStringRepresentation()}, {@"C:\Directory".ToStringRepresentation()}).");

				if(!Directory.Exists(dataDirectoryPath))
					throw new InvalidOperationException($"The directory-path {dataDirectoryPath.ToStringRepresentation()}, set as {DataDirectory.Key.ToStringRepresentation()}-key for the AppDomain, does not exist.");

				var resolvedDataDirectoryPath = dataDirectoryPath.TrimEnd(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
				var resolvedAttachDbFilename = attachDbFilename.Substring(DataDirectory.Substitution.Length).TrimStart(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

				fullAttachDbFilename = Path.Combine(resolvedDataDirectoryPath, resolvedAttachDbFilename);
			}
			else
			{
				fullAttachDbFilename = PathExtension.GetFullPath(attachDbFilename, hostEnvironment.ContentRootPath);
			}

			if(fullAttachDbFilename == null || string.Equals(attachDbFilename, fullAttachDbFilename, StringComparison.OrdinalIgnoreCase))
				return false;

			sqlConnectionStringBuilder.AttachDBFilename = fullAttachDbFilename;

			if(string.IsNullOrEmpty(sqlConnectionStringBuilder.InitialCatalog))
				sqlConnectionStringBuilder.InitialCatalog = fullAttachDbFilename;

			return true;
		}

		public static string ResolveConnectionString(string connectionString, IHostEnvironment hostEnvironment)
		{
			if(hostEnvironment == null)
				throw new ArgumentNullException(nameof(hostEnvironment));

			SqlConnectionStringBuilder sqlConnectionStringBuilder;

			try
			{
				sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
			}
			catch(Exception exception)
			{
				throw new InvalidOperationException("Could not create a sql-connection-string-builder from connection-string.", exception);
			}

			if(!sqlConnectionStringBuilder.Resolve(hostEnvironment))
				return connectionString;

			return sqlConnectionStringBuilder.ConnectionString;
		}

		#endregion
	}
}