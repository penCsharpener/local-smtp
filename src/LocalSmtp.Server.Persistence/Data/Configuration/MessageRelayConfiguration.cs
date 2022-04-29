using LocalSmtp.Server.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalSmtp.Server.Infrastructure.Data.Configuration;

public class MessageRelayConfiguration : IEntityTypeConfiguration<MessageRelay>
{
    public void Configure(EntityTypeBuilder<MessageRelay> builder)
    {
        builder.HasOne(r => r.Message)
            .WithMany(x => x.Relays)
            .HasForeignKey(x => x.MessageId)
            .IsRequired();
    }
}
