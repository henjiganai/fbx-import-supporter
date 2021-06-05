# fbx-import-supporter

## 概要
fbxをインポート後に設定しないといけないブレンドシェイプの法線やアニメーションタイプをインポート時に自動設定してくれるツールです  
Extract Materialsもいずれ自動化する予定です  

## 使い方
- unitypackageをインポートするか[リポジトリ](https://github.com/henjiganai/fbx-import-supporter)をcloneしてUnityProjectsのAsset内にfbx-import-supporterを格納
- fbxを格納するフォルダ内の直下に`fbxautoimport`（拡張子なし）という名称のファイルを作成
- fbxをインポート

## 機能説明
FBXImportSupporterの自動設定有効時に設定される内容とConsoleへ通知される内容一覧です

### 設定内容一覧
- Blend Shape Normas: None
- AnimationType: Humanoid

### 表示内容一覧
- Mecanimのボーン名とモデルのボーン名一覧を表示  
  ※Mecanimのボーン名とモデルのボーン名が完全一致しない場合、Consoleに青文字で表示

### 注意点
- 既にインポートしてあるモデルを再インポートする場合は自動設定およびConsoleへの通知が行われません
