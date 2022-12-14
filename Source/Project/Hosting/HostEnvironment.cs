using System;

namespace RegionOrebroLan.EPiServer.Data.Hosting
{
	/// <inheritdoc />
	public class HostEnvironment : IHostEnvironment
	{
		#region Properties

		public virtual string ContentRootPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

		#endregion
	}
}