## GamerLinkApp 异步初始化优化
- 改进 `SqliteDataService.SeedDataAsync`，将 JSON 反序列化换成 `JsonSerializer.DeserializeAsync`，避免 async 调用链上的同步 I/O。
- 引入 `MapSeedUserAsync` 与 `HashPasswordAsync`，使用 `Task.WhenAll` 并行处理种子用户并在后台线程执行密码哈希，减轻应用启动阶段的 CPU 阻塞。
- 保持原有业务逻辑与默认密码策略不变，提升初始化可扩展性，为未来更大的种子数据量做准备。

## GamerLinkApp 管理端动态导航
- 调整 `AppShell` 为基于 `IAuthService` 的动态 Tab 注入，管理员登录即呈现“服务管理 / 订单管理 / 用户管理”，普通用户保留“服务 / 专区 / 个人”。
- 新增 `AdminDashboardPage` + `AdminDashboardViewModel`，提供服务检索、编辑、精选切换与最近订单概览，同时保留管理员注销流程。
- 扩充 DI 与路由映射，并在种子数据中加入 `root/root` 管理员账号，验证身份切换时 Shell 可即时刷新。

## GamerLinkApp 订单管理中台
- 引入 `AdminOrdersViewModel` 与重构后的 `AdminOrdersPage`，支持状态筛选、关键字搜索、详情面板及“标记支付 / 待评价 / 完成 / 取消”等动作。
- 复用 `IDataService` 新增的 `UpdateOrderStatusAsync` 与已存在的 `MarkOrderAsPaidAsync`，实现订单状态闭环管理并同步统计数据。
- 列表与详情分栏布局配合 EmptyView/提示语，快速呈现订单信息并在无关联记录时输出“当前没有订单”。

## GamerLinkApp 管理后台 UX 强化
- 调整服务管理界面为上下布局并使用 FlexLayout，自适应展示统计卡片与操作按钮，解决窄屏截断问题。
- 为管理员支持上传头像/服务缩略图：集成 MAUI FilePicker、本地缓存与临时文件清理，提供预览与状态提示。
- 在用户管理页增加后台重置密码流程，扩展数据层接口并加入表单校验与命令可用性管理。
