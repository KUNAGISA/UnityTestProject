using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    internal static class Initialization
    {
        [RuntimeInitializeOnLoadMethod]
        private static void OnInitialize()
        {
            Application.quitting -= OnShutdown;
            Application.quitting += OnShutdown;

            GameArchitecture.MakeSureArchitecture();

            Debug.Log("游戏初始化成功");
        }

        private static void OnShutdown()
        {
            Application.quitting -= OnShutdown;

            DestroyAllGameObjects();

            GameArchitecture.DestroyInstance();
        }

        private static void DestroyAllGameObjects()
        {
            var rootGameObjects = new List<GameObject>();
            for (var index = 0; index < SceneManager.sceneCount; ++index)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid())
                {
                    scene.GetRootGameObjects(rootGameObjects);
                    foreach (var gameObject in rootGameObjects)
                    {
                        Object.DestroyImmediate(gameObject);
                    }
                }
            }
        }
    }
}