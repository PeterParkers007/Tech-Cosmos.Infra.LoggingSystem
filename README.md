# **LoggingSystem - 智能日志系统**

**Unity专业日志框架 - 提供分级、结构化、多输出日志能力，告别Debug.Log的混乱调试**

## 特性

### 智能分级系统
- **6级日志体系**：Trace、Debug、Info、Warning、Error、Critical
- **动态过滤**：开发期看细节，上线期只关注重要问题
- **分类管理**：按模块分类，精准定位问题

### 多输出通道
- **Unity控制台**：彩色输出，开发期直观查看
- **文件记录**：自动持久化，真机调试的黑匣子
- **网络传输**：线上监控，远程问题捕获
- **数据库存储**：长期分析，性能趋势监控

### 可视化配置
- **ScriptableObject驱动**：无需修改代码，配置即生效
- **灵活规则**：支持自定义命名模式、占位符替换

## 安装

### **通过 Unity Package Manager**
1. 打开 **Unity Package Manager**
2. 点击 **+ 按钮** - **Add package from git URL**
3. 输入：https://github.com/PeterParkers007/Tech-Cosmos.Infra.LoggingSystem.git

### **手动安装**
1. 下载**最新 Release**
2. 将 **Tech-Cosmos** 文件夹拖入项目的 **Assets 目录**

## 快速开始

**基础使用示例：**
```csharp
// 基础使用
LoggingSystem.Instance.Info("游戏启动完成", "System");
```
**分类日志示例：**
```csharp
// 分类日志
LoggingSystem.Instance.Debug("玩家位置更新", "Player");
LoggingSystem.Instance.Warn("资源加载较慢", "Resource");
```
**异常捕获示例：**
```csharp
// 异常捕获
LoggingSystem.Instance.Error($"操作失败: {ex.Message}", "Exception");
```
## 使用场景

### **单机游戏 - 真机问题排查**
```csharp
LoggingSystem.Instance.Info($"设备信息: {SystemInfo.deviceModel}", "System");
```
### **线上游戏 - 远程监控**
```csharp
LoggingSystem.Instance.Error("网络连接失败", "Network");
```
### **性能优化 - 卡顿分析**
```csharp
LoggingSystem.Instance.Warn($"加载过慢: {stopwatch.ElapsedMilliseconds}ms", "Performance");
```
## **目录结构**
```
**Assets**
└── Tech-Cosmos
    └── LoggingSystem
        ├── Runtime
        │   ├── LoggingSystem.cs
        │   ├── Outputs
        │   └── Models
        ├── Editor
        │   └── LoggingWindow.cs
        └── Samples
            ├── BasicUsage
            └── AdvancedConfigs
```
## **配置**

通过 **Create/Tech-Cosmos/Logging Config** 创建配置文件，可视化调整：
- **日志级别**
- **输出通道**
- **分类规则**
- **文件设置**

## **许可证**

**MIT License** - 详见 **LICENSE 文件**

---

**让调试从艺术变成科学**