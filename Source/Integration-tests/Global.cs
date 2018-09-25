using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using EPiServer.ApplicationModules.Security;
using EPiServer.Data;
using EPiServer.Data.Cache;
using EPiServer.Events;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Licensing;
using EPiServer.ServiceLocation;
using EPiServer.ServiceLocation.AutoDiscovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegionOrebroLan.EPiServer.Data.IntegrationTests
{
	[TestClass]
	[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
	public static class Global
	{
		#region Fields

		// ReSharper disable PossibleNullReferenceException
		public static readonly string ProjectDirectoryPath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
		// ReSharper restore PossibleNullReferenceException

		#endregion

		#region Methods

		private static IEnumerable<Assembly> GetAssemblies()
		{
			return new[]
			{
				typeof(SiteSecret).Assembly, // EPiServer.ApplicationModules
				typeof(DataAccessOptions).Assembly, // EPiServer.Data
				typeof(DefaultCacheProvider).Assembly, // EPiServer.Data.Cache
				typeof(EventMessage).Assembly, // EPiServer.Events
				typeof(ContextCache).Assembly, // EPiServer.Framework
				typeof(LicenseBuilder).Assembly, // EPiServer.Licensing
				typeof(StructureMapServiceLocatorFactory).Assembly, // EPiServer.ServiceLocation.StructureMap
				typeof(Global).Assembly // This assembly
			};
		}

		[AssemblyInitialize]
		[CLSCompliant(false)]
		[SuppressMessage("Usage", "CA1801:Review unused parameters")]
		public static void Initialize(TestContext testContext)
		{
			//AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(ProjectDirectoryPath, "App_Data") + @"\");
			AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(ProjectDirectoryPath, "App_Data"));

			var assemblies = GetAssemblies().ToArray();

			var initializationEngine = new InitializationEngine((IServiceLocatorFactory) null, HostType.TestFramework, assemblies);

			initializationEngine.Configure();
		}

		#endregion
	}
}