# Ytdlgui for WinUI 3

[![build](https://github.com/minittupoyo/ytdlwinui/actions/workflows/build.yml/badge.svg)](https://github.com/minittupoyo/ytdlwinui/actions/workflows/build.yml)

`yt-dlp` を使って動画・音声を保存する、日本語 UI の Windows 向けデスクトップアプリです。
[minittupoyo/ytdlgui](https://github.com/minittupoyo/ytdlgui) の機能を C#、WinUI 3、MVVM で再実装しています。

## ダウンロード

[GitHub Releases](https://github.com/minittupoyo/ytdlwinui/releases/latest) から最新の
`Ytdlgui-v*-win-x64.zip` をダウンロードし、展開後の `YtdlWinUI.exe` を実行してください。
インストールや自己署名証明書の追加は不要です。

配布版には .NET と Windows App SDK が含まれます。初回起動時は単一 EXE 内のファイルを一時フォルダーへ展開するため、起動に少し時間がかかる場合があります。

## 主な機能

- MP4、MKV、MP3、AAC、FLAC への保存
- 動画解像度・音声品質の選択
- プレイリスト／アルバム単位の保存
- サムネイルの埋め込みと正方形クロップ
- ダウンロード進捗、ログ、キャンセル
- Firefox、Floorp、Zen のプロファイル検出と Cookie 利用
- 不足している `yt-dlp`、`ffmpeg`、`deno` の自動インストール

## ドキュメント

- [ドキュメント一覧](docs/README.md)
- [利用ガイド](docs/user-guide.md)
- [開発ガイド](docs/development.md)
- [リリース手順](docs/releasing.md)
- [コントリビューションガイド](CONTRIBUTING.md)

## ライセンス

本体は [MIT License](LICENSE) です。自動取得される外部ツールには各プロジェクトのライセンスが適用されます。詳しくは [第三者ソフトウェアに関する通知](THIRD_PARTY_NOTICES.md) を参照してください。

不具合報告や改善提案は [Issues](https://github.com/minittupoyo/ytdlwinui/issues) へお願いします。
