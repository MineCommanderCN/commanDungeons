# commanDungeons

## 关于玩家
一款由.NET Core平台和C#语言构建的**命令行**式地牢闯关游戏，具有强大的自定义模组包管理甚至是机器游玩脚本功能。

### 资料
- 教程&百科手册: [见此](https://github.com/MineCommanderCN/commanDungeons/wiki)

- 下载正式版: [见此](https://github.com/MineCommanderCN/commanDungeons/releases/latest)

### 如何安装&运行
1. 前往上面的链接下载适用于最新完整版（FullRelease），并解压。

2. commanDungeons是一款绿色软件，你可以直接运行获得的程序文档而无需进行安装操作。
运行游戏最简单的方法便是双击启动commanDungeons.exe，但这并不推荐，因为这只会临时启动系统的cmd作为终端，并在游戏退出后自动关闭。
推荐的方法是先启动终端（命令指示符或PowerShell）再运行游戏（`./commanDungeons.exe`）。

对于支持UWP功能的Windows用户，我们推荐的进行游戏的终端是Windows Terminal，它支持自定义外观和更平滑的字体。详见[此项目](https://github.com/microsoft/terminal)了解更多。

对于MacOS和Linux用户，你可以在发布页下载源代码后自行使用跨平台的.NET Core编译器进行构建，并附加仅内容包的发布版（DatapackOnly）进行游戏。

### 内容包
~~commanDungeons有社区维护的丰富内容包（我相信将来会有的）~~，原生内容包仅作初步体验游戏的功能。

前往论坛下载社区内容包：https://github.com/MineCommanderCN/commanDungeons/discussions/categories/datapacks-内容包

## 关于开发者
commanDungeons是一个开源项目，任何人都可以来此贡献代码和原创内容。

### 架设工作站
你需要使用Visual Studio 2012及更新版本作为IDE，且已安装“使用.NET平台的桌面开发”功能集。
之后，下载源代码并解压，打开src文件夹中的`commanDungeons.sln`解决方案，方便您进行修改代码及调试。

`archived`分支是使用C++语言编写的旧版本，已不再更新。
