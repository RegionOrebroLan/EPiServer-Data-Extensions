using System.Threading.Tasks;
using RegionOrebroLan.EPiServer.Data.Hosting;
using RegionOrebroLan.EPiServer.Data.SqlClient.Extensions;

namespace IntegrationTests.Helpers
{
	public static class DatabaseHelper
	{
		#region Fields

		private static readonly IHostEnvironment _hostEnvironment = new TestHostEnvironment();

		#endregion

		#region Methods

		public static async Task CreateDatabaseAsync(string connectionString)
		{
			using(var testContext = new TestDbContext())
			{
				testContext.ConnectionString = SqlConnectionStringBuilderExtension.ResolveConnectionString(connectionString, _hostEnvironment);

				await testContext.Database.EnsureCreatedAsync();
			}
		}

		public static async Task DeleteDatabaseAsync(string connectionString)
		{
			using(var testContext = new TestDbContext())
			{
				testContext.ConnectionString = SqlConnectionStringBuilderExtension.ResolveConnectionString(connectionString, _hostEnvironment);

				await testContext.Database.EnsureDeletedAsync();
			}
		}

		#endregion
	}
}