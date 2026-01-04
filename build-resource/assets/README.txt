详细说明见 https://github.com/qzrs777/AIChat 的 README。

===

【安装说明】

安装步骤：

1. 将压缩包内的 BepInEx_win_x64_5.4.23.4 下的所有文件解压到游戏根目录
   （包括 BepInEx 文件夹、winhttp.dll、doorstop_config.ini 等）

2. 运行一次游戏，生成必要的目录结构

3. 把 AIChat.dll 放入 BepInEx/plugins，进游戏按 F9 设置 API Key 等

注意：Linux 用户需要在 Steam 的游戏设置里，将启动选项填写为：
WINEDLLOVERRIDES="winhttp=n,b" %command%

===

语音配置（可选）：

同时也要开启 GPT-SoVITS 的 WebAPI v2（api_v2.py）。
对于 Windows 用户，如果 GPT-SoVITS 根目录下没有 bat 脚本，
可以创建一个 txt，把以下代码粘贴进去，然后后缀改成.bat运行
@echo off
.\runtime\python.exe api_v2.py -a 127.0.0.1 -p 9880
pause

想使用语音输入功能，需要把 GPT-SoVITS 根目录中的 api_v2.py 替换成当前文件夹里的 api_v2_ex.py（记得重命名），
或者直接把 api_v2_ex.py 复制过去，然后将脚本中的 api_v2.py 改为 api_v2_ex.py，例如
@echo off
.\runtime\python.exe api_v2_ex.py -a 127.0.0.1 -p 9880
pause
