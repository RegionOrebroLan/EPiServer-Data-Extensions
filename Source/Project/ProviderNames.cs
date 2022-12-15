using System.Data.SqlClient;

namespace RegionOrebroLan.EPiServer.Data
{
	public static class ProviderNames
	{
		#region Fields

		public static readonly string SqlServer = typeof(SqlConnection).Namespace; // "System.Data.SqlClient"

		#endregion
	}
}