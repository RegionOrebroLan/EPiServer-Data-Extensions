using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegionOrebroLan.EPiServer.Data.SqlClient.Extensions;

namespace UnitTests.SqlClient.Extensions
{
	[TestClass]
	public class SqlConnectionStringBuilderExtensionTest
	{
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

		#endregion
	}
}