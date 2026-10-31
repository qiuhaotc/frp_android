# Frp Android

支持在安卓系统上配置`frps`,`frpc`的运行

## Frp

`Frp`的相关信息可以在 `https://github.com/fatedier/frp` 找到, 本地我已经克隆到了 `D:\Code\frp`目录

## 参考

参考 `https://github.com/AceDroidX/frp-Android` 实现功能, 本地我克隆到了 `D:\Code\frp-Android`目录, 请主要关注这个项目是怎么通过shell执行frpc和frps的, 需要在该项目中实现一样的功能, 保证不会出现 permission denied的问题

## 实现

1. 一个用于`frpc`的配置, 通过配置frpc的配置点击启动frpc用于启动相应版本的frpc。构建前自动从上游稳定 Release 下载, 校验后缓存到 `src/obj/frp/<版本>/<ABI>/libfrpc.so`, 支持 `arm64-v8a` 和 `armeabi-v7a`, 并通过 `AndroidNativeLibrary` 打包到 APK。
2. 一个用于`frps`的配置, 通过配置frps的配置点击启动frps用于启动相应版本的frps。与 frpc 使用同一版本和下载流程, 缓存路径为 `src/obj/frp/<版本>/<ABI>/libfrps.so`, 不再维护 `Resources/Raw` 下的源二进制。
3. 配置文件(例如`frps.ini`, `frpc.ini`), 可以保存到程序自己的data目录

## 注意点

1. 请详细参考 `https://github.com/AceDroidX/frp-Android` 的shell command实现, 从而实现C#版本下的等效替代, 确保不会出现运行shell命令时出现问题, 例如`permission denied`
2. 配置文件及可执行文件最好放在程序自己的data目录, 参考`https://github.com/AceDroidX/frp-Android`, 仿照这个项目的实现
3. 对功能要点添加简要的注释
4. 添加发布android程序包的发布配置, 可以实现一键发布
5. 不要修改程序签名配置, 保留现有配置即可
