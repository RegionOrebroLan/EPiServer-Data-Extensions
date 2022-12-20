using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RegionOrebroLan.EPiServer.Data.Hosting;
using RegionOrebroLan.EPiServer.Data.SqlClient.Extensions;

namespace IntegrationTests.Helpers
{
	public static class DatabaseHelper
	{
		#region Fields

		private static readonly IHostEnvironment _hostEnvironment = new HostEnvironment
		{
			ContentRootPath = Global.ProjectDirectoryPath
		};

		#endregion

		#region Methods

		private static async Task<DbContext> CreateContextAsync(string connectionString)
		{
			connectionString = SqlConnectionStringBuilderExtension.ResolveConnectionString(connectionString, _hostEnvironment);

			var contextOptionsBuilder = new DbContextOptionsBuilder<DbContext>();

			contextOptionsBuilder.UseSqlServer(connectionString);

			return await Task.FromResult(new DbContext(contextOptionsBuilder.Options));
		}

		public static async Task CreateDatabaseAsync(string connectionString)
		{
			// ReSharper disable All
			using(var context = await CreateContextAsync(connectionString))
			{
				await context.Database.EnsureCreatedAsync();
			}
			// ReSharper restore All
		}

		public static async Task DeleteDatabaseAsync(string connectionString)
		{
			// ReSharper disable All
			using(var context = await CreateContextAsync(connectionString))
			{
				await context.Database.EnsureDeletedAsync();
			}
			// ReSharper restore All
		}

		#endregion
	}
}