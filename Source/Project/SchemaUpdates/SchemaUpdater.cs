using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EPiServer.Data;
using EPiServer.Data.Providers.Internal;
using EPiServer.Data.SchemaUpdates;
using EPiServer.Data.SchemaUpdates.Internal;
using EPiServer.Framework;
using RegionOrebroLan.EPiServer.Data.Common;
using RegionOrebroLan.EPiServer.Data.Hosting;

namespace RegionOrebroLan.EPiServer.Data.SchemaUpdates
{
	public class SchemaUpdater : SchemaUpdaterBase, ISchemaUpdater
	{
		#region Fields

		private const string _createDatabaseResourceName = "EPiServer.Data.Resources.SqlCreateScripts.zip";
		private object _databaseVersionRetriever;
		private static FieldInfo _databaseVersionRetrieverField;
		private DatabaseVersionValidator _databaseVersionValidator;
		private Func<bool, Version> _getDatabaseVersionFunction;

		#endregion

		#region Constructors

		public SchemaUpdater(IDatabaseConnectionResolver databaseConnectionResolver, IDatabaseExecutor databaseExecutor, IDbProviderFactories dbProviderFactories, EnvironmentOptions environment, IHostEnvironment hostEnvironment, ScriptExecutor scriptExecutor) : base(dbProviderFactories)
		{
			this.DatabaseConnectionResolver = databaseConnectionResolver ?? throw new ArgumentNullException(nameof(databaseConnectionResolver));
			this.DatabaseExecutor = databaseExecutor ?? throw new ArgumentNullException(nameof(databaseExecutor));
			this.Environment = environment ?? throw new ArgumentNullException(nameof(environment));
			this.HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			this.ScriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
		}

		#endregion

		#region Properties

		protected internal virtual string CreateDatabaseResourceName => _createDatabaseResourceName;
		protected internal virtual IDatabaseConnectionResolver DatabaseConnectionResolver { get; }
		protected internal virtual IDatabaseExecutor DatabaseExecutor { get; }

		protected internal virtual object DatabaseVersionRetriever
		{
			get
			{
				this._databaseVersionRetriever ??= this.DatabaseVersionRetrieverField.GetValue(this.DatabaseVersionValidator);

				return this._databaseVersionRetriever;
			}
		}

		protected internal virtual FieldInfo DatabaseVersionRetrieverField
		{
			get
			{
				if(_databaseVersionRetrieverField == null)
					_databaseVersionRetrieverField = typeof(DatabaseVersionValidator).GetField("_databaseVersionRetriever", BindingFlags.Instance | BindingFlags.NonPublic);

				return _databaseVersionRetrieverField;
			}
		}

		protected internal virtual DatabaseVersionValidator DatabaseVersionValidator => this._databaseVersionValidator ??= new DatabaseVersionValidator(this.DatabaseExecutor, this.DatabaseConnectionResolver, this.ScriptExecutor);
		protected internal virtual EnvironmentOptions Environment { get; }

		protected internal virtual Func<bool, Version> GetDatabaseVersionFunction
		{
			get
			{
				// ReSharper disable All
				if(this._getDatabaseVersionFunction == null)
				{
					var databaseVersionRetrieverType = Type.GetType(typeof(DatabaseVersionValidator).AssemblyQualifiedName.Replace("DatabaseVersionValidator", "DatabaseVersionRetriever"), true);
					var getDatabaseVersionMethod = databaseVersionRetrieverType.GetMethod("GetDatabaseVersion");
					this._getDatabaseVersionFunction = (Func<bool, Version>)Delegate.CreateDelegate(typeof(Func<bool, Version>), this.DatabaseVersionRetriever, getDatabaseVersionMethod);
				}
				// ReSharper restore All

				return this._getDatabaseVersionFunction;
			}
		}

		protected internal virtual IHostEnvironment HostEnvironment { get; }
		protected internal virtual ScriptExecutor ScriptExecutor { get; }

		#endregion

		#region Methods

		protected internal virtual IDictionary<string, T> CreateStringKeyDictionary<T>()
		{
			return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
		}

		protected internal virtual IDictionary<string, string> GetArchiveEntries(Stream stream)
		{
			var archiveEntries = this.CreateStringKeyDictionary<string>();

			using(var archive = new ZipArchive(stream))
			{
				foreach(var entry in archive.Entries)
				{
					using(var entryStream = entry.Open())
					{
						using(var streamReader = new StreamReader(entryStream))
						{
							var scriptContent = streamReader.ReadToEnd();

							archiveEntries.Add(entry.FullName, scriptContent);
						}
					}
				}
			}

			return archiveEntries;
		}

		protected internal virtual Version GetDatabaseVersion(bool forceRefresh)
		{
			return this.GetDatabaseVersionFunction(forceRefresh);
		}

		protected internal virtual string GetDataDirectoryPath()
		{
			/*
				When we are at EPiServer 12 we can do:

				return Path.GetFullPath(this.Environment.AppDataPath, this.HostEnvironment.ContentRootPath);
			*/

			var applicationDataPath = this.Environment.BasePath;

			return Path.IsPathRooted(applicationDataPath) ? applicationDataPath : Path.Combine(this.HostEnvironment.ContentRootPath, applicationDataPath);
		}

		protected internal virtual IDictionary<string, string> GetReplacementEntries(string resourceName)
		{
			var replacementsArchivePath = Path.Combine(this.GetDataDirectoryPath(), resourceName + "-Replacements.zip");
			var replacementsArchiveExists = File.Exists(replacementsArchivePath);

			// ReSharper disable InvertIf
			if(replacementsArchiveExists)
			{
				using(var stream = File.OpenRead(replacementsArchivePath))
				{
					return this.GetArchiveEntries(stream);
				}
			}
			// ReSharper restore InvertIf

			return this.CreateStringKeyDictionary<string>();
		}

		protected internal virtual IDictionary<string, IEnumerable<Tuple<string, string, bool>>> GetReplacements(string resourceName)
		{
			var temporaryReplacements = this.CreateStringKeyDictionary<IList<Tuple<string, string, bool>>>();
			var replacementEntries = this.GetReplacementEntries(resourceName);

			foreach(var key in replacementEntries.Keys)
			{
				var extension = Path.GetExtension(key);
				var keyWithoutExtension = key.Substring(0, key.Length - extension.Length);
				var replacementKey = keyWithoutExtension + ".Replacement" + extension;

				if(!replacementEntries.ContainsKey(replacementKey))
					continue;

				const char delimiter = '/';

				var keyParts = key.Split(delimiter).ToList();

				if(!keyParts.Any())
					continue;

				keyParts.RemoveAt(keyParts.Count - 1);

				var resourceKey = string.Join(delimiter.ToString(CultureInfo.InvariantCulture), keyParts);

				if(!temporaryReplacements.TryGetValue(resourceKey, out var list))
				{
					list = new List<Tuple<string, string, bool>>();
					temporaryReplacements.Add(resourceKey, list);
				}

				var replacement = new Tuple<string, string, bool>(replacementEntries[key], replacementEntries[replacementKey], true);

				list.Add(replacement);
			}

			var replacements = this.CreateStringKeyDictionary<IEnumerable<Tuple<string, string, bool>>>();

			foreach(var replacement in temporaryReplacements)
			{
				replacements.Add(replacement.Key, replacement.Value.ToArray());
			}

			return replacements;
		}

		protected internal virtual IEnumerable<string> GetScriptContents(string resourceName)
		{
			var scriptContents = new List<string>();

			var scriptEntries = this.GetScriptEntries(resourceName);
			var replacements = this.GetReplacements(resourceName);

			foreach(var scriptEntry in scriptEntries)
			{
				var scriptContent = scriptEntry.Value;

				foreach(var item in replacements.Where(item => item.Key.Equals(scriptEntry.Key, StringComparison.OrdinalIgnoreCase)))
				{
					foreach(var replacement in item.Value)
					{
						var options = replacement.Item3 ? RegexOptions.IgnoreCase : RegexOptions.None;
						scriptContent = Regex.Replace(scriptContent, Regex.Escape(replacement.Item1), replacement.Item2, options);
					}
				}

				scriptContents.Add(scriptContent);
			}

			return scriptContents.ToArray();
		}

		protected internal virtual IDictionary<string, string> GetScriptEntries(string resourceName)
		{
			using(var stream = typeof(IDatabaseExecutor).Assembly.GetManifestResourceStream(resourceName))
			{
				return this.GetArchiveEntries(stream);
			}
		}

		public virtual SchemaStatus GetStatus(IEnumerable<ConnectionStringOptions> connectionStringOptions)
		{
			var schemaStatus = this.DatabaseVersionValidator.GetStatus(connectionStringOptions);

			return schemaStatus;
		}

		public virtual void Update(ConnectionStringOptions connectionStringOptions)
		{
			if(connectionStringOptions == null)
				throw new ArgumentNullException(nameof(connectionStringOptions));

			var databaseVersion = this.GetDatabaseVersion(false);

			if(databaseVersion == SchemaStatus.UndefinedVersion)
			{
				this.ScriptExecutor.OrderScriptsByVersion = false;

				foreach(var scriptContent in this.GetScriptContents(this.CreateDatabaseResourceName))
				{
					using(var stream = this.CreateStream(scriptContent))
					{
						this.ScriptExecutor.ExecuteScript(connectionStringOptions.ConnectionString, stream);
					}
				}
			}
			else
			{
				this.ScriptExecutor.OrderScriptsByVersion = true;
				this.ScriptExecutor.ExecuteEmbeddedZippedScripts(connectionStringOptions.ConnectionString, typeof(IDatabaseExecutor).Assembly, "EPiServer.Data.Resources.SqlUpdateScripts.zip");
			}
		}

		#endregion
	}
}