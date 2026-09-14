# 開発ガイド

## 必要な環境

- .NET 10 SDK
- Windows App Development CLI 0.6 以降
- Windows の開発者モード

## ビルドして実行する

```powershell
git clone https://github.com/minittupoyo/ytdlwinui.git
cd ytdlwinui\YtdlWinUI
.\BuildAndRun.ps1
```

ビルドだけを行う場合は、リポジトリのルートで次を実行します。

```powershell
dotnet build .\YtdlWinUI\YtdlWinUI.csproj -p:Platform=x64
```

## テスト

```powershell
dotnet test .\YtdlWinUI.Tests\YtdlWinUI.Tests.csproj --configuration Release
```

UI を変更した場合は `YtdlWinUI\BuildAndRun.ps1` で起動し、ライト／ダークテーマとキーボード操作も確認してください。UI 自動テストは次のスクリプトから実行できます。

```powershell
.\YtdlWinUI\ui-tests.ps1
```

## プロジェクト構成

```text
YtdlWinUI/
├─ Models/       設定・表示モデル
├─ Services/     yt-dlp 実行、設定保存、ブラウザープロファイル検出
├─ ViewModels/   画面状態とコマンド
├─ Assets/       WinUI／MSIX 用アセット
└─ MainPage.xaml メイン画面

YtdlWinUI.Tests/ サービス層の自動テスト
docs/            利用者・開発者向けドキュメント
```

開発への参加方法は [コントリビューションガイド](../CONTRIBUTING.md)、公開手順は [リリース手順](releasing.md) を参照してください。
