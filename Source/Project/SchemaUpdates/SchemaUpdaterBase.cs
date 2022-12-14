using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using EPiServer.Data;
using RegionOrebroLan.EPiServer.Data.Common;

namespace RegionOrebroLan.EPiServer.Data.SchemaUpdates
{
	public abstract class SchemaUpdaterBase
	{
		#region Constructors

		protected SchemaUpdaterBase(IDbProviderFactories dbProviderFactories)
		{
			this.DbProviderFactories = dbProviderFactories ?? throw new ArgumentNullException(nameof(dbProviderFactories));
		}

		#endregion

		#region Properties

		protected internal virtual IDbProviderFactories DbProviderFactories { get; }

		#endregion

		#region Methods

		protected internal virtual Stream CreateStream(string value)
		{
			var stream = new MemoryStream();
			var streamWriter = new StreamWriter(stream);
			streamWriter.Write(value);
			streamWriter.Flush();
			stream.Position = 0;

			return stream;
		}

		protected internal virtual void ExecuteScript(ConnectionStringOptions connectionStringOption, string content)
		{
			if(connectionStringOption == null)
				throw new ArgumentNullException(nameof(connectionStringOption));

			var databaseProviderFactory = this.DbProviderFactories.Get(connectionStringOption.ProviderName);

			using(var connection = databaseProviderFactory.CreateConnection())
			{
				// ReSharper disable PossibleNullReferenceException
				connection.ConnectionString = connectionStringOption.ConnectionString;
				// ReSharper restore PossibleNullReferenceException
				connection.Open();

				var lines = new List<string>();

				foreach(var line in (content ?? string.Empty).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
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