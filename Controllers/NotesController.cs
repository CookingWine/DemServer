using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.Dtos;

namespace Server.Controllers;

/// <summary>
/// 笔记 CRUD 接口，可作为桌面客户端对接的示例。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotesController(AppDbContext db) => _db = db;

    /// <summary>分页查询笔记，支持关键字模糊搜索，置顶优先、按更新时间倒序。</summary>
    /// <param name="keyword">标题或正文包含的关键字，可空。</param>
    /// <param name="page">页码，从 1 开始。</param>
    /// <param name="pageSize">每页条数，1~200。</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NoteDto>>> List(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var query = _db.Notes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            var pattern = $"%{kw}%";
            query = query.Where(n =>
                EF.Functions.Like(n.Title, pattern) ||
                (n.Content != null && EF.Functions.Like(n.Content, pattern)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoteDto(n.Id, n.Title, n.Content, n.IsPinned, n.CreatedAt, n.UpdatedAt))
            .ToListAsync(ct);

        return Ok(new PagedResult<NoteDto>(items, page, pageSize, total));
    }

    /// <summary>按 Id 获取单条笔记。</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> GetById(int id, CancellationToken ct)
    {
        var note = await _db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
        if (note is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "笔记不存在",
                Detail = $"未找到 Id 为 {id} 的笔记。",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(NoteDto.From(note));
    }

    /// <summary>新建笔记。</summary>
    [HttpPost]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NoteDto>> Create([FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Title = request.Title.Trim(),
            Content = request.Content,
            IsPinned = request.IsPinned,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Notes.Add(note);
        await _db.SaveChangesAsync(ct);

        var dto = NoteDto.From(note);
        return CreatedAtAction(nameof(GetById), new { id = note.Id }, dto);
    }

    /// <summary>全量更新一条笔记。</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> Update(int id, [FromBody] UpdateNoteRequest request, CancellationToken ct)
    {
        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (note is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "笔记不存在",
                Detail = $"未找到 Id 为 {id} 的笔记。",
                Status = StatusCodes.Status404NotFound
            });
        }

        note.Title = request.Title.Trim();
        note.Content = request.Content;
        note.IsPinned = request.IsPinned;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(NoteDto.From(note));
    }

    /// <summary>删除一条笔记。</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (note is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "笔记不存在",
                Detail = $"未找到 Id 为 {id} 的笔记。",
                Status = StatusCodes.Status404NotFound
            });
        }

        _db.Notes.Remove(note);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
