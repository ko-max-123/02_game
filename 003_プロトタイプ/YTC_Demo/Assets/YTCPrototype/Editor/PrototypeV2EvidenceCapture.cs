using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YTCPrototype.Editor
{
    public static class PrototypeV2EvidenceCapture
    {
        private const int Width = 1920;
        private const int Height = 1080;

        public static void CaptureFromCommandLine()
        {
            PrototypeV2SceneBuilder.BuildOrRefreshScene();
            EditorSceneManager.OpenScene(PrototypeV2SceneBuilder.ScenePath, OpenSceneMode.Single);

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root could not be resolved.");
            string prototypeRoot = Directory.GetParent(projectRoot)?.FullName
                ?? throw new InvalidOperationException("Prototype root could not be resolved.");
            string outputRoot = Path.Combine(prototypeRoot, "YTC_V2_Evidence");
            Directory.CreateDirectory(outputRoot);

            GameObject player = GameObject.Find("Yamada_K1_Player")
                ?? throw new InvalidOperationException("V2 player is missing.");
            Transform visual = player.transform.Find("PlayerVisualRoot/YamadaK1RiggedV2")
                ?? throw new InvalidOperationException("V2 visual is missing.");
            Animator animator = visual.GetComponentInChildren<Animator>(true)
                ?? throw new InvalidOperationException("V2 Animator is missing.");
            Camera camera = Camera.main
                ?? throw new InvalidOperationException("V2 camera is missing.");

            ConfigureCamera(camera, player.transform.position.x, 2.0f, 3.6f);
            SampleState(animator, K1V2AnimatorDriver.IdleState, 0.25f);
            Capture(camera, Path.Combine(outputRoot, "01_normal_gameplay_distance.png"));

            SampleState(animator, K1V2AnimatorDriver.WalkState, 0.5f);
            ConfigureCamera(camera, player.transform.position.x, 1.15f, 2.25f);
            Capture(camera, Path.Combine(outputRoot, "02_walk_foot_contact.png"));

            SampleState(animator, K1V2AnimatorDriver.ShootState, 0.35f, 1);
            ConfigureCamera(camera, player.transform.position.x + 0.45f, 1.45f, 2.15f);
            Capture(camera, Path.Combine(outputRoot, "03_k11_grip_and_muzzle.png"));

            ConfigureCamera(camera, 0f, 4.3f, 8.6f);
            SampleState(animator, K1V2AnimatorDriver.IdleState, 0.1f);
            Capture(camera, Path.Combine(outputRoot, "04_field_readability.png"));

            string[] sequenceStates =
            {
                K1V2AnimatorDriver.IdleState,
                K1V2AnimatorDriver.WalkState,
                K1V2AnimatorDriver.TurnLeftState,
                K1V2AnimatorDriver.JumpLoopState,
                K1V2AnimatorDriver.JetLoopState,
                K1V2AnimatorDriver.ShootState
            };
            for (int index = 0; index < sequenceStates.Length; index++)
            {
                int layer = sequenceStates[index] == K1V2AnimatorDriver.ShootState ? 1 : 0;
                SampleState(animator, sequenceStates[index], 0.5f, layer);
                ConfigureCamera(camera, player.transform.position.x + 1.2f, 2.2f, 4.2f);
                Capture(
                    camera,
                    Path.Combine(outputRoot, $"sequence_{index + 1:00}_{sequenceStates[index]}.png"));
            }

            WriteImportReport(outputRoot, visual, animator);
            Debug.Log("YTC V2 evidence capture completed: " + outputRoot);
        }

        private static void ConfigureCamera(Camera camera, float x, float y, float size)
        {
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.transform.SetPositionAndRotation(new Vector3(x, y, -16f), Quaternion.identity);
        }

        private static void SampleState(Animator animator, string stateName, float normalizedTime, int layer = 0)
        {
            animator.Rebind();
            animator.Update(0f);
            if (layer > 0)
            {
                animator.SetLayerWeight(layer, 1f);
            }
            else if (animator.layerCount > 1)
            {
                animator.SetLayerWeight(1, 0f);
            }
            animator.Play(stateName, layer, normalizedTime);
            animator.Update(0f);
        }

        private static void Capture(Camera camera, string outputPath)
        {
            RenderTexture renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void WriteImportReport(string outputRoot, Transform visual, Animator animator)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips
                .GroupBy(clip => clip.name)
                .Select(group => group.First())
                .OrderBy(clip => clip.name)
                .ToArray();
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            List<string> hierarchy = new List<string>();
            CollectHierarchy(visual, hierarchy, 0);
            StringBuilder report = new StringBuilder();
            report.AppendLine("# YTC V2 Unity Import Report");
            report.AppendLine();
            report.AppendLine($"- Unity: {Application.unityVersion}");
            report.AppendLine("- Importer: com.unity.cloud.gltfast 6.19.0");
            report.AppendLine("- Source: DesignAssets_V2/Models/yamada_k1_rigged_v2.glb");
            report.AppendLine("- Method: direct GLB / Mecanim / Generic / in-place / root motion disabled");
            report.AppendLine($"- Root local position: {visual.localPosition}");
            report.AppendLine($"- Root local rotation: {visual.localEulerAngles}");
            report.AppendLine($"- Root local scale: {visual.localScale}");
            report.AppendLine($"- Render bounds size: {bounds.size}");
            report.AppendLine($"- SkinnedMeshRenderer count: {visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");
            report.AppendLine($"- Animator layers: {animator.layerCount}");
            report.AppendLine($"- Animation clips: {clips.Length}");
            foreach (AnimationClip clip in clips)
            {
                bool loopTime = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
                report.AppendLine($"  - {clip.name}: {clip.length:F3}s, loop={loopTime}");
            }
            report.AppendLine();
            report.AppendLine("## Imported hierarchy");
            report.AppendLine("```text");
            foreach (string line in hierarchy)
            {
                report.AppendLine(line);
            }
            report.AppendLine("```");
            File.WriteAllText(Path.Combine(outputRoot, "V2_IMPORT_REPORT.md"), report.ToString());
        }

        private static void CollectHierarchy(Transform transform, List<string> output, int depth)
        {
            output.Add(new string(' ', depth * 2) + transform.name);
            foreach (Transform child in transform)
            {
                CollectHierarchy(child, output, depth + 1);
            }
        }
    }
}
