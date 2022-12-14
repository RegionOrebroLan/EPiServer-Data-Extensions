using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;

namespace RegionOrebroLan.EPiServer.Data.Common
{
	/// <inheritdoc />
	public class DbProviderFactoriesWrapper : IDbProviderFactories
	{
		#region Methods

		public virtual DbProviderFactory Get(string name)
		{
			/*
				When System.Data.Common.DbProviderFactories has been ported, use the following code instead:
				return DbProviderFactories.GetFactory(name);
			*/

			if(name != null && name.Equals("System.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
				return SqlClientFactory.Instance;

			throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, $"Can not get a factory for provider with invariant-name \"{name}\"."));
		}

		#endregion
	}
}