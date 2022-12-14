using System;
using System.Collections.Generic;
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

namespace IntegrationTests
{
	[TestClass]
	public static class Global
	{
		#region Fields

		private static readonly Assembly[] _defaultAssemblies =
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

		// ReSharper disable PossibleNullReferenceException
		public static readonly string ProjectDirectoryPath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
		// ReSharper restore PossibleNullReferenceException

		#endregion

		#region Properties

		public static IEnumerable<Assembly> DefaultAssemblies => _defaultAssemblies;

		#endregion

		#region Methods

		public static IList<Assembly> GetAssemblies()
		{
			return new List<Assembly>(DefaultAssemblies);
		}

		public static void Initialize()
		{
			Initialize(DefaultAssemblies);
		}

		public static void Initialize(IEnumerable<Assembly> assemblies)
		{
			assemblies = (assemblies ?? Enumerable.Empty<Assembly>()).ToArray();

			var initializationEngine = new InitializationEngine((IServiceLocatorFactory)null, HostType.TestFramework, assemblies);

			initializationEngine.Configure();
		}

		#endregion
	}
}