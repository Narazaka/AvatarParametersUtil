# Avatar Parameters Util

VRC Expressions Parameter Utility

## インストール

### VCCによる方法

1. https://vpm.narazaka.net/ から「Add to VCC」ボタンを押してリポジトリをVCCにインストールします。
2. VCCでSettings→Packages→Installed Repositoriesの一覧中で「Narazaka VPM Listing」にチェックが付いていることを確認します。
3. アバタープロジェクトの「Manage Project」から「Avatar Parameters Util」をインストールします。

## 更新履歴

- 2.1.4
  - null check
- 2.1.3
  - indentLevelが0でない時に`AvatarParametersUtilEditor.ShowParameterNameField`で型名が出ない問題を修正
- 2.1.2
  - アバターの外で実行されたときにエラーが出ないように修正
  - 空のパラメーターを表示しないように修正
- 2.1.1
  - labelがnullの時のエラーを回避
- 2.1.0
  - `AvatarParametersUtilEditor.ShowParameterNameField`で表示するパラメーターをフィルター出来るように
- 2.0.4
  - マニフェストにchangelogUrlを追加
- 2.0.3
  - internal (内部値/自動リネーム) なパラメーターを表示しないように修正
- 2.0.2
  - ビルドの問題を修正
- 2.0.1
  - NDMF Parameter Provider対応
- 1.0.5
  - MA Parametersのremapを正しく扱うように修正
  - 非アクティブなコンポーネントから値を取っていなかった問題を修正
- 1.0.4
  - Unityバージョンを指定
- 1.0.3
  - PhysBoneのパラメーターを取っていなかった問題を修正
- 1.0.2
  - アップロード不能だった不具合を修正
- 1.0.1
  - アバターがnullの場合にエラーにならないように
- 1.0.0
  - リリース

## License

[Zlib License](LICENSE.txt)
