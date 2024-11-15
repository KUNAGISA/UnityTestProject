using HybridCLR.Editor.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace HybridCLRGenerate.Editor
{
    internal static class CopyHotUpdateDlls
    {
        private const string CopyTargetPath = "Assets/HybridCLRGenerate/HotUpdateDlls";
        private const string HotfixDLLGroupName = "Hotfix Scripts Group";

        [MenuItem("HybridCLR/Copy HotUpdateDlls", priority = 2000)]
        public static void CopyBuildTargetDlls()
        {
            var aotAssemblyRootPath = Path.Combine(".\\HybridCLRData\\AssembliesPostIl2CppStrip", EditorUserBuildSettings.activeBuildTarget.ToString());
            var hotUpdateDllsRootPath = Path.Combine(".\\HybridCLRData\\HotUpdateDlls", EditorUserBuildSettings.activeBuildTarget.ToString());
            if (!Directory.Exists(aotAssemblyRootPath) || !Directory.Exists(hotUpdateDllsRootPath)) 
            {
                Debug.LogError($"{EditorUserBuildSettings.activeBuildTarget} 还没有生成DLL");
                return;
            }

            var aa = AddressableAssetSettingsDefaultObject.Settings;
            var hotfixDllGroup = aa.FindGroup(HotfixDLLGroupName);
            if (hotfixDllGroup == null)
            {
                hotfixDllGroup = aa.CreateGroup(HotfixDLLGroupName, false, true, true, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
                var schema = hotfixDllGroup.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(aa, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(aa, AddressableAssetSettings.kRemoteLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                schema.IncludeGUIDInCatalog = false;
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            }
            else
            {
                hotfixDllGroup.RemoveAssetEntries(hotfixDllGroup.entries.ToArray(), true);
            }

            if (!Directory.Exists(CopyTargetPath))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(CopyTargetPath), Path.GetFileName(CopyTargetPath));
                AssetDatabase.Refresh();
            }
            else
            {
                var outFailedPaths = new List<string>();
                var deleteFilePaths = AssetDatabase.FindAssets("", new string[] { CopyTargetPath }).Select(x => AssetDatabase.GUIDToAssetPath(x)).ToArray();
                if (!AssetDatabase.DeleteAssets(deleteFilePaths, outFailedPaths))
                {
                    Debug.LogError($"删除目录[{CopyTargetPath}]下的所有文件失败\n {string.Join('\n', outFailedPaths)}");
                    return;
                }
            }

            foreach(var patchedAOTAssembly in AOTGenericReferences.PatchedAOTAssemblyList)
            {
                var sourcePath = Path.Combine(aotAssemblyRootPath, patchedAOTAssembly);
                if (!File.Exists(sourcePath))
                {
                    Debug.LogWarning($"找不到文件[{sourcePath}]");
                    continue;
                }

                var destinationPath = Path.Combine(CopyTargetPath, patchedAOTAssembly + ".bytes");
                File.Copy(sourcePath, destinationPath, true);
                AssetDatabase.Refresh();

                var guid = AssetDatabase.AssetPathToGUID(destinationPath);
                var entry = aa.CreateOrMoveEntry(guid, hotfixDllGroup, true, true);
                entry.SetAddress(patchedAOTAssembly + ".bytes");
                entry.SetLabel("aotassembly", true, true);
            }

            foreach(var hotUpdateAssembly in HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions)
            {
                var sourcePath = Path.Combine(hotUpdateDllsRootPath, hotUpdateAssembly.name + ".dll");
                if (!File.Exists(sourcePath))
                {
                    Debug.LogWarning($"找不到文件[{sourcePath}]");
                    continue;
                }

                var destinationPath = Path.Combine(CopyTargetPath, hotUpdateAssembly.name + ".dll.bytes");
                File.Copy(sourcePath, destinationPath, true);
                AssetDatabase.Refresh();

                var guid = AssetDatabase.AssetPathToGUID(destinationPath);
                var entry = aa.CreateOrMoveEntry(guid, hotfixDllGroup, true, true);
                entry.SetAddress(hotUpdateAssembly.name + ".dll.bytes");
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Copy HotUpdateDlls", "Copy HotUpdateDlls Complete.", "OK");
        }
    }
}
