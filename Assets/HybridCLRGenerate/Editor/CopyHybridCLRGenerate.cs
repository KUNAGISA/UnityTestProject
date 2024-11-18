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
    internal static class CopyHybridCLRGenerate
    {
        private const string HotfixDLLGroupName = "Hotfix Scripts Group";

        private const string CopyTargetHotUpdateDllsPath = "Assets/HybridCLRGenerate/HotUpdateDlls";
        private const string PatchedAOTLabelName = "aotassembly";

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

            CreateOrClearDirectory(CopyTargetHotUpdateDllsPath);

            var aa = AddressableAssetSettingsDefaultObject.Settings;
            var hotfixDllGroup = CreateOrClearAssetGroup(aa, HotfixDLLGroupName);

            var patchedAOTAssetPaths = CopyFiles(AOTGenericReferences.PatchedAOTAssemblyList, aotAssemblyRootPath, CopyTargetHotUpdateDllsPath);
            var hotUpdateDllsAssetPaths = CopyFiles(HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions.Select(x => x.name + ".dll"), hotUpdateDllsRootPath, CopyTargetHotUpdateDllsPath);
            AssetDatabase.Refresh();

            AddToAddressable(aa, hotfixDllGroup, patchedAOTAssetPaths, PatchedAOTLabelName);
            AddToAddressable(aa, hotfixDllGroup, hotUpdateDllsAssetPaths);

            EditorUtility.DisplayDialog("Copy HotUpdateDlls", "Copy HotUpdateDlls Complete.", "OK");
        }

        private static bool CreateOrClearDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(directoryPath), Path.GetFileName(directoryPath));
                AssetDatabase.Refresh();
            }
            else
            {
                var outFailedPaths = new List<string>();
                var deleteFilePaths = AssetDatabase.FindAssets("", new string[] { directoryPath }).Select(x => AssetDatabase.GUIDToAssetPath(x)).ToArray();
                if (!AssetDatabase.DeleteAssets(deleteFilePaths, outFailedPaths))
                {
                    Debug.LogError($"删除目录[{directoryPath}]下的所有文件失败\n {string.Join('\n', outFailedPaths)}");
                    return false;
                }
            }
            return true;
        }

        private static AddressableAssetGroup CreateOrClearAssetGroup(AddressableAssetSettings aa, string groupName)
        {
            var hotfixDllGroup = aa.FindGroup(groupName);
            if (hotfixDllGroup == null)
            {
                hotfixDllGroup = aa.CreateGroup(groupName, false, true, true, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
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
            return hotfixDllGroup;
        }

        private static string[] CopyFiles(IEnumerable<string> files, string sourceRootPath, string destinationRootPath, string label = null)
        {
            var copyTargetPaths = new List<string>();
            foreach (var fileName in files)
            {
                var sourcePath = Path.Combine(sourceRootPath, fileName);
                if (!File.Exists(sourcePath))
                {
                    Debug.LogWarning($"找不到文件[{sourcePath}]");
                    continue;
                }

                var destinationPath = Path.Combine(destinationRootPath, fileName + ".bytes");
                File.Copy(sourcePath, destinationPath, true);
                copyTargetPaths.Add(destinationPath);
            }
            return copyTargetPaths.ToArray();
        }

        private static void AddToAddressable(AddressableAssetSettings aa, AddressableAssetGroup targetParent, string[] assetPaths, string label = null)
        {
            for (var index = 0; index < assetPaths.Length; index++)
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPaths[index]);
                var entry = aa.CreateOrMoveEntry(guid, targetParent, true);
                entry.SetAddress(Path.GetFileName(assetPaths[index]));

                if (!string.IsNullOrEmpty(label))
                {
                    entry.SetLabel(label, true, true);
                }
            }
        }
    }
}
