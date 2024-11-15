#if !UNITY_EDITOR
#define RUN_IN_BUILD
#endif

using HybridCLR;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Conditional = System.Diagnostics.ConditionalAttribute;

internal class Lancher : MonoBehaviour
{
    private AsyncOperationHandle m_downloadAsyncOperationHandle = default;

    private async void Start()
    {
        await InitializeAndDownload();

        LoadMetadataForAOTAssemblies();
        LoadHotAssembliesAndInitialize();

        await Resources.UnloadUnusedAssets();
        GC.Collect();

        Debug.Log("2秒后进入SampleScene");
        StartCoroutine(ChangeSceneWaitForSecond(2f));
    }

    private void OnGUI()
    {
        var rect = new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 100f, 30f);
        GUI.Label(rect, m_downloadAsyncOperationHandle.IsValid() ? m_downloadAsyncOperationHandle.PercentComplete.ToString() : "更新完成");
    }

    private async Task InitializeAndDownload()
    {
        await Addressables.InitializeAsync(true).Task;

        var catalogs = await Addressables.CheckForCatalogUpdates(true).Task;
        Debug.Log($"检查Catalog更新 {catalogs != null} {catalogs?.Count() ?? 0}");

        //Addressable目前有Bug，CheckForCatalogUpdates返回的catalogs一直都是0个，只能先去掉OnlyUpdateCatalogsManually设置，在InitializeAsync的时候自动更新catalogs
        if (catalogs != null && catalogs.Count > 0)
        {
            Debug.Log("Catlog开始下载");
            await Addressables.UpdateCatalogs(catalogs, autoReleaseHandle: true).Task;
            Debug.Log("Catlog下载完成");
        }
        else
        {
            Debug.Log("Catlog不需要更新");
        }

        var size = await Addressables.GetDownloadSizeAsync(Addressables.ResourceLocators.SelectMany(x => x.AllLocations)).Task;
        if (size > 0L)
        {
            Debug.Log($"下载资源 大小 {size}");
            m_downloadAsyncOperationHandle = Addressables.DownloadDependenciesAsync(Addressables.ResourceLocators.SelectMany(x => x.AllLocations).ToList(), true);
            await m_downloadAsyncOperationHandle.Task;
            m_downloadAsyncOperationHandle = default;
            Debug.Log("下载完成");
        }
        else
        {
            Debug.Log("没有需要更新的");
        }
    }

    [Conditional("RUN_IN_BUILD")]
    private void LoadMetadataForAOTAssemblies()
    {
        var handle = Addressables.LoadAssetsAsync<TextAsset>("aotassembly");
        handle.WaitForCompletion();

        foreach(var textAsset in handle.Result)
        {
            var err = RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HomologousImageMode.SuperSet);
            Debug.Log($"LoadMetadataForAOTAssembly:{textAsset.name}. ret:{err}");
        }

        Addressables.Release(handle);
    }

    [Conditional("RUN_IN_BUILD")]
    private void LoadHotAssembliesAndInitialize()
    {
        //加载的Assembly需要按照引用顺序排序
        var assemblies = new string[]
        {
            "Gameplay.dll.bytes"
        };

        Assembly assembly = null;
        for (var index = 0; index < assemblies.Length; index++)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(assemblies[index]);
            handle.WaitForCompletion();

            assembly = Assembly.Load(handle.Result.bytes);
            Addressables.Release(handle);
        }

        //Initialization.OnInitialize标记了RuntimeInitializeOnLoadMethod，在Editor下会自动调用不用管，打包后走热更流程因为时机问题要通过反射调用
        var type = assembly.GetType("Gameplay.Initialization");
        type.GetMethod("OnInitialize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }

    private IEnumerator ChangeSceneWaitForSecond(float second)
    {
        yield return new WaitForSeconds(second);
        yield return Addressables.LoadSceneAsync("Assets/Gameplay/Scenes/SampleScene.unity", LoadSceneMode.Single, true);
    }
}
