## GamerLinkApp 异步初始化优化
- 调整 `SqliteDataService.SeedDataAsync`，改用 `JsonSerializer.DeserializeAsync` 避免同步 I/O 阻塞启动线程。
- 在 `MapSeedUserAsync` / `HashPasswordAsync` 中引入 `Task.WhenAll` 并行哈希，缓解批量建库阶段 CPU 峰值。
- 保留原有业务默认值，同时为未来数据迁移预留可扩展配置。

## GamerLinkApp 管理端动态导航
- 基于 `IAuthService` 在 `AppShell` 中实现管理员/普通用户 Tab 动态组合。
- 扩展 `AdminDashboardPage` + `AdminDashboardViewModel`，新增服务编辑、精选开关与统计摘要。
- 完善依赖注入与路由映射，确保 `root/root` 管理员登录后 Shell 能即时刷新。

## GamerLinkApp 管理端订单看板
- 新增 `AdminOrdersViewModel` + `AdminOrdersPage`，支持状态筛选、全文检索与支付/取消等订单动作。
- 拓展 `IDataService`，在 `UpdateOrderStatusAsync` 上同步更新统计字段。
- 补充 EmptyView 与状态提示，让空数据场景也有明确反馈。

## GamerLinkApp 管理端 UX 强化
- 调整看板布局为 FlexLayout，自适应展示统计卡、操作按钮等控件。
- 通过 MAUI FilePicker 支持上传服务缩略图，并在异步处理中增加进度提示。
- 在个人中心补充管理员跳转入口，串联后台与前台的导航体验。

## GamerLinkApp 智能客服集成
- 新增 `SupportChatPage` + `SupportChatViewModel`，将“联系客服”入口接入 Gemini RAG 流程，并处理登录校验与滚动定位。
- 强化 `AppShell` 登出链路，监控 Handler 生命周期，避免 Tab 重建期间触发 Android fragment 崩溃。
- 重构 `RagService`：多渠道解析 `GEMINI_API_KEY`（环境变量、AppData、包内文件），在知识库加载失败时输出可读提示，并把语义检索阈值从 0.75 降到 0.2；同时重写 `knowledge_base.md` 为纯净问答 Markdown。此前由于 markdown 噪音和高阈值，像“如果我想退款怎么办？”这类提问无法命中上下文，调整后可稳定召回并生成引用回答。
