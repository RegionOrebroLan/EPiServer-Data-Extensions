using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegionOrebroLan.EPiServer.Data.IO.Extensions;

namespace UnitTests.IO.Extensions
{
	[TestClass]
	public class PathExtensionTest
	{
		#region Methods

		[TestMethod]
		public async Task GetFullPath_Test()
		{
			await Task.CompletedTask;

			var basePath = @"C:\Directory";

			Assert.AreEqual(@"C:\Directory\Sub-directory", PathExtension.GetFullPath("Sub-directory", basePath));
			Assert.AreEqual(@"C:\Directory\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory\", basePath));
			Assert.AreEqual(@"C:\Directory\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory/", basePath));
			Assert.AreEqual(@"\Sub-directory", PathExtension.GetFullPath(@"\Sub-directory", basePath));
			Assert.AreEqual("/Sub-directory", PathExtension.GetFullPath("/Sub-directory", basePath));

			basePath = @"C:\Directory\";

			Assert.AreEqual(@"C:\Directory\Sub-directory", PathExtension.GetFullPath("Sub-directory", basePath));
			Assert.AreEqual(@"C:\Directory\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory\", basePath));
			Assert.AreEqual(@"C:\Directory\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory/", basePath));
			Assert.AreEqual(@"\Sub-directory", PathExtension.GetFullPath(@"\Sub-directory", basePath));
			Assert.AreEqual("/Sub-directory", PathExtension.GetFullPath("/Sub-directory", basePath));

			basePath = @"C:\Directory/";

			Assert.AreEqual(@"C:\Directory\Sub-directory", PathExtension.GetFullPath("Sub-directory", basePath));
			Assert.AreEqual(@"C:\Directory\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory\", basePath));
			Assert.AreEqual(@"C:\Directory\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory/", basePath));
			Assert.AreEqual(@"\Sub-directory", PathExtension.GetFullPath(@"\Sub-directory", basePath));
			Assert.AreEqual("/Sub-directory", PathExtension.GetFullPath("/Sub-directory", basePath));

			basePath = @"\Root/";

			Assert.AreEqual(@"C:\Root\Sub-directory", PathExtension.GetFullPath("Sub-directory", basePath));
			Assert.AreEqual(@"C:\Root\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory\", basePath));
			Assert.AreEqual(@"C:\Root\Sub-directory\", PathExtension.GetFullPath(@"Sub-directory/", basePath));
			Assert.AreEqual(@"\Sub-directory", PathExtension.GetFullPath(@"\Sub-directory", basePath));
			Assert.AreEqual("/Sub-directory", PathExtension.GetFullPath("/Sub-directory", basePath));
		}

		#endregion
	}
}