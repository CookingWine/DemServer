# 本地后台服务（Server）

面向**桌面客户端**的本地 HTTP 后台服务。启动后在本机监听固定端口，桌面程序通过 HTTP + JSON 调用，数据持久化到本地 SQLite 单文件数据库。

自带一个「笔记」示例模块（完整 CRUD + 分页 + 搜索），可以直接当模板替换成自己的业务。

## 技术栈

| 组件 | 选型 |
|---|---|
| 运行时 | .NET 8 (net8.0) |
| Web 框架 | ASP.NET Core 8，Controller 模式 |
| ORM | EF Core 8.0.11 |
| 数据库 | SQLite（单文件，零安装） |
| 接口文档 | Swashbuckle (Swagger UI) |

## 环境要求

- **.NET 8 SDK**（8.0.x 即可）
- macOS / Windows / Linux 均可

验证安装：

```bash
dotnet --version   # 应输出 8.0.x
```

> **本机注意**：SDK 安装在 `~/.dotnet` 而不是系统默认位置，命令行里可能报 `dotnet: command not found`。
> 临时生效：
> ```bash
> export DOTNET_ROOT="$HOME/.dotnet"
> export PATH="$DOTNET_ROOT:$PATH"
> ```
> 永久生效：把上面两行加到 `~/.zshrc`，然后 `source ~/.zshrc`。

## 快速开始

```bash
cd /Users/jiaqili/Desktop/Demo/Server

# 若 dotnet 未进 PATH，先执行这两行
export DOTNET_ROOT="$HOME/.dotnet" && export PATH="$DOTNET_ROOT:$PATH"

dotnet run
```

启动成功后终端会打印：

```
Now listening on: http://127.0.0.1:5080
```

三个入口可以立刻验证：

| 地址 | 用途 |
|---|---|
| http://127.0.0.1:5080/ | 服务信息（确认服务活着） |
| http://127.0.0.1:5080/swagger | **交互式接口文档，可直接点按钮调用** |
| http://127.0.0.1:5080/api/health | 健康检查（桌面端启动时探活） |

首次启动会自动创建数据库和表，无需手动初始化。

停止服务：终端按 `Ctrl + C`。

## 目录结构

```
Server/
├── Program.cs                            服务装配：数据库、CORS、异常处理、Swagger、启动建表
├── Server.csproj                         项目定义与依赖
├── appsettings.json                      端口、数据库连接串
├── appsettings.Development.json          开发环境日志级别
├── Properties/launchSettings.json        本地调试启动配置
├── Server.http                           请求集合（VS Code REST Client / Rider 可直接运行）
│
├── Data/
│   └── AppDbContext.cs                   DbContext、表配置、索引、UTC 转换
├── Models/
│   ├── Note.cs                           示例实体
│   └── Dtos/NoteDtos.cs                  请求/响应 DTO、分页结构
├── Controllers/
│   ├── NotesController.cs                CRUD + 分页 + 关键字搜索
│   └── HealthController.cs               健康检查
├── Infrastructure/
│   └── GlobalExceptionHandler.cs         全局异常兜底，统一输出 ProblemDetails
│
└── App_Data/                             运行时生成，SQLite 数据库所在目录
    └── server.db
```

## 接口说明

所有接口均返回 `application/json`，时间字段为 **UTC ISO 8601**（带 `Z` 后缀）。

| 方法 | 路径 | 说明 | 成功状态码 |
|---|---|---|---|
| GET | `/` | 服务信息 | 200 |
| GET | `/api/health` | 健康检查，含数据库连通性 | 200 / 503 |
| GET | `/api/notes` | 分页列表 | 200 |
| GET | `/api/notes/{id}` | 单条详情 | 200 / 404 |
| POST | `/api/notes` | 新建 | 201 |
| PUT | `/api/notes/{id}` | 全量更新 | 200 / 404 |
| DELETE | `/api/notes/{id}` | 删除 | 204 / 404 |

### 列表查询参数

| 参数 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `keyword` | string | 空 | 在标题和正文中模糊匹配 |
| `page` | int | 1 | 页码，小于 1 自动纠正为 1 |
| `pageSize` | int | 20 | 每页条数，合法范围 1~200，越界回落为 20 |

排序规则：**置顶优先，其次按更新时间倒序**。

### 请求/响应示例

**新建**

```bash
curl -X POST http://127.0.0.1:5080/api/notes \
  -H "Content-Type: application/json" \
  -d '{"title":"第一条笔记","content":"内容","isPinned":false}'
```

```json
{
  "id": 1,
  "title": "第一条笔记",
  "content": "内容",
  "isPinned": false,
  "createdAt": "2026-10-21T05:08:50.430463Z",
  "updatedAt": "2026-10-21T05:08:50.430463Z"
}
```

**列表**

```bash
curl "http://127.0.0.1:5080/api/notes?page=1&pageSize=10&keyword=笔记"
```

```json
{
  "items": [ { "id": 2, "title": "置顶笔记", "...": "..." } ],
  "page": 1,
  "pageSize": 10,
  "total": 2,
  "totalPages": 1
}
```

**更新**（全量覆盖，未传的字段会被重置）

```bash
curl -X PUT http://127.0.0.1:5080/api/notes/1 \
  -H "Content-Type: application/json" \
  -d '{"title":"改后的标题","content":"改后的内容","isPinned":true}'
```

**删除**

```bash
curl -X DELETE http://127.0.0.1:5080/api/notes/1   # 返回 204，无响应体
```

### 错误格式

统一使用 RFC 7807 `ProblemDetails`，客户端可以按 `status` 分支处理。

参数校验失败（400），`errors` 里是字段级中文提示：

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Title": ["标题不能为空"] },
  "traceId": "..."
}
```

资源不存在（404）：

```json
{ "title": "笔记不存在", "status": 404, "detail": "未找到 Id 为 999 的笔记。" }
```

未处理异常（500）统一由 `GlobalExceptionHandler` 兜底。**Development 环境**下 `detail` 会带堆栈便于排查，**Production 环境**下只返回通用提示，不泄漏内部细节。

## 配置

### 修改端口

优先级从高到低：

1. 命令行参数：`dotnet run --urls http://127.0.0.1:5090`
2. 环境变量：`ASPNETCORE_URLS=http://127.0.0.1:5090`
3. `appsettings.json` 的 `Urls` 字段（默认 `http://127.0.0.1:5080`）
4. 调试时 `Properties/launchSettings.json` 的 `applicationUrl`

绑定地址说明：

- `127.0.0.1` → **仅本机可访问**（默认，最安全）
- `0.0.0.0` → 允许局域网内其他设备访问，**请确认确实需要，并配合鉴权使用**

### 修改数据库位置

`appsettings.json` 的 `ConnectionStrings:Default`：

```json
{
  "ConnectionStrings": { "Default": "Data Source=App_Data/server.db" }
}
```

相对路径会基于**项目根目录**解析成绝对路径，因此从不同工作目录启动都不会生成多个 db 文件。填绝对路径则原样使用。

> 目录名特意用 `App_Data` 而不是 `data`：macOS 文件系统大小写不敏感，`data` 会和代码目录 `Data/` 撞成同一个，误删时连代码一起没了。

## 数据库

- 首次启动自动建库建表，文件位于 `App_Data/server.db`
- SQLite 会产生 `server.db-wal` / `server.db-shm` 两个附属文件，属正常现象
- 当前策略：有 EF 迁移文件时执行 `Migrate()`，没有则用 `EnsureCreated()` 兜底

**改表结构时**，推荐改用正式迁移：

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"

# 每次改完实体后
dotnet ef migrations add AddSomething
dotnet ef database update
```

有迁移文件后，启动逻辑会自动切到 `Migrate()`。

> 开发阶段如果只是试表结构，直接删掉 `App_Data/` 重启即可重建。

## 发布与分发

### 依赖目标机安装 .NET 8 Runtime

```bash
dotnet publish -c Release -o out
```

### 独立部署（目标机无需安装 .NET）

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out

# macOS Apple Silicon
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o out

# macOS Intel
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o out
```

首次执行会联网下载对应平台的 runtime pack，耗时较长。

**发布后注意两点：**

1. 发布产物默认是 **Production** 环境，`/swagger` 不可用（接口本身正常工作）。需要文档时用 `ASPNETCORE_ENVIRONMENT=Development` 启动。
2. `ContentRootPath` 变成产物目录，数据库会在 `out/App_Data/` 下新建。要做数据迁移的话，记得把原库一起带走。

启动产物：

```bash
./out/Server                    # macOS / Linux
out\Server.exe                  # Windows
```

## 桌面客户端调用示例

C# 用 `HttpClient` + `System.Text.Json`，注意开启**大小写不敏感**（接口是 camelCase，C# 属性是 PascalCase）：

```csharp
using System.Net.Http.Json;
using System.Text.Json;

public sealed class ServerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public ServerClient(string baseUrl = "http://127.0.0.1:5080")
        => _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };

    /// 启动时先探活，确认后台服务就绪
    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/api/health", ct);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;   // 服务没起来
        }
    }

    public async Task<PagedResult<NoteDto>?> ListNotesAsync(
        string? keyword = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = $"/api/notes?page={page}&pageSize={pageSize}"
                  + (string.IsNullOrWhiteSpace(keyword) ? "" : $"&keyword={Uri.EscapeDataString(keyword)}");

        return await _http.GetFromJsonAsync<PagedResult<NoteDto>>(url, JsonOptions, ct);
    }

    public async Task<NoteDto?> CreateNoteAsync(
        string title, string? content = null, bool isPinned = false, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/notes",
            new { title, content, isPinned }, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<NoteDto>(JsonOptions, ct);
    }

    public async Task DeleteNoteAsync(int id, CancellationToken ct = default)
        => (await _http.DeleteAsync($"/api/notes/{id}", ct)).EnsureSuccessStatusCode();
}

public record NoteDto(int Id, string Title, string? Content, bool IsPinned,
                      DateTime CreatedAt, DateTime UpdatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPages);
```

## 常见问题

**`dotnet: command not found`**
SDK 装在 `~/.dotnet`，按「环境要求」里的说明加到 PATH。

**启动报端口被占用（`Address already in use`）**
查占用进程并结束，或换端口启动：

```bash
lsof -nP -iTCP:5080 -sTCP:LISTEN      # 看是谁占了
dotnet run --urls http://127.0.0.1:5090
```

**客户端请求被拦（浏览器里报 CORS）**
纯桌面程序（WPF / WinForms）不受 CORS 限制。如果是 Electron / WebView2 内嵌页面，服务端已配置放开所有来源；即便如此仍报错，检查是否请求到了错误端口。

**`/swagger` 打不开**
Swagger 只在 Development 环境注册。用 `dotnet run`（默认 Development）启动，或显式设置 `ASPNETCORE_ENVIRONMENT=Development`。

**时间少了 8 小时**
接口返回的是 UTC。客户端展示时转本地时间：`createdAt.ToLocalTime()`。

**`dotnet restore` 很慢或中断**
首次需要从 nuget.org 拉包。确保网络通畅后重试；包已缓存在 `~/.nuget/packages`，后续会快很多。
