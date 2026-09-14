# Third-party software notices

Ytdlguiは以下のソフトウェアをリポジトリや配布パッケージへ同梱しません。
ユーザーが自動インストールを実行した場合に限り、各プロジェクトの公式GitHub Releaseから
ユーザーのコンピューターへ直接ダウンロードします。各ソフトウェアにはYtdlguiのMIT Licenseではなく、
それぞれのライセンスが適用されます。

## yt-dlp

- Project: https://github.com/yt-dlp/yt-dlp
- License information: https://github.com/yt-dlp/yt-dlp#licensing
- Downloaded artifact: Windows standalone executable

yt-dlpのソースはThe Unlicenseですが、PyInstaller版Windows実行ファイルはGPLv3+コードを含み、
結合された配布物にはGPLv3+が適用されるとyt-dlpプロジェクトが説明しています。

## FFmpeg

- Build project: https://github.com/yt-dlp/FFmpeg-Builds
- FFmpeg legal information: https://ffmpeg.org/legal.html
- Downloaded artifact: GPL Windows build containing `ffmpeg.exe` and `ffprobe.exe`

FFmpegのライセンスはビルド構成に依存します。Ytdlguiの自動インストール機能は、
yt-dlpプロジェクトが公開するGPLビルドを取得します。

## Deno

- Project: https://github.com/denoland/deno
- License: MIT License
- Downloaded artifact: Windows release archive

各プロジェクトの最新のライセンス条件と第三者通知は、ダウンロードされたリリースに付属する情報を確認してください。
