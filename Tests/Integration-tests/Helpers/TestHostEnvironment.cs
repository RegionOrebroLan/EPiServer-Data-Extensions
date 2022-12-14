using RegionOrebroLan.EPiServer.Data.Hosting;

namespace IntegrationTests.Helpers
{
	public class TestHostEnvironment : IHostEnvironment
	{
		#region Properties

		public virtual string ContentRootPath => Global.ProjectDirectoryPath;

		#endregion
	}
}