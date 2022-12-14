using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EPiServer.Data;
using EPiServer.Data.Providers.Internal;
using EPiServer.Data.SchemaUpdates;
using EPiServer.Data.SchemaUpdates.Internal;
using RegionOrebroLan.Data;
using RegionOrebroLan.Data.Common;
using RegionOrebroLan.Extensions;

namespace RegionOrebroLan.EPiServer.Data.SchemaUpdates
{
	public class SchemaUpdater : BasicSchemaUpdater, ISchemaUpdater
	{
		#region Fields

		private const string _createDatabaseResourceName = "EPiServer.Data.Resources.SqlCreateScripts.zip";
		private object _databaseVersionRetriever;
		private DatabaseVersionValidator _databaseVersionValidator;
		private Func<bool, Version> _getDatabaseVersionFunction;

		#endregion

		#region Constructors

		public SchemaUpdater(IApplicationDomain applicationDomain, IConnectionStringBuilderFactory connectionStringBuilderFactory, IDatabaseConnectionResolver databaseConnectionResolver, IDatabaseExecutor databaseExecutor, IFileSystem fileSystem, IProviderFactories providerFactories, ScriptExecutor scriptExecutor) : base(providerFactories)
		{
			this.ApplicationDomain = applicationDomain ?? throw new ArgumentNullException(nameof(applicationDomain));
			this.ConnectionStringBuilderFactory = connectionStringBuilderFactory ?? throw new ArgumentNullException(nameof(connectionStringBuilderFactory));
			this.DatabaseConnectionResolver = databaseConnectionResolver ?? throw new ArgumentNullException(nameof(databaseConnectionResolver));
			this.DatabaseExecutor = databaseExecutor ?? throw new ArgumentNullException(nameof(databaseExecutor));
			this.FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			this.ScriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
		}

		#endregion

		#region Properties

		protected internal virtual IApplicationDomain ApplicationDomain { get; }
		protected internal virtual IConnectionStringBuilderFactory ConnectionStringBuilderFactory { get; }
		protected internal virtual string CreateDatabaseResourceName => _createDatabaseResourceName;
		protected internal virtual IDatabaseConnectionResolver DatabaseConnectionResolver { get; }
		protected internal virtual IDatabaseExecutor DatabaseExecutor { get; }

		protected internal virtual object DatabaseVersionRetriever
		{
			get
			{
				// ReSharper disable All
				this._databaseVersionRetriever ??= typeof(DatabaseVersionValidator).GetField("_databaseVersionRetriever", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this.DatabaseVersionValidator);
				// ReSharper restore All

				return this._databaseVersionRetriever;
			}
		}

		protected internal virtual DatabaseVersionValidator DatabaseVersionValidator => this._databaseVersionValidator ??= new DatabaseVersionValidator(this.DatabaseExecutor, this.DatabaseConnectionResolver, this.ScriptExecutor);
		protected internal virtual IFileSystem FileSystem { get; }

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

		protected internal virtual ScriptExecutor ScriptExecutor { get; }

		#endregion

		#region Methods

		protected internal virtual IDictionary<string, T> CreateStringKeyDictionary<T>()
		{
			return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
		}

		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
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

		protected internal virtual IDictionary<string, string> GetReplacementEntries(string resourceName)
		{
			var replacementsArchivePath = this.FileSystem.Path.Combine(this.ApplicationDomain.GetDataDirectoryPath(), resourceName + "-Replacements.zip");
			var replacementsArchiveExists = this.FileSystem.File.Exists(replacementsArchivePath);

			// ReSharper disable InvertIf
			if(replacementsArchiveExists)
			{
				using(var stream = this.FileSystem.File.OpenRead(replacementsArchivePath))
				{
					return this.GetArchiveEntries(stream);
				}
			}
			// ReSharper restore InvertIf

			return this.CreateStringKeyDictionary<string>();
		}

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		protected internal virtual IDictionary<string, IEnumerable<Tuple<string, string, bool>>> GetReplacements(string resourceName)
		{
			var temporaryReplacements = this.CreateStringKeyDictionary<IList<Tuple<string, string, bool>>>();
			var replacementEntries = this.GetReplacementEntries(resourceName);

			foreach(var key in replacementEntries.Keys)
			{
				var extension = this.FileSystem.Path.GetExtension(key);
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