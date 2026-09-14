# Ytdlgui for WinUI 3

`yt-dlp` を使って動画・音声を保存する Windows 向け WinUI 3 アプリです。
[minittupoyo/ytdlgui](https://github.com/minittupoyo/ytdlgui) の機能を C# / WinUI 3 / MVVM で再実装しています。

## 必要なコマンド

- `yt-dlp`
- `ffmpeg`
- `deno`

アプリの出力フォルダーに `tools` フォルダーを作り、各 `.exe` を置くか、PATH から参照できるようにしてください。

## 実行

```powershell
.\BuildAndRun.ps1
```

設定は元アプリと同じ `%APPDATA%\ytdlgui\settings.json` に保存されます。

## Firefox系ブラウザーのCookie

設定画面の「ブラウザーCookie」で Firefox、Floorp、Zen とプロファイル名を選択できます。
各ブラウザーの `profiles.ini` からプロファイルの実パスを検出し、ダウンロード時に
`--cookies-from-browser firefox:<profilepath>` として `yt-dlp` に渡します。
