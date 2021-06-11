using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;
using System.Linq;

public class FBXImportSupporter : AssetPostprocessor
{
    /// <summary>
    /// <para>fbxインポート時に自動設定を有効化させる判定ファイル名</para>
    /// この定数を変更することで認識させる判定ファイル名を変更することが可能です
    /// </summary>
    const string IMPORT_FILE_NAME = "fbxautoimport";

    /// <summary>
    /// <para>fbxインポート時にマテリアルを自動生成するかのフラグ</para>
    /// 不要な場合はfalseに設定してください
    /// </summary>
    const bool IS_CREATE_MATERIALS = true;

    /// <summary>
    /// <para>fbxインポート時に作成されるマテリアルを格納するフォルダ名</para>
    /// fbxがインポートされたフォルダ直下に作成されます
    /// </summary>
    const string MATERIALS_FOLDER_NAME = "Materials";

    bool isFirstImport = false;
    bool isAutoImport = false;

    void OnPreprocessModel()
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        isFirstImport = modelImporter.importSettingsMissing;

        // 初回インポートのみ
        if (!isFirstImport)
        {
            return;
        }

        string folderPath = Path.GetDirectoryName(assetPath);
        string autoImportFilePath = Path.Combine(folderPath, IMPORT_FILE_NAME);
        isAutoImport = File.Exists(autoImportFilePath);

        // 自動インポートが有効化されていない場合は処理を行わない
        if (!isAutoImport)
        {
            return;
        }

        Debug.Log("fbxインポート時自動化設定：有効");
        Debug.Log($"インポート対象のfbx：{assetPath}");
        Debug.Log($"インポート先のディレクトリ{folderPath}");

        InitialFbxImportSetting();
        InitialMaterialImportSetting();
    }

    void OnPostprocessModel(GameObject gameObject)
    {
        // 初回インポートのみ
        if (!isFirstImport)
        {
            return;
        }

        // 自動インポートが有効化されていない場合は処理を行わない
        if (!isAutoImport)
        {
            return;
        }

        GetHumanDescription(gameObject);
    }

    void InitialFbxImportSetting()
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;

        modelImporter.importBlendShapeNormals = ModelImporterNormals.None;
        modelImporter.animationType = ModelImporterAnimationType.Human;
    }

    void InitialMaterialImportSetting()
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        string folderPath = Path.GetDirectoryName(assetPath);
        string materialsFolderPath = Path.Combine(folderPath, MATERIALS_FOLDER_NAME);

        if (IS_CREATE_MATERIALS && !Directory.Exists(materialsFolderPath))
        {
            Debug.Log("マテリアルフォルダ作成");
            AssetDatabase.CreateFolder(folderPath, MATERIALS_FOLDER_NAME);
        }

        modelImporter.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
    }

    void GetHumanDescription(GameObject gameObject)
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;

        HumanDescription humanDescription = modelImporter.humanDescription;
        Debug.Log("Mecanimのマッピング");

        foreach (HumanBone humanBone in humanDescription.human)
        {
            bool isNotMatch = humanBone.humanName.Equals(humanBone.boneName);
            // Mecanimのボーン名とモデルのボーン名が完全一致しない場合consoleで青文字になるように設定
            var decorationColor = isNotMatch ? "<color=black>" : "<color=blue>";

            Debug.Log($"{humanBone.humanName} : {decorationColor}{humanBone.boneName}</color>");
        }
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!IS_CREATE_MATERIALS)
        {
            return;
        }

        foreach (string importedAsset in importedAssets)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(importedAsset) as ModelImporter;

            if (modelImporter == null)
            {
                Debug.Log(importedAsset);
                Debug.Log(modelImporter);
                continue;
            }

            bool importSettingsMissing = modelImporter.importSettingsMissing;
            string folderPath = Path.GetDirectoryName(importedAsset);
            string autoImportFilePath = Path.Combine(folderPath, IMPORT_FILE_NAME);
            bool isAutoImport = File.Exists(autoImportFilePath);

            //// 初回インポートのみ
            //if (!importSettingsMissing)
            //{
            //    Debug.Log("初回インポートのみ");
            //    return;
            //}

            // 自動インポートが有効化されていない場合は処理を行わない
            if (!isAutoImport)
            {
                return;
            }

            string materialsFolderPath = Path.Combine(folderPath, MATERIALS_FOLDER_NAME);
            ExtractMaterials(importedAsset, materialsFolderPath);
        }
    }

    static void ExtractMaterials(string assetPath, string folderPath)
    {
        IReadOnlyList<Material> materials = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Material>().ToList().AsReadOnly();
        //var materials = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x.GetType() == typeof(Material)).ToArray();

        HashSet<string> assetsToReload = new HashSet<string>();

        foreach (Material material in materials)
        {
            string newAssetPath = Path.Combine(folderPath, material.name) + ".mat";
            Debug.Log("マテリアル名：" + newAssetPath);

            if (File.Exists(newAssetPath))
            {
                continue;
            }

            string error = AssetDatabase.ExtractAsset(material, newAssetPath);
            if (string.IsNullOrEmpty(error))
            {
                assetsToReload.Add(assetPath);
            }
        }

        foreach (string path in assetsToReload)
        {
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        //AssetDatabase.Refresh();
    }
}
