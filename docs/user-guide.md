# 利用ガイド

## 動作要件

- Windows 10 バージョン 1809 以降、または Windows 11
- `yt-dlp`、`ffmpeg`、`deno`

GitHub Releases の配布版は Windows x64 向けです。アプリ本体に .NET と Windows App SDK が含まれるため、別途インストールする必要はありません。

## 外部ツールを準備する

設定画面の「不足ツールを自動インストール」から必要なツールを導入できます。アプリは公式 GitHub Release から取得したファイルを SHA-256 で検証し、`%LOCALAPPDATA%\ytdlgui\bin` に保存します。PATH は変更しません。

自動インストールは x64／ARM64 版に対応しています。x86 版では外部ツールを手動で用意してください。

GUIから導入できない場合は、配布ZIPに含まれる `install-tools.bat` を実行してください。
公式Releaseから `yt-dlp`、`deno`、`ffmpeg`、`ffprobe` を取得してSHA-256を検証し、
GUIと同じ `%LOCALAPPDATA%\ytdlgui\bin` へ配置します。管理者権限やPATHの変更は不要です。

手動で用意する場合は PATH に追加するか、ビルドされたアプリと同じ場所の `tools` フォルダーへ次のファイルを配置します。

- `yt-dlp.exe`
- `ffmpeg.exe`
- `ffprobe.exe`
- `deno.exe`

外部ツールの管理とダウンロード設定の初期化は、画面右下の歯車ボタンから開く「アプリ設定」で行えます。設定を初期化しても、ダウンロード済みツールやログは削除されません。

## 設定ファイル

設定は `%APPDATA%\ytdlgui\settings.json` に保存されます。共有 PC では設定ファイルとダウンロードログの取り扱いに注意してください。

## ブラウザー Cookie

設定画面で Firefox、Floorp、Zen とプロファイル名を選択できます。アプリは各ブラウザーの `profiles.ini` から実際のプロファイルパスを解決し、次の形式で `yt-dlp` に渡します。

```text
--cookies-from-browser firefox:<profilepath>
```

Cookie 自体はアプリ内へコピーされません。設定ファイルには選択したプロファイルパスだけが保存されます。

## 現在の制限

- UI 文字列は日本語固定です。
- GitHub Releases の配布版は Windows x64 向けです。
