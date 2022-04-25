// source: https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/Data/

using LocalSmtp.Server.Infrastructure.Models;
using LocalSmtp.Server.Infrastructure.ValueConverters;
using Microsoft.EntityFrameworkCore;

namespace LocalSmtp.Server.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        UtcDateTimeValueConverter.Apply(modelBuilder);

        modelBuilder.Entity<MessageRelay>()
            .HasOne(r => r.Message)
            .WithMany(x => x.Relays)
            .HasForeignKey(x => x.MessageId)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<MessageRelay> MessageRelays { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<ImapState> ImapState { get; set; }
}
