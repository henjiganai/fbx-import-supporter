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

        Debug.Log("Mecanimのマッピングここまで");
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!IS_CREATE_MATERIALS)
        {
            return;
        }

        var modelAssets = importedAssets.Where(asset =>
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(asset) as ModelImporter;
            bool isModel = modelImporter != null ? true : false;

            string folderPath = Path.GetDirectoryName(asset);
            string autoImportFilePath = Path.Combine(folderPath, IMPORT_FILE_NAME);
            bool isAutoImport = File.Exists(autoImportFilePath);

            return isModel && isAutoImport;
        }).Select((path) =>
        {
            string folderPath = Path.GetDirectoryName(path);
            return new
            {
                path,
                folderPath,
            };
        });

        foreach (var model in modelAssets)
        {
            string materialsFolderPath = Path.Combine(model.folderPath, MATERIALS_FOLDER_NAME);
            ExtractMaterials(model.path, materialsFolderPath);
        }

        Debug.Log("fbxインポート処理完了");
    }

    static void ExtractMaterials(string modelAssetPath, string folderPath)
    {
        var materials = AssetDatabase.LoadAllAssetsAtPath(modelAssetPath).OfType<Material>().ToList().Where(material =>
        {
            string newAssetPath = Path.Combine(folderPath, material.name) + ".mat";
            bool isNotMaterialExist = !File.Exists(newAssetPath);
            return isNotMaterialExist;
        }).Select(material =>
        {
            string assetPath = Path.Combine(folderPath, material.name) + ".mat";
            return new
            {
                material,
                assetPath,
            };
        });

        HashSet<string> assetsToReload = new HashSet<string>();

        foreach (var material in materials)
        {
            string error = AssetDatabase.ExtractAsset(material.material, material.assetPath);
            if (string.IsNullOrEmpty(error))
            {
                Debug.Log($"マテリアル生成：{material.assetPath}");
                assetsToReload.Add(modelAssetPath);
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
