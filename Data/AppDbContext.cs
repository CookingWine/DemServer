using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Server.Models;

namespace Server.Data;

public class AppDbContext : DbContext
{
    /// <summary>
    /// SQLite 不保存 DateTime 的 Kind，读回来是 Unspecified，序列化成 JSON 时会丢掉 "Z" 后缀，
    /// 客户端就会把它当本地时间。这里统一在读写两侧锁定为 UTC。
    /// </summary>
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("Notes");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Content)
                  .HasMaxLength(8000);

            entity.Property(x => x.CreatedAt).HasConversion(UtcConverter);
            entity.Property(x => x.UpdatedAt).HasConversion(UtcConverter);

            // 列表默认按「置顶 + 更新时间」排序，加复合索引减少全表扫描。
            entity.HasIndex(x => new { x.IsPinned, x.UpdatedAt });
        });
    }
}
