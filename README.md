# WinFramework - Windows 桌面网络助手

这是一个基于 WinForms 的最小桌面应用（.NET 10），用于抓取网页、解析出标题、meta 描述与正文，并生成简易摘要。项目采用 HtmlAgilityPack 进行 HTML 解析，HttpClient 进行网络请求。

主要功能：
- 在 UI 中输入 URL 并抓取页面
- 显示 HTTP 状态码、页面标题、meta 描述
- 提取可见文本并生成简短摘要（按句子截取前 5 句）
- 将结果导出为 JSON 文件或复制到剪贴板

构建与运行：
1. 安装 .NET 10 SDK（示例目标为 net10.0-windows）
2. 在仓库根目录运行：
   dotnet build ./WinFramework/WinFramework.csproj
   dotnet run --project ./WinFramework/WinFramework.csproj


LICENSE: MIT
