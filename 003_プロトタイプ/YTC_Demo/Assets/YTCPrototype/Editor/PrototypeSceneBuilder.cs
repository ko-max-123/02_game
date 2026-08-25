using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace YTCPrototype.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string MenuPath = "YTC Prototype/Setup or Refresh Combat Demo";
        private const string ScenePath = "Assets/YTCPrototype/Scenes/YTC_Demo.unity";
        private const string ImportedRoot = "Assets/YTCPrototype/ImportedDesignAssets";
        private const string GeneratedRoot = "Assets/YTCPrototype/Generated";
        private const string PlayerAssetPath = ImportedRoot + "/Models/yamada_k1_demo.obj";
        private const string FieldVisualAssetPath = ImportedRoot + "/Models/central_industrial_belt_demo.obj";
        private const string FieldCollisionAssetPath = ImportedRoot + "/Models/central_industrial_belt_collision.obj";

        [MenuItem(MenuPath)]
        public static void BuildFromMenu()
        {
            BuildOrRefreshScene();
            EditorUtility.DisplayDialog(
                "YTC Standalone Combat Prototype",
                "Combat demo scene is ready. Open Assets/YTCPrototype/Scenes/YTC_Demo.unity and press Play.",
                "OK");
        }

        public static void BuildFromCommandLine()
        {
            BuildOrRefreshScene();
            Debug.Log("YTC prototype command-line setup completed.");
        }

        public static void BuildOrRefreshScene()
        {
            EnsureAssetFolder("Assets/YTCPrototype/Scenes");
            EnsureAssetFolder(GeneratedRoot);
            EnsureUrpPipeline();
            SyncExternalDesignAssets();

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = EnsureRoot("YTC_PrototypeRoot");
            EnsureLighting(root.transform);

            string fieldLabel = EnsureField(root.transform);
            PrototypePlayerController player = EnsurePlayer(root.transform, out string playerLabel);
            Camera camera = EnsureCamera(root.transform, player.transform);
            EnsureCombat(
                root.transform,
                player,
                camera,
                out PrototypePlayerHealth playerHealth,
                out PrototypePlayerCombat playerCombat,
                out PrototypeCombatDirector combatDirector);
            EnsureGuide(
                root.transform,
                player,
                playerHealth,
                playerCombat,
                combatDirector,
                playerLabel,
                fieldLabel);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save prototype scene: " + ScenePath);
            }

            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = player.gameObject;
            Debug.Log($"YTC prototype ready. Player={playerLabel}, Field={fieldLabel}, Scene={ScenePath}");
        }

        private static void SyncExternalDesignAssets()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root could not be resolved.");
            string prototypeRoot = Directory.GetParent(projectRoot)?.FullName
                ?? throw new InvalidOperationException("Prototype root could not be resolved.");
            string externalRoot = Path.Combine(prototypeRoot, "DesignAssets");

            if (!Directory.Exists(externalRoot))
            {
                Debug.LogWarning($"DesignAssets not found at {externalRoot}. Primitive fallbacks will be used.");
                return;
            }

            string importedAbsolute = Path.Combine(Application.dataPath, "YTCPrototype", "ImportedDesignAssets");
            Directory.CreateDirectory(importedAbsolute);

            foreach (string sourcePath in Directory.EnumerateFiles(externalRoot, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(externalRoot, sourcePath);
                if (ShouldSkipDesignAsset(relative))
                {
                    continue;
                }

                string destination = Path.Combine(importedAbsolute, relative);
                string destinationDirectory = Path.GetDirectoryName(destination)
                    ?? throw new InvalidOperationException("Imported asset directory could not be resolved.");
                Directory.CreateDirectory(destinationDirectory);

                FileInfo sourceInfo = new FileInfo(sourcePath);
                FileInfo destinationInfo = new FileInfo(destination);
                bool needsCopy = !destinationInfo.Exists
                    || destinationInfo.Length != sourceInfo.Length
                    || destinationInfo.LastWriteTimeUtc < sourceInfo.LastWriteTimeUtc;

                if (needsCopy)
                {
                    File.Copy(sourcePath, destination, true);
                    File.SetLastWriteTimeUtc(destination, sourceInfo.LastWriteTimeUtc);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureImportedMaterialsUseUrp();
        }

        private static void EnsureImportedMaterialsUseUrp()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogWarning("URP Lit shader is unavailable; imported OBJ materials were left unchanged.");
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { ImportedRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == urpLit)
                {
                    continue;
                }

                Color color = material.HasProperty("_Color") ? material.color : Color.white;
                material.shader = urpLit;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                EditorUtility.SetDirty(material);
            }
        }

        private static bool ShouldSkipDesignAsset(string relativePath)
        {
            string normalized = relativePath.Replace('\\', '/');
            return normalized.StartsWith("Source/", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("Previews/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureField(Transform parent)
        {
            GameObject wrapper = EnsureChild(parent, "DemoField");
            GameObject fallback = EnsureFallbackField(wrapper.transform);

            GameObject visualAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FieldVisualAssetPath);
            GameObject collisionAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FieldCollisionAssetPath);

            if (visualAsset == null || collisionAsset == null)
            {
                fallback.SetActive(true);
                return "Primitive fallback";
            }

            GameObject visual = EnsureAssetInstance(wrapper.transform, visualAsset, "DesignFieldVisual");
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            GameObject collision = EnsureAssetInstance(wrapper.transform, collisionAsset, "DesignFieldCollision");
            collision.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            collision.transform.localScale = Vector3.one;
            ConfigureCollisionGeometry(collision);

            fallback.SetActive(false);
            return "DesignAssets/central_industrial_belt_demo.obj";
        }

        private static PrototypePlayerController EnsurePlayer(Transform parent, out string playerLabel)
        {
            GameObject player = EnsureChild(parent, "Yamada_K1_Player");
            player.transform.SetPositionAndRotation(new Vector3(-14f, 0.05f, 0f), Quaternion.identity);

            CharacterController character = GetOrAdd<CharacterController>(player);
            character.height = 2.1f;
            character.radius = 0.42f;
            character.center = new Vector3(0f, 1.05f, 0f);
            character.stepOffset = 0.3f;
            character.slopeLimit = 50f;
            character.skinWidth = 0.05f;

            PrototypePlayerController controller = GetOrAdd<PrototypePlayerController>(player);
            GameObject visualRoot = EnsureChild(player.transform, "PlayerVisualRoot");
            controller.ConfigureVisualRoot(visualRoot.transform);
            visualRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject fallback = EnsureFallbackPlayer(visualRoot.transform);
            GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetPath);

            if (playerAsset == null)
            {
                fallback.SetActive(true);
                playerLabel = "Primitive fallback";
                return controller;
            }

            GameObject designVisual = EnsureAssetInstance(visualRoot.transform, playerAsset, "YamadaK1DesignVisual");
            designVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            designVisual.transform.localScale = Vector3.one;
            DisablePlayerVisualPhysics(designVisual);
            fallback.SetActive(false);

            playerLabel = "DesignAssets/yamada_k1_demo.obj";
            return controller;
        }

        private static Camera EnsureCamera(Transform parent, Transform target)
        {
            GameObject cameraObject = EnsureChild(parent, "Main Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = GetOrAdd<Camera>(cameraObject);
            camera.orthographic = true;
            camera.orthographicSize = 6.2f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.09f, 1f);

            FixedDepthCamera follow = GetOrAdd<FixedDepthCamera>(cameraObject);
            follow.Configure(target, -16f);
            return camera;
        }

        private static void EnsureCombat(
            Transform parent,
            PrototypePlayerController player,
            Camera camera,
            out PrototypePlayerHealth playerHealth,
            out PrototypePlayerCombat playerCombat,
            out PrototypeCombatDirector combatDirector)
        {
            playerHealth = GetOrAdd<PrototypePlayerHealth>(player.gameObject);
            playerHealth.Configure(player);

            GameObject muzzleObject = EnsureChild(player.transform, "WeaponMuzzle");
            muzzleObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            muzzleObject.transform.localRotation = Quaternion.identity;

            playerCombat = GetOrAdd<PrototypePlayerCombat>(player.gameObject);
            playerCombat.Configure(player, playerHealth, camera, muzzleObject.transform);
            playerHealth.ConfigureCombat(playerCombat);

            GameObject directorObject = EnsureChild(parent, "PrototypeCombatDirector");
            combatDirector = GetOrAdd<PrototypeCombatDirector>(directorObject);
            combatDirector.Configure(player, playerHealth, playerCombat);

            GameObject enemiesRoot = EnsureChild(parent, "Enemies");
            EnsureEnemy(
                enemiesRoot.transform,
                "Enemy_Scout_A",
                new Vector3(-4f, 0.05f, -1.4f),
                combatDirector,
                playerHealth,
                0.1f,
                1.55f);
            EnsureEnemy(
                enemiesRoot.transform,
                "Enemy_Scout_B",
                new Vector3(5f, 0.05f, 0f),
                combatDirector,
                playerHealth,
                2.2f,
                1.8f);
            EnsureEnemy(
                enemiesRoot.transform,
                "Enemy_Guard_C",
                new Vector3(13f, 3.5f, 1.4f),
                combatDirector,
                playerHealth,
                4.1f,
                2.05f);
        }

        private static void EnsureEnemy(
            Transform parent,
            string name,
            Vector3 position,
            PrototypeCombatDirector director,
            PrototypePlayerHealth target,
            float patrolPhase,
            float attackInterval)
        {
            GameObject enemyObject = EnsureChild(parent, name);
            enemyObject.SetActive(true);
            enemyObject.transform.SetPositionAndRotation(position, Quaternion.identity);

            CapsuleCollider collider = GetOrAdd<CapsuleCollider>(enemyObject);
            collider.height = 1.9f;
            collider.radius = 0.44f;
            collider.center = new Vector3(0f, 0.95f, 0f);

            Material bodyMaterial = GetOrCreateMaterial(
                GeneratedRoot + "/EnemyBody.mat",
                new Color(0.24f, 0.28f, 0.3f));
            Material sensorMaterial = GetOrCreateMaterial(
                GeneratedRoot + "/EnemySensor.mat",
                new Color(0.9f, 0.1f, 0.1f));

            GameObject body = EnsurePrimitive(enemyObject.transform, "Body", PrimitiveType.Cube);
            body.transform.localPosition = new Vector3(0f, 0.76f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.92f, 1.18f, 0.72f);
            AssignMaterial(body, bodyMaterial);
            DisableCollider(body);

            GameObject head = EnsurePrimitive(enemyObject.transform, "LowHead", PrimitiveType.Cube);
            head.transform.localPosition = new Vector3(0f, 1.48f, 0f);
            head.transform.localRotation = Quaternion.identity;
            head.transform.localScale = new Vector3(0.66f, 0.3f, 0.66f);
            AssignMaterial(head, bodyMaterial);
            DisableCollider(head);

            GameObject sensor = EnsureChild(enemyObject.transform, "EnemySensorTriangle");
            sensor.transform.localPosition = new Vector3(0f, 1.48f, -0.345f);
            sensor.transform.localRotation = Quaternion.identity;
            sensor.transform.localScale = Vector3.one;
            MeshFilter sensorFilter = GetOrAdd<MeshFilter>(sensor);
            sensorFilter.sharedMesh = GetOrCreateEnemySensorMesh();
            MeshRenderer sensorRenderer = GetOrAdd<MeshRenderer>(sensor);
            sensorRenderer.sharedMaterial = sensorMaterial;

            PrototypeEnemy enemy = GetOrAdd<PrototypeEnemy>(enemyObject);
            enemy.Configure(director, target, patrolPhase, attackInterval);
        }

        private static Mesh GetOrCreateEnemySensorMesh()
        {
            const string meshPath = GeneratedRoot + "/EnemySensorTriangle.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                return existing;
            }

            Mesh mesh = new Mesh { name = "EnemySensorTriangle" };
            mesh.vertices = new[]
            {
                new Vector3(-0.16f, 0.12f, 0f),
                new Vector3(0.16f, 0.12f, 0f),
                new Vector3(0f, -0.16f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshPath);
            return mesh;
        }

        private static void EnsureGuide(
            Transform parent,
            PrototypePlayerController player,
            PrototypePlayerHealth playerHealth,
            PrototypePlayerCombat playerCombat,
            PrototypeCombatDirector combatDirector,
            string playerLabel,
            string fieldLabel)
        {
            GameObject guideObject = EnsureChild(parent, "PrototypeGuideOverlay");
            PrototypeGuideOverlay guide = GetOrAdd<PrototypeGuideOverlay>(guideObject);
            guide.Configure(
                player,
                playerHealth,
                playerCombat,
                combatDirector,
                playerLabel,
                fieldLabel);
        }

        private static void EnsureLighting(Transform parent)
        {
            GameObject lightObject = EnsureChild(parent, "Prototype Sun");
            Light light = GetOrAdd<Light>(lightObject);
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.9f, 0.78f);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.38f, 0.45f);
        }

        private static GameObject EnsureFallbackField(Transform parent)
        {
            GameObject fallback = EnsureChild(parent, "PrimitiveFieldFallback");
            Material groundMaterial = GetOrCreateMaterial(
                GeneratedRoot + "/FallbackGround.mat",
                new Color(0.26f, 0.3f, 0.32f));
            Material laneMaterial = GetOrCreateMaterial(
                GeneratedRoot + "/FallbackLane.mat",
                new Color(0.9f, 0.55f, 0.08f));

            EnsureCube(fallback.transform, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(36f, 1f, 6f), groundMaterial);
            EnsureCube(fallback.transform, "JumpPlatform", new Vector3(6f, 1f, 0f), new Vector3(4f, 0.5f, 6f), groundMaterial);
            EnsureCube(fallback.transform, "FlightPlatform", new Vector3(13f, 3.2f, 0f), new Vector3(5f, 0.5f, 6f), groundMaterial);
            EnsureCube(fallback.transform, "LaneFront", new Vector3(0f, 0.02f, -2.5f), new Vector3(36f, 0.04f, 0.08f), laneMaterial);
            EnsureCube(fallback.transform, "LaneBack", new Vector3(0f, 0.02f, 2.5f), new Vector3(36f, 0.04f, 0.08f), laneMaterial);
            return fallback;
        }

        private static GameObject EnsureFallbackPlayer(Transform parent)
        {
            GameObject fallback = EnsureChild(parent, "PrimitiveK1Fallback");
            Material bodyMaterial = GetOrCreateMaterial(
                GeneratedRoot + "/FallbackK1Body.mat",
                new Color(0.72f, 0.74f, 0.76f));
            Material accentMaterial = GetOrCreateMaterial(
                GeneratedRoot + "/FallbackK1Accent.mat",
                new Color(0.92f, 0.32f, 0.05f));

            GameObject body = EnsurePrimitive(fallback.transform, "Body", PrimitiveType.Capsule);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.72f, 1f, 0.52f);
            AssignMaterial(body, bodyMaterial);
            DisableCollider(body);

            GameObject shoulder = EnsurePrimitive(fallback.transform, "Shoulder", PrimitiveType.Cube);
            shoulder.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            shoulder.transform.localScale = new Vector3(1.05f, 0.24f, 0.5f);
            AssignMaterial(shoulder, accentMaterial);
            DisableCollider(shoulder);
            return fallback;
        }

        private static void ConfigureCollisionGeometry(GameObject collisionRoot)
        {
            foreach (Renderer renderer in collisionRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (MeshFilter meshFilter in collisionRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                }

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
            }
        }

        private static void DisablePlayerVisualPhysics(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
            }
        }

        private static GameObject EnsureAssetInstance(Transform parent, GameObject asset, string instanceName)
        {
            Transform existing = parent.Find(instanceName);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing.gameObject;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.name = instanceName;
            return instance;
        }

        private static GameObject EnsureRoot(string name)
        {
            return GameObject.Find(name) ?? new GameObject(name);
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static GameObject EnsurePrimitive(Transform parent, string name, PrimitiveType type)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            return primitive;
        }

        private static void EnsureCube(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject cube = EnsurePrimitive(parent, name, PrimitiveType.Cube);
            cube.transform.localPosition = position;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = scale;
            AssignMaterial(cube, material);
        }

        private static void DisableCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private static void AssignMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible Lit shader was found.");
            }

            Material material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void EnsureUrpPipeline()
        {
            const string rendererPath = GeneratedRoot + "/YTC_Prototype_Renderer.asset";
            const string pipelinePath = GeneratedRoot + "/YTC_Prototype_URP.asset";

            RenderPipelineAsset existing = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);
            if (existing != null)
            {
                GraphicsSettings.defaultRenderPipeline = existing;
                QualitySettings.renderPipeline = existing;
                return;
            }

            Type rendererType = FindType(
                "UnityEngine.Rendering.Universal.UniversalRendererData",
                "Unity.RenderPipelines.Universal.Runtime");
            Type pipelineType = FindType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset",
                "Unity.RenderPipelines.Universal.Runtime");

            if (rendererType == null || pipelineType == null)
            {
                Debug.LogWarning("URP package is not ready. Re-run the setup menu after package import.");
                return;
            }

            ScriptableObject rendererData = ScriptableObject.CreateInstance(rendererType);
            AssetDatabase.CreateAsset(rendererData, rendererPath);

            RenderPipelineAsset pipeline = CreatePipelineAsset(pipelineType, rendererType, rendererData);
            AssetDatabase.CreateAsset(pipeline, pipelinePath);

            SerializedObject serializedPipeline = new SerializedObject(pipeline);
            SerializedProperty rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }

            SerializedProperty defaultRenderer = serializedPipeline.FindProperty("m_DefaultRendererIndex");
            if (defaultRenderer != null)
            {
                defaultRenderer.intValue = 0;
            }

            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        private static RenderPipelineAsset CreatePipelineAsset(
            Type pipelineType,
            Type rendererType,
            ScriptableObject rendererData)
        {
            MethodInfo factory = pipelineType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    return method.Name == "Create"
                        && parameters.Length == 1
                        && parameters[0].ParameterType.IsAssignableFrom(rendererType)
                        && typeof(RenderPipelineAsset).IsAssignableFrom(method.ReturnType);
                });

            if (factory != null)
            {
                return (RenderPipelineAsset)factory.Invoke(null, new object[] { rendererData });
            }

            return (RenderPipelineAsset)ScriptableObject.CreateInstance(pipelineType);
        }

        private static Type FindType(string fullName, string assemblyName)
        {
            return Type.GetType($"{fullName}, {assemblyName}")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType(fullName))
                    .FirstOrDefault(type => type != null);
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(scene => scene.path == ScenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = scenes
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
                .ToArray();
        }
    }
}
