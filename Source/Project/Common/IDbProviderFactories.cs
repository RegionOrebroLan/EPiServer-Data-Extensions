using System.Data.Common;

namespace RegionOrebroLan.EPiServer.Data.Common
{
	public interface IDbProviderFactories
	{
		#region Methods

		DbProviderFactory Get(string name);

		#endregion
	}
}