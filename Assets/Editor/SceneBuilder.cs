using System.IO;
using StreetFighter.Core;
using StreetFighter.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StreetFighter.Editor
{
    /// <summary>用于生成 Main.unity 场景（也可用菜单 StreetFighter → Build Scene 手动执行）。</summary>
    public static class SceneBuilder
    {
        private const string MenuPath = "StreetFighter/Build Scene";
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ArtSearchFolder = "Assets/Resources/Art";
        private const string GameManagerObjectName = "GameManager";

        [MenuItem(MenuPath)]
        public static void Build()
        {
            ReimportArt();

            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            RemoveLights();
            SetupCamera();
            CreateGameManager();

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            Debug.Log($"[SF] 场景已生成: {ScenePath}");
        }

        /// <summary>2D 项目不需要方向光。</summary>
        private static void RemoveLights()
        {
            foreach (var light in Object.FindObjectsOfType<Light>())
            {
                Object.DestroyImmediate(light.gameObject);
            }
        }

        private static void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = GameConfig.MapHeight / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0xA0, 0xAA, 0xB2, 0xFF);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateGameManager()
        {
            var gameObject = new GameObject(GameManagerObjectName);
            gameObject.AddComponent<GameManager>();
        }

        private static void ReimportArt()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtSearchFolder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                TextureSettings.Apply(importer);
                importer.SaveAndReimport();
            }

            Debug.Log($"[SF] 纹理导入设置已应用: {guids.Length}");
        }
    }
}
