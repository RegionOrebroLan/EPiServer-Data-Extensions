using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegionOrebroLan.EPiServer.Data;
using RegionOrebroLan.EPiServer.Data.Extensions.Internal;
using RegionOrebroLan.EPiServer.Data.Hosting;
using RegionOrebroLan.EPiServer.Data.SqlClient.Extensions;

namespace IntegrationTests.SqlClient.Extensions
{
	[TestClass]
	public class SqlConnectionStringBuilderExtensionTest
	{
		#region Fields

		private static readonly IHostEnvironment _hostEnvironment = new HostEnvironment
		{
			ContentRootPath = Global.ProjectDirectoryPath
		};

		private const string _localDbServer = @"(LocalDB)\MSSQLLocalDB";

		#endregion

		#region Methods

		[ClassCleanup]
		public static async Task ClassCleanup()
		{
			await Cleanup();
		}

		[ClassInitialize]
		public static async Task ClassInitialize(TestContext _)
		{
			await Cleanup();
		}

		public static async Task Cleanup()
		{
			await Task.CompletedTask;

			AppDomainHelper.SetDataDirectory(null);
		}

		[TestMethod]
		public async Task Resolve_IfDataDirectoryRequiredAndSet_ShouldWorkProperly()
		{
			await Task.CompletedTask;

			var dataDirectoryPath = Path.GetTempPath();
			AppDomainHelper.SetDataDirectory(dataDirectoryPath);

			var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = $"{DataDirectory.Substitution}EPiServer.mdf",
				DataSource = _localDbServer
			};
			Assert.IsTrue(sqlConnectionStringBuilder.Resolve(_hostEnvironment));
			Assert.AreEqual(Path.Combine(dataDirectoryPath, "EPiServer.mdf"), sqlConnectionStringBuilder.AttachDBFilename);
			Assert.AreEqual(Path.Combine(dataDirectoryPath, "EPiServer.mdf"), sqlConnectionStringBuilder.InitialCatalog);

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = $"{DataDirectory.Substitution}/EPiServer.mdf",
				DataSource = _localDbServer
			};
			Assert.IsTrue(sqlConnectionStringBuilder.Resolve(_hostEnvironment));
			Assert.AreEqual(Path.Combine(dataDirectoryPath, "EPiServer.mdf"), sqlConnectionStringBuilder.AttachDBFilename);
			Assert.AreEqual(Path.Combine(dataDirectoryPath, "EPiServer.mdf"), sqlConnectionStringBuilder.InitialCatalog);

			sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = $"{DataDirectory.Substitution}\\EPiServer.mdf",
				DataSource = _localDbServer
			};
			Assert.IsTrue(sqlConnectionStringBuilder.Resolve(_hostEnvironment));
			Assert.AreEqual(Path.Combine(dataDirectoryPath, "EPiServer.mdf"), sqlConnectionStringBuilder.AttachDBFilename);
			Assert.AreEqual(Path.Combine(dataDirectoryPath, "EPiServer.mdf"), sqlConnectionStringBuilder.InitialCatalog);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public async Task Resolve_IfDataDirectoryRequiredAndSetButNotExists_ShouldThrowAnInvalidOperationException()
		{
			await Task.CompletedTask;

			var dataDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			Assert.IsFalse(Directory.Exists(dataDirectoryPath), $"The directory {dataDirectoryPath.ToStringRepresentation()} should not exist.");

			AppDomainHelper.SetDataDirectory(dataDirectoryPath);

			var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = $"{DataDirectory.Substitution}EPiServer.mdf",
				DataSource = _localDbServer
			};

			try
			{
				sqlConnectionStringBuilder.Resolve(_hostEnvironment);
			}
			catch(InvalidOperationException invalidOperationException)
			{
				if(invalidOperationException.Message.Equals($"The directory-path {dataDirectoryPath.ToStringRepresentation()}, set as \"DataDirectory\"-key for the AppDomain, does not exist."))
					throw;
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public async Task Resolve_IfDataDirectoryRequiredButNotSet_ShouldThrowAnInvalidOperationException()
		{
			await Task.CompletedTask;

			var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
			{
				AttachDBFilename = $"{DataDirectory.Substitution}EPiServer.mdf",
				DataSource = _localDbServer
			};

			try
			{
				sqlConnectionStringBuilder.Resolve(_hostEnvironment);
			}
			catch(InvalidOperationException invalidOperationException)
			{
				if(invalidOperationException.Message.Equals("The connection-string contains \"AttachDBFilename=|DataDirectory|EPiServer.mdf\" but the AppDomain does not have the \"DataDirectory\"-key set. You need to set the \"DataDirectory\"-key: AppDomain.CurrentDomain.SetData(\"DataDirectory\", \"C:\\Directory\")."))
					throw;
			}
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
			Assert.AreEqual(Path.Combine(_hostEnvironment.ContentRootPath, "App_Data/EPiServer.mdf"), sqlConnectionStringBuilder.AttachDBFilename);
			Assert.AreEqual(Path.Combine(_hostEnvironment.ContentRootPath, "App_Data/EPiServer.mdf"), sqlConnectionStringBuilder.InitialCatalog);

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

		[TestCleanup]
		public async Task TestCleanup()
		{
			await Cleanup();
		}

		#endregion
	}
}