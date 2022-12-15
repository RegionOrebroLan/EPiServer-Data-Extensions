using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPiServer.Data;
using EPiServer.Data.SchemaUpdates;
using EPiServer.Framework;
using EPiServer.ServiceLocation;
using IntegrationTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegionOrebroLan.EPiServer.Data.Common;

namespace IntegrationTests.SchemaUpdates
{
	[TestClass]
	public class SchemaUpdaterTest
	{
		#region Fields

		private string _applicationDataDirectoryPath;
		private ConnectionStringOptions _connectionSetting;
		private string _replacementFileCopyPath;
		private string _replacementFilePath;
		private const string _replacementFilePathFormat = "EPiServer.Data.Resources.SqlCreateScripts.zip-Replacements{0}.zip";

		#endregion

		#region Properties

		protected internal virtual string ApplicationDataDirectoryPath => this._applicationDataDirectoryPath ??= Path.Combine(Global.ProjectDirectoryPath, ServiceLocator.Current.GetInstance<EnvironmentOptions>().BasePath);

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

		protected internal virtual DataAccessOptions DataAccessOptions { get; } = ServiceLocator.Current.GetInstance<DataAccessOptions>();
		protected internal virtual string ReplacementFileCopyPath => this._replacementFileCopyPath ??= Path.Combine(this.ApplicationDataDirectoryPath, string.Format(CultureInfo.InvariantCulture, this.ReplacementFilePathFormat, ".Copy"));
		protected internal virtual string ReplacementFilePath => this._replacementFilePath ??= Path.Combine(this.ApplicationDataDirectoryPath, string.Format(CultureInfo.InvariantCulture, this.ReplacementFilePathFormat, string.Empty));
		protected internal virtual string ReplacementFilePathFormat => _replacementFilePathFormat;

		#endregion

		#region Methods

		[ClassCleanup]
		public static async Task ClassCeanup()
		{
			await Task.CompletedTask;

			AppDomainHelper.SetDataDirectory(null);
		}

		[ClassInitialize]
		public static async Task ClassInitialize(TestContext _)
		{
			await Task.CompletedTask;

			Global.Initialize();
		}

		protected internal virtual async Task CleanupAsync()
		{
			AppDomainHelper.SetDataDirectory(this.ApplicationDataDirectoryPath);

			await DatabaseHelper.DeleteDatabaseAsync(this.ConnectionSetting.ConnectionString);

			if(File.Exists(this.ReplacementFilePath))
				File.Delete(this.ReplacementFilePath);

			AppDomainHelper.SetDataDirectory(null);

			Thread.Sleep(2000);
		}

		protected internal virtual async Task CreateDatabaseAsync()
		{
			await DatabaseHelper.CreateDatabaseAsync(this.ConnectionSetting.ConnectionString);

			Thread.Sleep(2000);
		}

		protected internal virtual Tuple<int, string, string> GetLanguageBranchResult(int emptyLanguageIdRowNumber)
		{
			var emptyLanguageId = "Dummy";
			string firstLanguageId = null;
			var numberOfRows = 0;

			var providerFactories = ServiceLocator.Current.GetInstance<IDbProviderFactories>();

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
		public async Task GetStatus_IfTheDatabaseIsALocallyAttachedDatabaseButIsEmpty_ShouldReturnASchemaStatusWithAnUndefinedDatabaseVersion()
		{
			await this.CreateDatabaseAsync();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			var schemaStatus = schemaUpdater.GetStatus(null);

			Assert.IsNotNull(schemaStatus);
			Assert.AreEqual(SchemaStatus.UndefinedVersion, schemaStatus.DatabaseVersion);
		}

		[TestMethod]
		public async Task GetStatus_RegardlessOfWhetherTheConnectionStringOptionsParameterIsNullOrNot_ShouldReturnTheSameResult()
		{
			await this.CreateDatabaseAsync();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			var firstSchemaStatus = schemaUpdater.GetStatus(null);
			var secondSchemaStatus = schemaUpdater.GetStatus(this.DataAccessOptions.ConnectionStrings);

			Assert.AreEqual(firstSchemaStatus.DatabaseVersion, secondSchemaStatus.DatabaseVersion);

			schemaUpdater.Update(this.ConnectionSetting);

			firstSchemaStatus = schemaUpdater.GetStatus(null);
			secondSchemaStatus = schemaUpdater.GetStatus(this.DataAccessOptions.ConnectionStrings);

			Assert.AreEqual(firstSchemaStatus.DatabaseVersion, secondSchemaStatus.DatabaseVersion);
		}

		[TestCleanup]
		public async Task TestCleanup()
		{
			await this.CleanupAsync();
		}

		[TestInitialize]
		public async Task TestInitialize()
		{
			await this.CleanupAsync();

			AppDomainHelper.SetDataDirectory(this.ApplicationDataDirectoryPath);

			this.ValidateConnectionSetting();
		}

		[TestMethod]
		public async Task Update_WithoutReplacements_ShouldWorkProperly()
		{
			await this.CreateDatabaseAsync();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			schemaUpdater.Update(this.ConnectionSetting);

			var languageBranchResult = this.GetLanguageBranchResult(15);

			Assert.AreEqual(15, languageBranchResult.Item1);
			Assert.AreEqual("en", languageBranchResult.Item2);
			Assert.AreEqual(string.Empty, languageBranchResult.Item3);
		}

		[TestMethod]
		public async Task Update_WithReplacements_ShouldWorkProperly()
		{
			File.Copy(this.ReplacementFileCopyPath, this.ReplacementFilePath, true);

			Thread.Sleep(3000);

			await this.CreateDatabaseAsync();

			var schemaUpdater = ServiceLocator.Current.GetInstance<ISchemaUpdater>();

			schemaUpdater.Update(this.ConnectionSetting);

			var languageBranchResult = this.GetLanguageBranchResult(2);

			Assert.AreEqual(3, languageBranchResult.Item1);
			Assert.AreEqual("sv", languageBranchResult.Item2);
			Assert.AreEqual(string.Empty, languageBranchResult.Item3);
		}

		protected internal virtual void ValidateConnectionSetting()
		{
			var connectionStringToUpper = (this.ConnectionSetting?.ConnectionString ?? string.Empty).ToUpperInvariant();

			if(!connectionStringToUpper.Contains("LOCALDB") || !connectionStringToUpper.Contains("ATTACHDBFILENAME"))
				throw new InvalidOperationException("The connection-string is incorrect. Must be a connection-string for a locally attached database.");
		}

		#endregion
	}
}