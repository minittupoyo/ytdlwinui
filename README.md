# Ytdlgui for WinUI 3

[![build](https://github.com/minittupoyo/ytdlwinui/actions/workflows/build.yml/badge.svg)](https://github.com/minittupoyo/ytdlwinui/actions/workflows/build.yml)

`yt-dlp` を使って動画・音声を保存する、日本語UIのWindows向けデスクトップアプリです。
[minittupoyo/ytdlgui](https://github.com/minittupoyo/ytdlgui) の機能を C#、WinUI 3、MVVM で再実装しています。

## 主な機能

- MP4、MKV、MP3、AAC、FLACへの保存
- 動画解像度・音声品質の選択
- プレイリスト／アルバム単位の保存
- サムネイルの埋め込みと正方形クロップ
- ダウンロード進捗、ログ、キャンセル
- Firefox、Floorp、Zenのプロファイル自動検出とCookie利用
- `%APPDATA%\ytdlgui\settings.json` への設定保存

## 動作要件

- Windows 10 バージョン1809以降、またはWindows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Windows App Development CLI](https://github.com/microsoft/WindowsAppSDK) 0.6以降
- Windowsの開発者モード
- `yt-dlp`、`ffmpeg`、`deno`（アプリ内から自動インストール可能）

不足している外部ツールは設定画面の「不足ツールを自動インストール」から導入できます。
公式GitHub Releaseから取得したファイルをSHA-256で検証し、
`%LOCALAPPDATA%\ytdlgui\bin` へ保存します。PATHは変更しません。
自動インストールはx64／ARM64版に対応しています。x86版では外部ツールを手動で用意してください。

手動で用意する場合はPATHに追加するか、ビルドされたアプリと同じ場所の `tools` フォルダーへ
`yt-dlp.exe`、`ffmpeg.exe`、`ffprobe.exe`、`deno.exe` を配置してください。

外部ツールの管理とダウンロード設定の初期化は、画面右下の歯車ボタンから開く
「アプリ設定」で行えます。設定初期化ではダウンロード済みツールやログは削除されません。

## ビルドと実行

```powershell
git clone https://github.com/minittupoyo/ytdlwinui.git
cd ytdlwinui\YtdlWinUI
.\BuildAndRun.ps1
```

通常のビルドのみ行う場合：

```powershell
dotnet build .\YtdlWinUI.csproj -p:Platform=x64
```

## ブラウザーCookie

設定画面で「Firefox」「Floorp」「Zen」とプロファイル名を選ぶと、各ブラウザーの
`profiles.ini` から実パスを解決し、次の形式で `yt-dlp` に渡します。

```text
--cookies-from-browser firefox:<profilepath>
```

Cookieはアプリ内へコピーせず、選択したプロファイルパスだけを設定に保存します。
共有PCでは設定ファイルとダウンロードログの取り扱いに注意してください。

## プロジェクト構成

```text
YtdlWinUI/
├─ Models/       設定・表示モデル
├─ Services/     yt-dlp実行、設定保存、ブラウザープロファイル検出
├─ ViewModels/   画面状態とコマンド
├─ Assets/       WinUI/MSIX用アセット
└─ MainPage.xaml メイン画面
```

## ライセンス

Ytdlgui本体は [MIT License](LICENSE) です。自動取得される外部ツールには各プロジェクトの
ライセンスが適用されます。特に公式 `yt-dlp.exe` と取得対象のFFmpegビルドはGPL系コードを含みます。
詳しくは [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。

## 現在の制限

- UI文字列は日本語固定です。
- 配布用MSIXとコード署名はまだ用意していません。

不具合報告や改善提案は [Issues](https://github.com/minittupoyo/ytdlwinui/issues) へお願いします。
