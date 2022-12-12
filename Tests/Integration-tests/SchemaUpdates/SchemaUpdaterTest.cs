using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using EPiServer.Data;
using EPiServer.Data.SchemaUpdates;
using EPiServer.ServiceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegionOrebroLan;
using RegionOrebroLan.Data;
using RegionOrebroLan.Data.Common;
using RegionOrebroLan.Data.Extensions;

namespace IntegrationTests.SchemaUpdates
{
	[TestClass]
	public class SchemaUpdaterTest
	{
		#region Fields

		private string _applicationDataDirectoryPath;
		private ConnectionStringOptions _connectionSetting;
		private string _databaseFilePath;
		private string _databaseLogFilePath;
		private IDatabaseManager _databaseManager;
		private string _replacementFileCopyPath;
		private string _replacementFilePath;
		private const string _replacementFilePathFormat = "EPiServer.Data.Resources.SqlCreateScripts.zip-Replacements{0}.zip";

		#endregion

		#region Properties

		protected internal virtual string ApplicationDataDirectoryPath => this._applicationDataDirectoryPath ?? (this._applicationDataDirectoryPath = (string)this.ApplicationDomain.GetData("DataDirectory"));
		protected internal virtual IApplicationDomain ApplicationDomain { get; } = ServiceLocator.Current.GetInstance<IApplicationDomain>();

		protected internal virtual ConnectionStringOptions ConnectionSetting
		{
			get
			{
				// ReSharper disable InvertIf
				if(this._connectionSetting == null)
				{
					var connectionName = this.DataAccessOptions.DefaultConnectionStringName;
					var connectionSetting = this.DataAccessOptions.ConnectionStrings.FirstOrDefault(item => item.Name.Equals(connectionName, StringComparison.OrdinalIgnoreCase));
					this._connectionSetting = connectionSetting ?? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "There is no configured connection-setting with name \"{0}\".", connectionName));
				}
				// ReSharper restore InvertIf

				return this._connectionSetting;
			}
		}

		protected internal virtual IConnectionStringBuilderFactory ConnectionStringBuilderFactory { get; } = ServiceLocator.Current.GetInstance<IConnectionStringBuilderFactory>();
		protected internal virtual DataAccessOptions DataAccessOptions { get; } = ServiceLocator.Current.GetInstance<DataAccessOptions>();

		protected internal virtual string DatabaseFilePath
		{
			get
			{
				// ReSharper disable InvertIf
				if(this._databaseFilePath == null)
				{
					var connectionStringBuilder = this.ConnectionStringBuilderFactory.Create(this.ConnectionSetting.ConnectionString, this.ConnectionSetting.ProviderName);

					this._databaseFilePath = connectionStringBuilder.GetActualDatabaseFilePath(this.ApplicationDomain);
				}
				// ReSharper restore InvertIf

				return this._databaseFilePath;
			}
		}

		protected internal virtual string DatabaseLogFilePath
		{
			get
			{
				// ReSharper disable InvertIf
				if(this._databaseLogFilePath == null)
				{
					var directoryPath = this.FileSystem.Path.GetDirectoryName(this.DatabaseFilePath);
					var fileName = this.FileSystem.Path.GetFileNameWithoutExtension(this.DatabaseFilePath);

					this._databaseLogFilePath = this.FileSystem.Path.Combine(directoryPath, fileName) + "_log.ldf";
				}
				// ReSharper restore InvertIf

				return this._databaseLogFilePath;
			}
		}

		protected internal virtual IDatabaseManager DatabaseManager
		{
			get
			{
				// ReSharper disable InvertIf
				if(this._databaseManager == null)
				{
					var databaseManagerFactory = ServiceLocator.Current.GetInstance<IDatabaseManagerFactory>();

					this._databaseManager = databaseManagerFactory.Create(this.ConnectionSetting.ProviderName);
				}
				// ReSharper restore InvertIf

				return this._databaseManager;
			}
		}

		protected internal virtual IFileSystem FileSystem { get; } = ServiceLocator.Current.GetInstance<IFileSystem>();
		protected internal virtual string ReplacementFileCopyPath => this._replacementFileCopyPath ?? (this._replacementFileCopyPath = this.FileSystem.Path.Combine(this.ApplicationDataDirectoryPath, string.Format(CultureInfo.InvariantCulture, this.ReplacementFilePathFormat, ".Copy")));
		protected internal virtual string ReplacementFilePath => this._replacementFilePath ?? (this._replacementFilePath = this.FileSystem.Path.Combine(this.ApplicationDataDirectoryPath, string.Format(CultureInfo.InvariantCulture, this.ReplacementFilePathFormat, string.Empty)));
		protected internal virtual string ReplacementFilePathFormat => _replacementFilePathFormat;

		#endregion

		#region Methods

		protected internal virtual void Cleanup()
		{
			if(this.FileSystem.File.Exists(this.ReplacementFilePath))
			{
				this.FileSystem.File.Delete(this.ReplacementFilePath);

				Thread.Sleep(3000);
			}

			if(!this.DatabaseManager.DatabaseExists(this.ConnectionSetting.ConnectionString))
				return;

			this.DatabaseManager.DropDatabase(this.ConnectionSetting.ConnectionString);

			Thread.Sleep(3000);
		}

		[TestCleanup]
		public void CleanupEachTest()
		{
			this.Cleanup();
		}

		protected internal virtual void CreateDatabase()
		{
			this.DatabaseManager.CreateDatabase(this.ConnectionSetting.ConnectionString);

			Thread.Sleep(5000);
		}

		protected internal virtual Tuple<int, string, string> GetLanguageBranchResult(int emptyLanguageIdRowNumber)
		{
			var emptyLanguageId = "Dummy";
			string firstLanguageId = null;
			var numberOfRows = 0;

			var providerFactories = ServiceLocator.Current.GetInstance<IProviderFactories>();

			var providerFactory = providerFactories.Get(this.ConnectionSetting.ProviderName);

			using(var connection = providerFactory.CreateConnection())
			{
				// ReSharper disable All
				connection.ConnectionString = this.ConnectionSetting.ConnectionString;

				using(var command = providerFactory.CreateCommand())
				{
					command.Connection = connection;

					command.CommandText = "SELECT * FROM [tblLanguageBranch];";
					command.CommandType = CommandType.Text;

					command.Connection.Open();

					using(var reader = command.ExecuteReader())
					{
						while(reader.Read())
						{
							var id = (int)reader["pkID"];

							if(id == 1)
								firstLanguageId = ((string)reader["LanguageID"] ?? string.Empty).Trim();

							if(id == emptyLanguageIdRowNumber)
								emptyLanguageId = ((string)reader["LanguageID"] ?? string.Empty).Trim();

							numberOfRows++;
						}
					}
				}
				// ReSharper restore All
			}

			return new Tuple<int, string, string>(numberOfRows, firstLanguageId, emptyLanguageId);
		}

		[TestMethod]
		[ExpectedException(typeof(SqlException))]
		public void GetStatus_IfTheDatabaseIsALocallyAttachedDatabaseAndDoesNotExist_ShouldThrowASqlException()
		{
			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			schemaUpdater.GetStatus(null);

			Thread.Sleep(2000);
		}

		[TestMethod]
		public void GetStatus_IfTheDatabaseIsALocallyAttachedDatabaseButIsEmpty_ShouldReturnASchemaStatusWithAnUndefinedDatabaseVersion()
		{
			this.CreateDatabase();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			var schemaStatus = schemaUpdater.GetStatus(null);

			Assert.IsNotNull(schemaStatus);
			Assert.AreEqual(SchemaStatus.UndefinedVersion, schemaStatus.DatabaseVersion);
		}

		[TestMethod]
		public void GetStatus_RegardlessOfWhetherTheConnectionStringOptionsParameterIsNullOrNot_ShouldReturnTheSameResult()
		{
			this.CreateDatabase();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			var firstSchemaStatus = schemaUpdater.GetStatus(null);
			var secondSchemaStatus = schemaUpdater.GetStatus(this.DataAccessOptions.ConnectionStrings);

			Assert.AreEqual(firstSchemaStatus.DatabaseVersion, secondSchemaStatus.DatabaseVersion);

			schemaUpdater.Update(this.ConnectionSetting);

			firstSchemaStatus = schemaUpdater.GetStatus(null);
			secondSchemaStatus = schemaUpdater.GetStatus(this.DataAccessOptions.ConnectionStrings);

			Assert.AreEqual(firstSchemaStatus.DatabaseVersion, secondSchemaStatus.DatabaseVersion);
		}

		[TestInitialize]
		public void InitializeEachTest()
		{
			this.Cleanup();

			this.ValidateConnectionSetting();
		}

		[TestMethod]
		public void Update_WithoutReplacements_ShouldWorkProperly()
		{
			this.CreateDatabase();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			schemaUpdater.Update(this.ConnectionSetting);

			var languageBranchResult = this.GetLanguageBranchResult(15);

			Assert.AreEqual(15, languageBranchResult.Item1);
			Assert.AreEqual("en", languageBranchResult.Item2);
			Assert.AreEqual(string.Empty, languageBranchResult.Item3);
		}

		[TestMethod]
		public void Update_WithReplacements_ShouldWorkProperly()
		{
			this.FileSystem.File.Copy(this.ReplacementFileCopyPath, this.ReplacementFilePath, true);

			Thread.Sleep(3000);

			this.CreateDatabase();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			schemaUpdater.Update(this.ConnectionSetting);

			var languageBranchResult = this.GetLanguageBranchResult(2);

			Assert.AreEqual(3, languageBranchResult.Item1);
			Assert.AreEqual("sv", languageBranchResult.Item2);
			Assert.AreEqual(string.Empty, languageBranchResult.Item3);
		}

		[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
		protected internal virtual void ValidateConnectionSetting()
		{
			var connectionStringToUpper = (this.ConnectionSetting?.ConnectionString ?? string.Empty).ToUpperInvariant();

			if(!connectionStringToUpper.Contains("LOCALDB") || !connectionStringToUpper.Contains("ATTACHDBFILENAME"))
				throw new InvalidOperationException("The connection-string is incorrect. Must be a connection-string for a locally attached database.");
		}

		#endregion
	}
}