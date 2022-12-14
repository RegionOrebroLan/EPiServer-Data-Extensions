using EPiServer.Data;

namespace RegionOrebroLan.EPiServer.Data
{
	public interface IConnectionStringResolver
	{
		#region Methods

		bool Resolve(ConnectionStringOptions options);

		#endregion
	}
}