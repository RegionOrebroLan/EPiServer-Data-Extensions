using System;
using RegionOrebroLan.EPiServer.Data;

namespace IntegrationTests.Helpers
{
	public static class AppDomainHelper
	{
		#region Methods

		public static object GetDataDirectory()
		{
			return AppDomain.CurrentDomain.GetData(DataDirectory.Key);
		}

		public static void SetDataDirectory(object data)
		{
			AppDomain.CurrentDomain.SetData(DataDirectory.Key, data);
		}

		#endregion
	}
}