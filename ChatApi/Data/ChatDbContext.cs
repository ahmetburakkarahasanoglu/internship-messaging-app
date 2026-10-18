using ChatApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Data;

public sealed class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(conversation => conversation.Id);
            entity.Property(conversation => conversation.Title).HasMaxLength(100).IsRequired();
            entity.Property(conversation => conversation.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Sender).HasMaxLength(50).IsRequired();
            entity.Property(message => message.Content).HasMaxLength(2000).IsRequired();
            entity.Property(message => message.SentAt).IsRequired();
            entity.HasIndex(message => message.ConversationId);
            entity.HasOne(message => message.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
