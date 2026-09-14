# リリース手順

リリースは `.github/workflows/release.yml` により自動化されています。`v1.2.3` 形式のタグを push すると、テスト、Windows x64 向け配布物の作成、GitHub Release の公開が行われます。

## 1. バージョンを更新する

`YtdlWinUI/YtdlWinUI.csproj` の次の値を同じリリース番号へ更新します。

- `Version`（例: `1.2.3`）
- `AssemblyVersion`（例: `1.2.3.0`）
- `FileVersion`（例: `1.2.3.0`）

## 2. ローカルで確認する

```powershell
dotnet test .\YtdlWinUI.Tests\YtdlWinUI.Tests.csproj --configuration Release
dotnet build .\YtdlWinUI\YtdlWinUI.csproj --configuration Release -p:Platform=x64 -r win-x64
```

バージョン変更を main ブランチへ反映してからタグを作成します。

## 3. タグを push する

```powershell
git tag v1.2.3
git push origin v1.2.3
```

タグから先頭の `v` を除いた値とプロジェクトの `Version` が一致しない場合、ワークフローは公開前に停止します。

## 生成される配布物

- `Ytdlgui-v1.2.3-win-x64.zip`
- `SHA256SUMS.txt`

ZIP には自己完結型の単一 EXE、GUIが使えない場合の `install-tools.bat` と
`Install-Tools.ps1`、`LICENSE`、`README.md`、`THIRD_PARTY_NOTICES.md` が含まれます。
既に同じタグの GitHub Release がある場合は、添付ファイルが置き換えられます。
