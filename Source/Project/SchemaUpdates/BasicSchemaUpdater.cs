using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EPiServer.Data;
using RegionOrebroLan.Data.Common;

namespace RegionOrebroLan.EPiServer.Data.SchemaUpdates
{
	public abstract class BasicSchemaUpdater
	{
		#region Constructors

		protected BasicSchemaUpdater(IProviderFactories providerFactories)
		{
			this.ProviderFactories = providerFactories ?? throw new ArgumentNullException(nameof(providerFactories));
		}

		#endregion

		#region Properties

		protected internal virtual IProviderFactories ProviderFactories { get; }

		#endregion

		#region Methods

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Should be disposed by the caller.")]
		protected internal virtual Stream CreateStream(string value)
		{
			var stream = new MemoryStream();
			var streamWriter = new StreamWriter(stream);
			streamWriter.Write(value);
			streamWriter.Flush();
			stream.Position = 0;

			return stream;
		}

		[SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		protected internal virtual void ExecuteScript(ConnectionStringOptions connectionStringOption, string content)
		{
			if(connectionStringOption == null)
				throw new ArgumentNullException(nameof(connectionStringOption));

			var databaseProviderFactory = this.ProviderFactories.Get(connectionStringOption.ProviderName);

			using(var connection = databaseProviderFactory.CreateConnection())
			{
				// ReSharper disable PossibleNullReferenceException
				connection.ConnectionString = connectionStringOption.ConnectionString;
				// ReSharper restore PossibleNullReferenceException
				connection.Open();

				var lines = new List<string>();

				foreach(var line in (content ?? string.Empty).Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
				{
					if(line.Equals("GO", StringComparison.Ordinal))
					{
						if(!lines.Any())
							continue;

						using(var command = connection.CreateCommand())
						{
							command.CommandText = string.Join(Environment.NewLine, lines);
							command.CommandType = CommandType.Text;
							command.ExecuteNonQuery();
						}

						lines.Clear();
					}
					else
					{
						lines.Add(line);
					}
				}
			}
		}

		#endregion
	}
}