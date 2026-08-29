using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YTCPrototype.Editor
{
    public static class PrototypeV2SceneBuilder
    {
        public const string ScenePath = "Assets/YTCPrototype/Scenes/YTC_Demo_V2.unity";
        public const string ImportedRoot = "Assets/YTCPrototype/ImportedDesignAssetsV2";

        private const string MenuPath = "YTC Prototype V2/Setup or Refresh V2 Combat Demo";
        private const string V1ScenePath = "Assets/YTCPrototype/Scenes/YTC_Demo.unity";
        private const string GeneratedRoot = "Assets/YTCPrototype/GeneratedV2";
        private const string GeneratedClipRoot = GeneratedRoot + "/Clips";
        private const string PlayerAssetPath = ImportedRoot + "/Models/yamada_k1_rigged_v2.glb";
        private const string WeaponAssetPath = ImportedRoot + "/Models/k11_rifle_v2.glb";
        private const string FieldVisualAssetPath = ImportedRoot + "/Models/central_industrial_belt_v2.glb";
        private const string FieldCollisionAssetPath = ImportedRoot + "/Models/central_industrial_belt_collision_v2.obj";
        private const string AnimatorControllerPath = GeneratedRoot + "/K1V2.controller";

        private static readonly string[] RequiredClipNames =
        {
            K1V2AnimatorDriver.IdleState,
            K1V2AnimatorDriver.WalkState,
            K1V2AnimatorDriver.DepthPositiveState,
            K1V2AnimatorDriver.DepthNegativeState,
            K1V2AnimatorDriver.TurnLeftState,
            K1V2AnimatorDriver.TurnRightState,
            K1V2AnimatorDriver.JumpStartState,
            K1V2AnimatorDriver.JumpLoopState,
            K1V2AnimatorDriver.LandState,
            K1V2AnimatorDriver.JetStartState,
            K1V2AnimatorDriver.JetLoopState,
            K1V2AnimatorDriver.JetEndState,
            K1V2AnimatorDriver.ShootState
        };

        private static readonly HashSet<string> LoopingClipNames = new HashSet<string>
        {
            K1V2AnimatorDriver.IdleState,
            K1V2AnimatorDriver.WalkState,
            K1V2AnimatorDriver.DepthPositiveState,
            K1V2AnimatorDriver.DepthNegativeState,
            K1V2AnimatorDriver.JumpLoopState,
            K1V2AnimatorDriver.JetLoopState
        };

        [MenuItem(MenuPath)]
        public static void BuildFromMenu()
        {
            BuildOrRefreshScene();
            EditorUtility.DisplayDialog(
                "YTC V2 Combat Prototype",
                "V2 scene is ready at Assets/YTCPrototype/Scenes/YTC_Demo_V2.unity.",
                "OK");
        }

        public static void BuildFromCommandLine()
        {
            BuildOrRefreshScene();
            Debug.Log("YTC V2 prototype command-line setup completed.");
        }

        public static void BuildOrRefreshScene()
        {
            PrototypeSceneBuilder.BuildOrRefreshScene();
            EnsureAssetFolder(GeneratedRoot);
            EnsureAssetFolder(GeneratedClipRoot);
            SyncExternalDesignAssetsV2();
            ConfigureGltfImporter(PlayerAssetPath, true);
            ConfigureGltfImporter(WeaponAssetPath, false);
            ConfigureGltfImporter(FieldVisualAssetPath, false);

            GameObject playerAsset = RequireAsset<GameObject>(PlayerAssetPath);
            GameObject weaponAsset = RequireAsset<GameObject>(WeaponAssetPath);
            GameObject fieldVisualAsset = RequireAsset<GameObject>(FieldVisualAssetPath);
            GameObject fieldCollisionAsset = RequireAsset<GameObject>(FieldCollisionAssetPath);
            AnimatorController animatorController = EnsureAnimatorController();

            Scene v1Scene = EditorSceneManager.OpenScene(V1ScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(v1Scene, ScenePath, true))
            {
                throw new InvalidOperationException("Failed to preserve V1 scene as V2 copy: " + ScenePath);
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("YTC_PrototypeRoot")
                ?? throw new InvalidOperationException("V1 prototype root is missing.");

            PrototypePlayerController movement = ConfigurePlayer(root.transform, playerAsset, weaponAsset, animatorController);
            string fieldLabel = ConfigureField(root.transform, fieldVisualAsset, fieldCollisionAsset);
            ConfigureEnemies(root.transform);
            ConfigureGuide(root.transform, movement, fieldLabel);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save V2 scene: " + ScenePath);
            }

            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = movement.gameObject;
            Debug.Log("YTC V2 prototype ready. GLB=Mecanim direct import, Scene=" + ScenePath);
        }

        private static void SyncExternalDesignAssetsV2()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root could not be resolved.");
            string prototypeRoot = Directory.GetParent(projectRoot)?.FullName
                ?? throw new InvalidOperationException("Prototype root could not be resolved.");
            string externalRoot = Path.Combine(prototypeRoot, "DesignAssets_V2");
            if (!Directory.Exists(externalRoot))
            {
                throw new DirectoryNotFoundException("DesignAssets_V2 not found: " + externalRoot);
            }

            string importedAbsolute = Path.Combine(Application.dataPath, "YTCPrototype", "ImportedDesignAssetsV2");
            Directory.CreateDirectory(importedAbsolute);

            foreach (string sourcePath in Directory.EnumerateFiles(externalRoot, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(externalRoot, sourcePath);
                string normalized = relative.Replace('\\', '/');
                if (normalized.StartsWith("Source/", StringComparison.OrdinalIgnoreCase)
                    || normalized.StartsWith("Previews/", StringComparison.OrdinalIgnoreCase)
                    || normalized.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string destination = Path.Combine(importedAbsolute, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)
                    ?? throw new InvalidOperationException("V2 import destination is invalid."));

                FileInfo source = new FileInfo(sourcePath);
                FileInfo target = new FileInfo(destination);
                if (!target.Exists || target.Length != source.Length || target.LastWriteTimeUtc < source.LastWriteTimeUtc)
                {
                    File.Copy(sourcePath, destination, true);
                    File.SetLastWriteTimeUtc(destination, source.LastWriteTimeUtc);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureGltfImporter(string assetPath, bool mecanim)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath)
                ?? throw new InvalidOperationException("glTF importer was not created for " + assetPath);
            SerializedObject serializedImporter = new SerializedObject(importer);
            SerializedProperty animationMethod = serializedImporter.FindProperty("importSettings.animationMethod");
            if (animationMethod == null)
            {
                throw new InvalidOperationException("glTFast animation import setting is unavailable for " + assetPath);
            }

            int requestedValue = mecanim ? 2 : 0;
            if (animationMethod.enumValueIndex != requestedValue)
            {
                animationMethod.enumValueIndex = requestedValue;
                serializedImporter.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController EnsureAnimatorController()
        {
            Dictionary<string, AnimationClip> importedClips = AssetDatabase.LoadAllAssetsAtPath(PlayerAssetPath)
                .OfType<AnimationClip>()
                .GroupBy(clip => clip.name)
                .ToDictionary(group => group.Key, group => group.First());

            string[] missing = RequiredClipNames.Where(name => !importedClips.ContainsKey(name)).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException("K1 V2 clips missing after GLB import: " + string.Join(", ", missing));
            }

            Dictionary<string, AnimationClip> clips = importedClips.ToDictionary(
                pair => pair.Key,
                pair => EnsureRuntimeClip(pair.Value));

            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existing != null)
            {
                foreach (AnimatorControllerLayer layer in existing.layers)
                {
                    foreach (ChildAnimatorState child in layer.stateMachine.states)
                    {
                        if (clips.TryGetValue(child.state.name, out AnimationClip clip))
                        {
                            child.state.motion = clip;
                            EditorUtility.SetDirty(child.state);
                        }
                    }
                }
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            controller.AddParameter(K1V2AnimatorDriver.LocomotionRateParameter, AnimatorControllerParameterType.Float);

            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = "K1 Base";
            AnimatorStateMachine baseMachine = baseLayer.stateMachine;
            baseMachine.name = "K1 Base";

            for (int index = 0; index < RequiredClipNames.Length - 1; index++)
            {
                string clipName = RequiredClipNames[index];
                AnimatorState state = baseMachine.AddState(clipName, new Vector3(240f + index % 4 * 220f, 60f + index / 4 * 90f));
                state.motion = clips[clipName];
                if (clipName == K1V2AnimatorDriver.WalkState
                    || clipName == K1V2AnimatorDriver.DepthPositiveState
                    || clipName == K1V2AnimatorDriver.DepthNegativeState)
                {
                    state.speedParameterActive = true;
                    state.speedParameter = K1V2AnimatorDriver.LocomotionRateParameter;
                }

                if (clipName == K1V2AnimatorDriver.IdleState)
                {
                    baseMachine.defaultState = state;
                }
            }

            AnimatorStateMachine shootMachine = new AnimatorStateMachine { name = "K1 Shoot" };
            AssetDatabase.AddObjectToAsset(shootMachine, controller);
            AnimatorState shootState = shootMachine.AddState(K1V2AnimatorDriver.ShootState, new Vector3(300f, 80f));
            shootState.motion = clips[K1V2AnimatorDriver.ShootState];
            shootMachine.defaultState = shootState;

            AnimatorControllerLayer shootLayer = new AnimatorControllerLayer
            {
                name = "K1 Shoot",
                defaultWeight = 0f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = shootMachine
            };
            controller.layers = new[] { baseLayer, shootLayer };
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip EnsureRuntimeClip(AnimationClip source)
        {
            string assetPath = $"{GeneratedClipRoot}/{source.name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            EditorUtility.CopySerialized(source, clip);
            clip.name = source.name;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = LoopingClipNames.Contains(source.name);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static PrototypePlayerController ConfigurePlayer(
            Transform root,
            GameObject playerAsset,
            GameObject weaponAsset,
            AnimatorController animatorController)
        {
            Transform playerTransform = root.Find("Yamada_K1_Player")
                ?? throw new InvalidOperationException("Player is missing from V1 scene.");
            GameObject player = playerTransform.gameObject;
            player.transform.SetPositionAndRotation(new Vector3(-15f, 0.05f, 0f), Quaternion.identity);

            CharacterController capsule = player.GetComponent<CharacterController>()
                ?? throw new InvalidOperationException("Player CharacterController is missing.");
            capsule.center = new Vector3(0f, 0.99f, 0f);
            capsule.height = 1.88f;
            capsule.radius = 0.31f;

            PrototypePlayerController movement = player.GetComponent<PrototypePlayerController>()
                ?? throw new InvalidOperationException("Player movement is missing.");
            movement.ConfigureV2Motion(4.2f, 3.5f, -2.56f, 2.56f, 0.30f);

            Transform visualRoot = playerTransform.Find("PlayerVisualRoot")
                ?? throw new InvalidOperationException("PlayerVisualRoot is missing.");
            foreach (Transform child in visualRoot)
            {
                child.gameObject.SetActive(false);
            }

            GameObject visual = EnsureAssetInstance(visualRoot, playerAsset, "YamadaK1RiggedV2");
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;
            DisablePhysics(visual);
            movement.ConfigureVisualRoot(visualRoot);

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Transform weaponSocket = FindDescendant(visual.transform, "WeaponSocket_R")
                ?? throw new InvalidOperationException("K1 WeaponSocket_R is missing after import.");
            GameObject weapon = EnsureAssetInstance(weaponSocket, weaponAsset, "K11_Rifle_V2");
            weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            weapon.transform.localScale = Vector3.one;
            DisablePhysics(weapon);
            Transform muzzle = FindDescendant(weapon.transform, "MuzzleSocket")
                ?? throw new InvalidOperationException("K11 MuzzleSocket is missing after import.");

            PrototypePlayerHealth health = player.GetComponent<PrototypePlayerHealth>();
            PrototypePlayerCombat combat = player.GetComponent<PrototypePlayerCombat>();
            Camera camera = Camera.main;
            combat.Configure(movement, health, camera, muzzle);

            K1V2AnimatorDriver driver = animator.GetComponent<K1V2AnimatorDriver>();
            if (driver == null)
            {
                driver = animator.gameObject.AddComponent<K1V2AnimatorDriver>();
            }
            driver.Configure(movement, combat, animator);
            return movement;
        }

        private static string ConfigureField(Transform root, GameObject visualAsset, GameObject collisionAsset)
        {
            Transform field = root.Find("DemoField")
                ?? throw new InvalidOperationException("DemoField is missing from V1 scene.");
            foreach (Transform child in field)
            {
                child.gameObject.SetActive(false);
            }

            GameObject visual = EnsureAssetInstance(field, visualAsset, "DesignFieldVisualV2");
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            GameObject collision = EnsureAssetInstance(field, collisionAsset, "DesignFieldCollisionV2");
            collision.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            collision.transform.localScale = Vector3.one;
            ConfigureCollisionGeometry(collision);
            return "DesignAssets_V2/central_industrial_belt_v2.glb";
        }

        private static void ConfigureEnemies(Transform root)
        {
            Transform enemies = root.Find("Enemies");
            if (enemies == null)
            {
                return;
            }

            SetWorldPosition(enemies, "Enemy_Scout_A", new Vector3(4.2f, 0.05f, 0.25f));
            SetWorldPosition(enemies, "Enemy_Scout_B", new Vector3(8f, 1.10f, -0.70f));
            SetWorldPosition(enemies, "Enemy_Guard_C", new Vector3(14f, 3.5f, 1.4f));
        }

        private static void ConfigureGuide(Transform root, PrototypePlayerController movement, string fieldLabel)
        {
            PrototypePlayerHealth health = movement.GetComponent<PrototypePlayerHealth>();
            PrototypePlayerCombat combat = movement.GetComponent<PrototypePlayerCombat>();
            PrototypeCombatDirector director = root.Find("PrototypeCombatDirector")
                ?.GetComponent<PrototypeCombatDirector>();
            PrototypeGuideOverlay guide = root.Find("PrototypeGuideOverlay")
                ?.GetComponent<PrototypeGuideOverlay>();
            if (health != null && combat != null && director != null && guide != null)
            {
                guide.Configure(movement, health, combat, director, "DesignAssets_V2/yamada_k1_rigged_v2.glb", fieldLabel);
            }
        }

        private static void ConfigureCollisionGeometry(GameObject collisionRoot)
        {
            foreach (Renderer renderer in collisionRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (MeshFilter filter in collisionRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshCollider collider = filter.GetComponent<MeshCollider>();
                if (collider == null)
                {
                    collider = filter.gameObject.AddComponent<MeshCollider>();
                }
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
            }
        }

        private static void DisablePhysics(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
            foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
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
            instance.SetActive(true);
            return instance;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindDescendant(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static void SetWorldPosition(Transform parent, string childName, Vector3 position)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.position = position;
            }
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException("Required V2 asset is missing: " + path);
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
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
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(scene => !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase)))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
