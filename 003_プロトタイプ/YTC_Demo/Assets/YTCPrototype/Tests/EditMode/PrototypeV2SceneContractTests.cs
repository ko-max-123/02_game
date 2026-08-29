using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YTCPrototype.Tests
{
    public sealed class PrototypeV2SceneContractTests
    {
        private const string ScenePath = "Assets/YTCPrototype/Scenes/YTC_Demo_V2.unity";
        private const string PlayerAssetPath =
            "Assets/YTCPrototype/ImportedDesignAssetsV2/Models/yamada_k1_rigged_v2.glb";

        private static readonly string[] ExpectedClips =
        {
            "Idle_Loop",
            "WalkForward_Loop",
            "WalkDepth_Positive_Loop",
            "WalkDepth_Negative_Loop",
            "Turn180_L",
            "Turn180_R",
            "Jump_Start",
            "Jump_Loop",
            "Land",
            "Jet_Start",
            "Jet_Loop",
            "Jet_End",
            "Shoot_Recoil"
        };

        [OneTimeSetUp]
        public void OpenV2Scene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void DirectGlbImport_ContainsRigSkinnedMeshSocketsAndThirteenClips()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(prefab.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.GreaterThan(0));

            Assert.That(FindDescendant(prefab.transform, "K1_Root"), Is.Not.Null);
            Assert.That(FindDescendant(prefab.transform, "WeaponSocket_R"), Is.Not.Null);
            Assert.That(FindDescendant(prefab.transform, "JetSocket_L"), Is.Not.Null);
            Assert.That(FindDescendant(prefab.transform, "JetSocket_R"), Is.Not.Null);

            string[] clipNames = AssetDatabase.LoadAllAssetsAtPath(PlayerAssetPath)
                .OfType<AnimationClip>()
                .Select(clip => clip.name)
                .OrderBy(name => name)
                .ToArray();
            Assert.That(clipNames, Is.EquivalentTo(ExpectedClips));
        }

        [Test]
        public void AnimatorController_UsesOnlyContractLoopingClips()
        {
            HashSet<string> expectedLooping = new HashSet<string>
            {
                "Idle_Loop",
                "WalkForward_Loop",
                "WalkDepth_Positive_Loop",
                "WalkDepth_Negative_Loop",
                "Jump_Loop",
                "Jet_Loop"
            };
            Animator animator = GameObject.Find("YamadaK1RiggedV2").GetComponentInChildren<Animator>(true);
            Dictionary<string, bool> actual = animator.runtimeAnimatorController.animationClips
                .GroupBy(clip => clip.name)
                .Select(group => group.First())
                .ToDictionary(
                    clip => clip.name,
                    clip => AnimationUtility.GetAnimationClipSettings(clip).loopTime);

            Assert.That(actual.Keys, Is.EquivalentTo(ExpectedClips));
            foreach (KeyValuePair<string, bool> pair in actual)
            {
                Assert.That(pair.Value, Is.EqualTo(expectedLooping.Contains(pair.Key)), pair.Key);
            }
        }

        [Test]
        public void Player_UsesContractCapsuleLaneAnimatorAndWeaponMuzzle()
        {
            GameObject player = GameObject.Find("Yamada_K1_Player");
            Assert.That(player, Is.Not.Null);

            CharacterController capsule = player.GetComponent<CharacterController>();
            Assert.That(capsule.center.y, Is.EqualTo(0.99f).Within(0.0001f));
            Assert.That(capsule.height, Is.EqualTo(1.88f).Within(0.0001f));
            Assert.That(capsule.radius, Is.EqualTo(0.31f).Within(0.0001f));

            PrototypePlayerController movement = player.GetComponent<PrototypePlayerController>();
            Assert.That(movement.MinimumDepth, Is.EqualTo(-2.56f).Within(0.0001f));
            Assert.That(movement.MaximumDepth, Is.EqualTo(2.56f).Within(0.0001f));
            Assert.That(movement.UsesAnimatedTurning, Is.True);

            Transform visual = player.transform.Find("PlayerVisualRoot/YamadaK1RiggedV2");
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.gameObject.activeSelf, Is.True);
            Assert.That(player.transform.Find("PlayerVisualRoot/YamadaK1DesignVisual").gameObject.activeSelf, Is.False);
            Assert.That(player.transform.Find("PlayerVisualRoot/PrimitiveK1Fallback").gameObject.activeSelf, Is.False);

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.runtimeAnimatorController, Is.TypeOf<AnimatorController>());
            Assert.That(animator.GetComponent<K1V2AnimatorDriver>(), Is.Not.Null);
            Assert.That(FindDescendant(visual, "K11_Rifle_V2"), Is.Not.Null);
            Assert.That(FindDescendant(visual, "MuzzleSocket"), Is.Not.Null);
        }

        [Test]
        public void AnimatorController_ContainsAllRequiredStatesAndSeparateShootLayer()
        {
            Animator animator = GameObject.Find("YamadaK1RiggedV2").GetComponentInChildren<Animator>(true);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.layers.Length, Is.EqualTo(2));

            HashSet<string> states = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(child => child.state.name)
                .ToHashSet();
            Assert.That(states, Is.SupersetOf(ExpectedClips));
            Assert.That(controller.parameters.Select(parameter => parameter.name),
                Does.Contain(K1V2AnimatorDriver.LocomotionRateParameter));
        }

        [Test]
        public void Field_UsesV2DisplayAndHiddenCollisionAtSharedOrigin()
        {
            Transform field = GameObject.Find("YTC_PrototypeRoot").transform.Find("DemoField");
            Transform visual = field.Find("DesignFieldVisualV2");
            Transform collision = field.Find("DesignFieldCollisionV2");
            Assert.That(visual, Is.Not.Null);
            Assert.That(collision, Is.Not.Null);
            Assert.That(visual.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(collision.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(visual.localScale, Is.EqualTo(Vector3.one));
            Assert.That(collision.localScale, Is.EqualTo(Vector3.one));
            Assert.That(collision.GetComponentsInChildren<MeshCollider>(true).Length, Is.GreaterThan(0));
            Assert.That(collision.GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled), Is.True);
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
    }
}
