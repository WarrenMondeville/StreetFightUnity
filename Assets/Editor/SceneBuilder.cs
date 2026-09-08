using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StreetFighter.EditorTools
{
    /// <summary>用于生成 Main.unity 场景（也可用菜单 StreetFighter/Build Scene 手动执行）。</summary>
    public static class SceneBuilder
    {
        [MenuItem("StreetFighter/Build Scene")]
        public static void Build()
        {
            ReimportArt();

            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 2D 项目不需要方向光
            foreach (var light in Object.FindObjectsOfType<Light>())
                Object.DestroyImmediate(light.gameObject);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = Cfg.MapHeight / 2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color32(0xA0, 0xAA, 0xB2, 0xFF);
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[SF] 场景已生成: Assets/Scenes/Main.unity");
        }

        private static void ReimportArt()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Art" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                TextureSettings.Apply(imp);
                imp.SaveAndReimport();
            }
            Debug.Log("[SF] 纹理导入设置已应用: " + guids.Length);
        }
    }
}
