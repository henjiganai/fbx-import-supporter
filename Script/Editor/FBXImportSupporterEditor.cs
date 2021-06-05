using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.Linq;

public class FBXImportSupporter : AssetPostprocessor
{
    const string IMPORT_FILE_NAME = "fbxautoimport";
    void OnPreprocessModel()
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        bool importSettingsMissing = modelImporter.importSettingsMissing;
        string folderPath = Path.GetDirectoryName(assetPath);
        string importFilePath = folderPath + "/" + IMPORT_FILE_NAME;
        bool isAutoImport = File.Exists(importFilePath);

        Debug.Log("インポート対象のfbx：" + assetPath);
        Debug.Log("インポート先のディレクトリ" + importFilePath);

        // 初回インポートのみ
        if (!importSettingsMissing)
        {
            return;
        }

        // 自動インポートが有効化されていない場合は処理を行わない
        if (!isAutoImport)
        {
            return;
        }

        Debug.Log("fbxインポート時自動化設定：有効");
        modelImporter.importBlendShapeNormals = ModelImporterNormals.None;
        modelImporter.animationType = ModelImporterAnimationType.Human;
    }

    void OnPostprocessModel(GameObject gameObject)
    {
        GetHumanDescription(gameObject);
    }

    void GetHumanDescription(GameObject gameObject)
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        bool importSettingsMissing = modelImporter.importSettingsMissing;

        // 初回インポートのみ
        if (!importSettingsMissing)
        {
            return;
        }

        HumanDescription humanDescription = modelImporter.humanDescription;
        Debug.Log("Mecanimのマッピング");

        foreach (HumanBone humanBone in humanDescription.human)
        {
            bool isNotMatch = humanBone.humanName.Equals(humanBone.boneName);
            // Mecanimのボーン名とモデルのボーン名が完全一致しない場合consoleで青文字になるように設定
            var decorationColor = isNotMatch ? "<color=black>" : "<color=blue>";

            Debug.Log(humanBone.humanName + ":" + $"{decorationColor}{humanBone.boneName}</color>");
        }
    }
}
