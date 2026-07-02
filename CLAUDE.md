# FrostNova 開発メモ（Claude 用）

## NuGet パッケージ公開手順（バッチ3種）

FrostNova の各パッケージを nuget.org に公開する手順。**リポジトリルート（このファイルと同じ `FrostNova\FrostNova` ディレクトリ）をカレントにして**実行すること。

### 事前準備
1. 対象プロジェクトの `.csproj` の `<Version>` を上げる（例: `FrostNova.Core.Wpf` の `1.4.0` → `1.4.1`）。
2. **Release ビルド**して nupkg を生成する。出力先は `Directory.Build.props` の `PackageOutputPath = $(SolutionDir).localNuget`。
   ```
   dotnet build <対象>.csproj -c Release
   ```
   ※ Debug ビルドはプロジェクト参照になり nupkg を生成しない。必ず Release。

### 実行順（この順で起動する）
1. **`copy_nuget.exe`** — `.localNuget` の**安定版のみ**（`dev` を含む名前は除外）を `publish_packages\` へコピー。
   - 内部で `AppContext.BaseDirectory`（= exe の場所）基準でパス解決する。
2. **`delete_old_pkg.exe`** — `publish_packages\` と `.localNuget\` の両方で、パッケージ ID 毎に**新しい 2 件だけ残して**古い版・プレリリースを削除。
   - このツールは**カレントディレクトリ基準**でパス解決する（copy_nuget と挙動が異なる）。必ず `FrostNova\FrostNova` をカレントにして実行すること。別ディレクトリで実行するとそのディレクトリの `.localNuget` を誤って掃除してしまう。
3. **`upload_nuget_local.cmd`** — `NUGET_API_KEY` を設定して `upload_nuget.cmd` を呼び、`publish_packages\*.nupkg` を nuget.org へ `--skip-duplicate` で一括 push。
   - `upload_nuget.cmd` を直叩きしないこと（APIキー未設定でエラー終了する）。ラッパーの `upload_nuget_local.cmd` を実行する。
   - 既存バージョンは `Conflict`（既に存在）でスキップされ、新規版のみ実際に push される。

### ⚠️ 実行に関する厳守事項
- **これらのバッチ（特に手順3の push）はユーザーの明示的な許可なしに実行しないこと。** nuget.org への公開は取り消しがほぼ不可能（unlist はできるが削除は原則不可）。
- 手順1・2はローカルのファイル削除を伴う。誤ったカレントで実行すると別リポジトリの `.localNuget` を壊すため、実行前にカレントディレクトリを必ず確認する。
- `upload_nuget_local.cmd` の1行目に **NuGet API キーが平文でハードコード**されている。コミット・共有・ログ出力時に露出しないよう注意（環境変数化を推奨）。

### 自動化環境での既知の注意点
- 3種ともコンソール待ち（`pause` / `Console.ReadKey`）で終わる。非対話実行では標準入力をリダイレクト（`< nul`）して抜ける。exe 側の `ReadKey` はリダイレクト時に例外を投げるが、本処理は例外前に完了している。
- この環境の `cmd` はカレントディレクトリのバッチ／exe を名前解決しないことがある。`call .\upload_nuget_local.cmd` のように `.\` を付けるか、対象ディレクトリを `PATH` に一時追加する。
