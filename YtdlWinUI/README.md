# Ytdlgui for WinUI 3

`yt-dlp` を使って動画・音声を保存する Windows 向け WinUI 3 アプリです。
[minittupoyo/ytdlgui](https://github.com/minittupoyo/ytdlgui) の機能を C# / WinUI 3 / MVVM で再実装しています。

## 外部ツール

- `yt-dlp`
- `ffmpeg`
- `deno`

不足時は設定画面から公式リリースを自動取得できます。SHA-256検証後、
`%LOCALAPPDATA%\ytdlgui\bin` へ配置されます。手動配置やPATH上のツールも利用できます。

## 実行

```powershell
.\BuildAndRun.ps1
```

設定は元アプリと同じ `%APPDATA%\ytdlgui\settings.json` に保存されます。

## Firefox系ブラウザーのCookie

設定画面の「ブラウザーCookie」で Firefox、Floorp、Zen とプロファイル名を選択できます。
各ブラウザーの `profiles.ini` からプロファイルの実パスを検出し、ダウンロード時に
`--cookies-from-browser firefox:<profilepath>` として `yt-dlp` に渡します。
