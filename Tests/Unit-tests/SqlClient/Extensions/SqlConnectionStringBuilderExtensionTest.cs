using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegionOrebroLan.EPiServer.Data;
using RegionOrebroLan.EPiServer.Data.Hosting;
using RegionOrebroLan.EPiServer.Data.SqlClient.Extensions;

namespace UnitTests.SqlClient.Extensions
{
	[TestClass]
	public class SqlConnectionStringBuilderExtensionTest
	{
		#region Fields

		private static readonly IHostEnvironment _hostEnvironment = new HostEnvironment
		{
			ContentRootPath = Path.GetTempPath()
		};

		private const string _localDbServer = @"(LocalDB)\MSSQLLocalDB";

		#endregion

		#region Methods

		[TestMethod]
		public async Task IsLocalDatabaseConnectionString_Test()
		{
			await Task.CompletedTask;

			Assert.IsTrue(new SqlConnectionStringBuilder("Server=(LocalDb)").IsLocalDatabaseConnectionString());
			Assert.IsTrue(new SqlConnectionStringBuilder("Server= (LocalDb)").IsLocalDatabaseConnectionString());
			Assert.IsTrue(new SqlConnectionStringBuilder("Server= (LocalDb) ").IsLocalDatabaseConnectionString());
			Assert.IsTrue(new SqlConnectionStringBuilder("Server=(localdb)").IsLocalDatabaseConnectionString());
			Assert.IsTrue(new SqlConnectionStringBuilder("Server= (localdb)").IsLocalDatabaseConnectionString());
			Assert.IsTrue(new SqlConnectionStringBuilder("Server= (localdb) ").IsLocalDatabaseConnectionString());
		}

		[TestMethod]
		public async Task Resolve_Test()
		{
			await Task.CompletedTask;

			var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = $"{DataDirectory.Substitution}EPiServer.mdf"
			};
			Assert.IsFalse(sqlConnectionStringBuilder.Resolve(_hostEnvironment));

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = "App_Data/EPiServer.mdf"
			};
			Assert.IsFalse(sqlConnectionStringBuilder.Resolve(_hostEnvironment));

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = @"App_Data\EPiServer.mdf"
			};
			Assert.IsFalse(sqlConnectionStringBuilder.Resolve(_hostEnvironment));

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = Path.Combine(Path.GetTempPath(), "App_Data", "EPiServer.mdf")
			};
			Assert.IsFalse(sqlConnectionStringBuilder.Resolve(_hostEnvironment));

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = "App_Data/EPiServer.mdf",
				DataSource = _localDbServer
			};
			Assert.IsTrue(sqlConnectionStringBuilder.Resolve(_hostEnvironment));
			Assert.AreEqual(Path.Combine(_hostEnvironment.ContentRootPath, @"App_Data\EPiServer.mdf"), sqlConnectionStringBuilder.AttachDBFilename);
			Assert.AreEqual(Path.Combine(_hostEnvironment.ContentRootPath, @"App_Data\EPiServer.mdf"), sqlConnectionStringBuilder.InitialCatalog);

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = @"App_Data\EPiServer.mdf",
				DataSource = _localDbServer
			};
			Assert.IsTrue(sqlConnectionStringBuilder.Resolve(_hostEnvironment));
			Assert.AreEqual(Path.Combine(_hostEnvironment.ContentRootPath, @"App_Data\EPiServer.mdf"), sqlConnectionStringBuilder.AttachDBFilename);
			Assert.AreEqual(Path.Combine(_hostEnvironment.ContentRootPath, @"App_Data\EPiServer.mdf"), sqlConnectionStringBuilder.InitialCatalog);

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = Path.Combine(Path.GetTempPath(), "App_Data", "EPiServer.mdf"),
				DataSource = _localDbServer
			};
			Assert.IsFalse(sqlConnectionStringBuilder.Resolve(_hostEnvironment));

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = @"App_Data\EPiServer.mdf",
				DataSource = _localDbServer,
				InitialCatalog = "Test"
			};
			Assert.IsTrue(sqlConnectionStringBuilder.Resolve(_hostEnvironment));
			Assert.AreEqual(Path.Combine(_hostEnvironment.ContentRootPath, @"App_Data\EPiServer.mdf"), sqlConnectionStringBuilder.AttachDBFilename);
			Assert.AreEqual("Test", sqlConnectionStringBuilder.InitialCatalog);
		}

		#endregion
	}
}