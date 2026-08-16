NMEA MSFS BRIDGE - README
=========================

这个程序的作用
--------------
这个程序将 Microsoft Flight Simulator (MSFS) 与支持 NMEA 的应用程序连接起来（例如 XCSoar）。

简要工作流程：
1) 通过 SimConnect 从 MSFS 读取飞行遥测数据。
2) 将这些遥测数据转换为兼容的 NMEA 语句。
3) 通过 TCP 将 NMEA 流发布到 127.0.0.1:4353（可配置）。

输出的 NMEA 语句（当前兼容模式）：
- GPGGA
- GPRMC
- WIMWV（真风）

配置文件
--------
配置保存在 appsettings.json。

常见位置：
- 如果从发布目录运行：
  dist\NmeaMsfsBridge-win-x64\appsettings.json
- 如果从项目运行：
  NmeaMsfsBridge.App\appsettings.json

重要参数：
- Bridge.Telemetry.PollHz：遥测读取频率
- Bridge.Output.TransmitHz：NMEA 发送频率
- Bridge.Output.Host 和 Bridge.Output.Port：TCP 目标地址（默认 127.0.0.1:4353）

如何启动（不使用 VS Code）
--------------------------
1) 启动 Microsoft Flight Simulator。
2) 进入你下载程序的文件夹：
3) 运行：
   NmeaMsfsBridge.App.exe
4) 保持窗口打开（如果关闭窗口，桥接器将停止运行）。

快速检查
--------
启动成功后，你应该能看到类似以下消息：
NMEA MSFS2024 XCSoar Bridge 1.0.1
由 Juan Carlos Quijano Abad 创建 - 2026

正在设置桥接器：TCP 地址 127.0.0.1:4353，发送频率 5Hz，遥测频率 5Hz。
按 Ctrl+C 关闭应用程序

等待与模拟器建立连接
已连接到模拟器

与 XCSoar 的集成
-----------------
在 XCSoar 中将设备配置为 TCP 客户端：
- Host：127.0.0.1
- Port：4353
- Driver：Generic

如果 XCSoar 中没有风数据，请检查：
- 设备是否已连接。
- XCSoar 中是否启用了外部风选项。

停止程序
--------
- 关闭桥接器控制台窗口。
- 或在控制台中按 Ctrl+C。

说明
----
- 这个可执行文件是独立版（不需要打开 VS Code）。
- 如果修改了 appsettings.json，请重启可执行文件以应用更改。

发布新版本
----------
运行以下命令：
- git tag v1.0.2
- git push origin v1.0.2
