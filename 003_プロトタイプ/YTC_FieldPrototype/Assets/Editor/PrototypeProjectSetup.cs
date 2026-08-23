using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace YTC.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeProjectSetup
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/Prototype.unity";
        private const string SettingsDirectory = "Assets/Settings";
        private const string RendererPath = SettingsDirectory + "/YTC_PrototypeRenderer.asset";
        private const string PipelinePath = SettingsDirectory + "/YTC_PrototypeURP.asset";
        private const string OfficialAssetDirectory =
            "002_スライド/assets/3d_movement_prototype_v1";
        private const string YamadaResourceDirectory =
            "Assets/Resources/Characters/Yamada";
        private const string FieldResourceDirectory =
            "Assets/Resources/Environment/DemoField";

        static PrototypeProjectSetup()
        {
            EditorApplication.delayCall += ConfigureProject;
        }

        [MenuItem("YTC Prototype/Configure Project")]
        public static void ConfigureProject()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(SettingsDirectory);
            SyncOfficialDesignAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureUniversalRenderPipeline();
            EnsurePrototypeScene();
            ConfigureBuildSettings();

            PlayerSettings.companyName = "YTC";
            PlayerSettings.productName = "Yamada Field Prototype";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SyncOfficialDesignAssets()
        {
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            string repositoryDirectory = Path.GetFullPath(
                Path.Combine(projectDirectory, "..", ".."));
            string sourceDirectory = Path.Combine(
                repositoryDirectory,
                OfficialAssetDirectory.Replace('/', Path.DirectorySeparatorChar));

            string yamadaSource = Path.Combine(sourceDirectory, "yamada_k1_prototype_v1.obj");
            string fieldSource = Path.Combine(
                sourceDirectory,
                "central_belt_stage01_demo_field_v1.obj");
            string materialSource = Path.Combine(sourceDirectory, "prototype_materials_v1.mtl");

            if (!File.Exists(yamadaSource) ||
                !File.Exists(fieldSource) ||
                !File.Exists(materialSource))
            {
                Debug.LogWarning(
                    $"Official YTC design assets were not found at {sourceDirectory}. " +
                    "The prototype will use its fallback visuals.");
                return;
            }

            EnsureDirectory(YamadaResourceDirectory);
            EnsureDirectory(FieldResourceDirectory);

            CopyAsset(yamadaSource, YamadaResourceDirectory + "/Yamada.obj");
            CopyAsset(materialSource, YamadaResourceDirectory + "/prototype_materials_v1.mtl");
            CopyAsset(fieldSource, FieldResourceDirectory + "/DemoField.obj");
            CopyAsset(materialSource, FieldResourceDirectory + "/prototype_materials_v1.mtl");
        }

        private static void CopyAsset(string sourcePath, string destinationAssetPath)
        {
            string destinationPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                destinationAssetPath.Replace('/', Path.DirectorySeparatorChar));
            File.Copy(sourcePath, destinationPath, true);
        }

        private static void ConfigureUniversalRenderPipeline()
        {
            UniversalRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            UniversalRenderPipelineAsset pipelineAsset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            int originalQualityLevel = QualitySettings.GetQualityLevel();
            for (int index = 0; index < QualitySettings.names.Length; index++)
            {
                QualitySettings.SetQualityLevel(index, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            QualitySettings.SetQualityLevel(originalQualityLevel, false);
        }

        private static void EnsurePrototypeScene()
        {
            if (File.Exists(ScenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static void EnsureDirectory(string assetPath)
        {
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
            }
        }
    }
}
