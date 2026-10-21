using System.ComponentModel.DataAnnotations;

namespace Server.Models.Dtos;

/// <summary>返回给客户端的笔记数据。</summary>
public record NoteDto(
    int Id,
    string Title,
    string? Content,
    bool IsPinned,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static NoteDto From(Note note) => new(
        note.Id,
        note.Title,
        note.Content,
        note.IsPinned,
        note.CreatedAt,
        note.UpdatedAt);
}

/// <summary>
/// 新建笔记的请求体。
/// 用普通 class 而非 record：record 的主构造函数参数无法可靠承载 DataAnnotations 校验元数据。
/// </summary>
public class CreateNoteRequest
{
    [Required(ErrorMessage = "标题不能为空")]
    [MaxLength(200, ErrorMessage = "标题最长 200 个字符")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(8000, ErrorMessage = "正文最长 8000 个字符")]
    public string? Content { get; set; }

    public bool IsPinned { get; set; }
}

/// <summary>更新笔记的请求体（全量覆盖）。</summary>
public class UpdateNoteRequest
{
    [Required(ErrorMessage = "标题不能为空")]
    [MaxLength(200, ErrorMessage = "标题最长 200 个字符")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(8000, ErrorMessage = "正文最长 8000 个字符")]
    public string? Content { get; set; }

    public bool IsPinned { get; set; }
}

/// <summary>统一分页返回结构。</summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
