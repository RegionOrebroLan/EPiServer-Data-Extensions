using EPiServer.Data;

namespace RegionOrebroLan.EPiServer.Data
{
	public interface IDatabaseCreator
	{
		#region Methods

		bool EnsureCreated(ConnectionStringOptions options);

		#endregion
	}
}