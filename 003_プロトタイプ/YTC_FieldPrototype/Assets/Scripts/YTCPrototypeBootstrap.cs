using UnityEngine;
using UnityEngine.Rendering;

namespace YTC.Prototype
{
    public sealed class YTCPrototypeBootstrap : MonoBehaviour
    {
        private const string YamadaResourcePath = "Characters/Yamada/Yamada";
        private const string DemoFieldResourcePath = "Environment/DemoField/DemoField";
        private static bool runtimeCreated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            runtimeCreated = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeExists()
        {
            if (runtimeCreated || FindAnyObjectByType<YTCPrototypeBootstrap>() != null)
            {
                return;
            }

            runtimeCreated = true;
            var bootstrapObject = new GameObject("[YTC] Prototype Bootstrap");
            bootstrapObject.AddComponent<YTCPrototypeBootstrap>();
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            CreateLighting();

            bool fieldLoaded = CreateEnvironment();
            YamadaPrototypeController controller = CreatePlayer(out bool modelLoaded);
            CreateCamera(controller.transform);

            PrototypeStatusOverlay overlay = gameObject.AddComponent<PrototypeStatusOverlay>();
            overlay.Configure(controller, modelLoaded, fieldLoaded);

            Debug.Log(
                $"YTC prototype ready. Yamada model: {modelLoaded}; Demo field: {fieldLoaded}.");
        }

        private static void CreateLighting()
        {
            if (FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.43f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.18f, 0.23f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.07f, 0.09f);

            var lightObject = new GameObject("Prototype Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static bool CreateEnvironment()
        {
            var environmentRoot = new GameObject("[YTC] Environment");

            GameObject fieldAsset = Resources.Load<GameObject>(DemoFieldResourcePath);
            if (fieldAsset != null)
            {
                GameObject field = Instantiate(fieldAsset, environmentRoot.transform);
                field.name = "DemoField_DesignAsset";
                field.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                DisableColliders(field);
                CreateOfficialFieldColliders(environmentRoot.transform);
                return true;
            }

            CreateBlock(
                "Collision Ground",
                environmentRoot.transform,
                new Vector3(0f, -0.15f, 0f),
                new Vector3(40f, 0.3f, 40f),
                new Color(0.10f, 0.14f, 0.19f));
            CreateFallbackField(environmentRoot.transform);
            return false;
        }

        private static void CreateFallbackField(Transform parent)
        {
            CreateBlock(
                "Spawn Pad",
                parent,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(4f, 0.1f, 4f),
                new Color(0.08f, 0.48f, 0.62f));
            CreateBlock(
                "Jump Step Low",
                parent,
                new Vector3(4f, 0.4f, 2f),
                new Vector3(2.5f, 0.8f, 2.5f),
                new Color(0.28f, 0.34f, 0.42f));
            CreateBlock(
                "Jump Step High",
                parent,
                new Vector3(8f, 0.8f, 2f),
                new Vector3(2.5f, 1.6f, 2.5f),
                new Color(0.36f, 0.42f, 0.50f));
            CreateBlock(
                "Lane Wall North",
                parent,
                new Vector3(0f, 1.5f, 8f),
                new Vector3(18f, 3f, 0.6f),
                new Color(0.22f, 0.25f, 0.31f));
            CreateBlock(
                "Lane Wall South",
                parent,
                new Vector3(0f, 1.5f, -8f),
                new Vector3(18f, 3f, 0.6f),
                new Color(0.22f, 0.25f, 0.31f));

            for (int index = 0; index < 6; index++)
            {
                float x = -10f + index * 4f;
                float z = index % 2 == 0 ? 5f : -5f;
                CreateBlock(
                    $"Course Marker {index + 1:00}",
                    parent,
                    new Vector3(x, 1f, z),
                    new Vector3(0.6f, 2f, 0.6f),
                    new Color(0.88f, 0.43f, 0.10f));
            }
        }

        private static YamadaPrototypeController CreatePlayer(out bool modelLoaded)
        {
            Vector3 spawnPoint = new Vector3(-13.7f, 0.05f, 0f);
            var player = new GameObject("Yamada_Player");
            player.transform.position = spawnPoint;

            var characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.86f;
            characterController.radius = 0.38f;
            characterController.center = new Vector3(0f, 0.93f, 0f);
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 48f;
            characterController.skinWidth = 0.04f;

            YamadaPrototypeController controller = player.AddComponent<YamadaPrototypeController>();
            controller.Configure(spawnPoint);
            modelLoaded = CreatePlayerVisual(player.transform);
            return controller;
        }

        private static bool CreatePlayerVisual(Transform player)
        {
            GameObject modelAsset = Resources.Load<GameObject>(YamadaResourcePath);
            if (modelAsset == null)
            {
                CreateFallbackPlayerVisual(player);
                return false;
            }

            GameObject model = Instantiate(modelAsset, player);
            model.name = "Yamada_DesignAsset";
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            NormalizeCharacterVisual(model, player.position, 2.3f);
            return true;
        }

        private static void CreateFallbackPlayerVisual(Transform player)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Yamada_CapsuleFallback";
            body.transform.SetParent(player, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.78f, 0.95f, 0.78f);
            body.GetComponent<Collider>().enabled = false;
            body.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                "Yamada Fallback Material",
                new Color(0.08f, 0.65f, 0.86f));

            GameObject facingMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            facingMarker.name = "Facing Marker";
            facingMarker.transform.SetParent(player, false);
            facingMarker.transform.localPosition = new Vector3(0f, 1.15f, 0.38f);
            facingMarker.transform.localScale = new Vector3(0.34f, 0.22f, 0.12f);
            facingMarker.GetComponent<Collider>().enabled = false;
            facingMarker.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                "Facing Marker Material",
                new Color(1f, 0.58f, 0.12f));
        }

        private static void NormalizeCharacterVisual(
            GameObject model,
            Vector3 playerPosition,
            float targetHeight)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            if (bounds.size.y > 0.001f)
            {
                float scale = targetHeight / bounds.size.y;
                model.transform.localScale *= scale;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float groundOffset = playerPosition.y - bounds.min.y;
            model.transform.position += Vector3.up * groundOffset;
        }

        private static void DisableColliders(GameObject root)
        {
            SetStaticRecursively(root);
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private static void CreateRampCollider(Transform parent)
        {
            const float run = 4.4f;
            const float rise = 1.1733333f;
            const float thickness = 0.2f;
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(run * run + rise * rise);
            Vector3 surfaceNormal = new Vector3(
                -Mathf.Sin(angle * Mathf.Deg2Rad),
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f);

            var ramp = new GameObject("COLLISION_ASCENT_RAMP");
            ramp.transform.SetParent(parent, false);
            ramp.transform.localPosition =
                new Vector3(7.2f, 0.32f, 0f) - surfaceNormal * (thickness * 0.5f);
            ramp.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var collider = ramp.AddComponent<BoxCollider>();
            collider.size = new Vector3(length, thickness, 4.4f);
            ramp.isStatic = true;
        }

        private static void CreateOfficialFieldColliders(Transform parent)
        {
            CreateBoxCollider(
                "COLLISION_START_PLATFORM",
                parent,
                new Vector3(-10f, -0.15f, 0f),
                new Vector3(10f, 0.3f, 4.4f));
            CreateBoxCollider(
                "COLLISION_MIDDLE_PLATFORM",
                parent,
                new Vector3(1.75f, -0.15f, 0f),
                new Vector3(9.5f, 0.3f, 4.4f));
            CreateBoxCollider(
                "COLLISION_GOAL_PLATFORM",
                parent,
                new Vector3(12.2f, 0.65f, 0f),
                new Vector3(5.6f, 0.3f, 4.4f));
            CreateBoxCollider(
                "COLLISION_LOW_OBSTACLE",
                parent,
                new Vector3(-7.3f, 0.3f, 0f),
                new Vector3(1f, 0.6f, 1.7f));
            CreateBoxCollider(
                "COLLISION_STEP_01",
                parent,
                new Vector3(-1.4f, 0.1f, 0f),
                new Vector3(1.05f, 0.2f, 2.2f));
            CreateBoxCollider(
                "COLLISION_STEP_02",
                parent,
                new Vector3(-0.3f, 0.2f, 0f),
                new Vector3(1.05f, 0.4f, 2.2f));
            CreateBoxCollider(
                "COLLISION_STEP_03",
                parent,
                new Vector3(0.8f, 0.3f, 0f),
                new Vector3(1.05f, 0.6f, 2.2f));
            CreateRampCollider(parent);
        }

        private static void CreateBoxCollider(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size)
        {
            var colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = position;
            colliderObject.AddComponent<BoxCollider>().size = size;
            colliderObject.isStatic = true;
        }

        private static void SetStaticRecursively(GameObject root)
        {
            root.isStatic = true;
            foreach (Transform child in root.transform)
            {
                SetStaticRecursively(child.gameObject);
            }
        }

        private static void CreateCamera(Transform target)
        {
            Camera existingCamera = Camera.main;
            GameObject cameraObject;
            Camera camera;

            if (existingCamera == null)
            {
                cameraObject = new GameObject("Prototype Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            else
            {
                camera = existingCamera;
                cameraObject = existingCamera.gameObject;
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.8f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 250f;
            camera.backgroundColor = new Color(0.03f, 0.05f, 0.08f);

            PrototypeFollowCamera followCamera = cameraObject.GetComponent<PrototypeFollowCamera>();
            if (followCamera == null)
            {
                followCamera = cameraObject.AddComponent<PrototypeFollowCamera>();
            }

            followCamera.Configure(target);
        }

        private static GameObject CreateBlock(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = CreateMaterial($"{name} Material", color);
            block.isStatic = true;
            return block;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = name,
                color = color
            };
            return material;
        }
    }
}
