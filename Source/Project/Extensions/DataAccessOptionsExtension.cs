using System;
using EPiServer.Data;

namespace RegionOrebroLan.EPiServer.Data.Extensions
{
	public static class DataAccessOptionsExtension
	{
		#region Methods

		public static void ResolveConnectionStrings(this DataAccessOptions options, IConnectionStringResolver connectionStringResolver)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			if(connectionStringResolver == null)
				throw new ArgumentNullException(nameof(connectionStringResolver));

			foreach(var connectionStringOptions in options.ConnectionStrings)
			{
				connectionStringResolver.Resolve(connectionStringOptions);
			}
		}

		#endregion
	}
}