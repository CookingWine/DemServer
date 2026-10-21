namespace Server.Models;

/// <summary>
/// 示例业务实体：一条笔记。
/// 换成你自己的业务实体时，同步修改 Data/AppDbContext.cs 里的 DbSet 与配置即可。
/// </summary>
public class Note
{
    public int Id { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>正文，可为空。</summary>
    public string? Content { get; set; }

    /// <summary>是否置顶。</summary>
    public bool IsPinned { get; set; }

    /// <summary>创建时间（UTC）。注意用 DateTime 而非 DateTimeOffset：SQLite 不支持对 DateTimeOffset 排序。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最后更新时间（UTC）。</summary>
    public DateTime UpdatedAt { get; set; }
}
