
using Domain;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Persistence.Context

{
    public class ChatContext : DbContext
    {
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<MessageEntity> Messages { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseSqlite("Data Source = DB\\chat.db");
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            ConfigurateUsers(builder);
            ConfigurateMessage(builder);
        }

        private static void ConfigurateMessage(ModelBuilder builder)
        {
            builder.Entity<MessageEntity>().HasKey(x => x.Id);
            builder.Entity<MessageEntity>().Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Entity<MessageEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.SenderId);
            builder.Entity<MessageEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.RecepientId);
        }

        private static void ConfigurateUsers(ModelBuilder builder)
        {
            builder.Entity<UserEntity>().HasKey(x=>x.Id);
            builder.Entity<UserEntity>().Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Entity<UserEntity>().HasIndex(x => x.Name).IsUnique();
        }
    }
}
