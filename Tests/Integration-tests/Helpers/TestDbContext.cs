using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Helpers
{
	public class TestDbContext : DbContext
	{
		#region Properties

		public virtual string ConnectionString { get; set; }

		#endregion

		#region Methods

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlServer(this.ConnectionString);

			base.OnConfiguring(optionsBuilder);
		}

		#endregion
	}
}