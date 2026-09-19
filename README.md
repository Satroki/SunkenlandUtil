# SunkenlandUtil

沉没之地功能性Mod
https://www.nexusmods.com/sunkenland/mods/7
https://www.bilibili.com/read/cv26380665/

UI库 https://github.com/GlossMod/UnityGameUI

## 生成与部署

生成后会自动把 `SunkenlandUtil.dll` 复制到 BepInEx 插件目录（`CopyToModFolder` 目标）：

```
dotnet build SunkenlandUtil/SunkenlandUtil.csproj -c Release
```

目标目录按以下优先级确定：

| 方式 | 用法 |
| --- | --- |
| 命令行参数 | `dotnet build -p:ModPluginsDir="D:\...\BepInEx\plugins"` |
| 环境变量 | `set SUNKENLAND_MOD_DIR=D:\...\BepInEx\plugins` |
| 默认值 | `...\Steam\steamapps\common\Sunkenland\BepInEx\plugins` |

其他开关：

- 只生成不复制：`dotnet build -p:CopyToModFolder=false`
- 游戏运行时插件 DLL 被占用，复制会失败并报错（已部署的文件不受影响）：先关闭游戏，或用上面的开关跳过复制。
- `Debug` 配置下面板会显示半透明红色调试底色，正式使用请用 `-c Release`。
