using UnityEngine;

namespace YTC.Prototype
{
    public sealed class PrototypeStatusOverlay : MonoBehaviour
    {
        private YamadaPrototypeController controller;
        private bool modelLoaded;
        private bool fieldLoaded;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;

        public void Configure(
            YamadaPrototypeController playerController,
            bool usesYamadaModel,
            bool usesDemoField)
        {
            controller = playerController;
            modelLoaded = usesYamadaModel;
            fieldLoaded = usesDemoField;
        }

        private void OnGUI()
        {
            EnsureStyles();

            var panel = new Rect(16f, 16f, 430f, 142f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(32f, 28f, 390f, 24f), "YTC / YAMADA FIELD PROTOTYPE", titleStyle);
            GUI.Label(
                new Rect(32f, 58f, 390f, 22f),
                "MOVE: W A S D    JUMP: SPACE    DEBUG RESET: BACKSPACE",
                labelStyle);
            GUI.Label(
                new Rect(32f, 84f, 390f, 22f),
                $"MODEL: {(modelLoaded ? "YAMADA ASSET" : "CAPSULE FALLBACK")}",
                labelStyle);
            GUI.Label(
                new Rect(32f, 106f, 390f, 22f),
                $"FIELD: {(fieldLoaded ? "DEMO ASSET" : "PROCEDURAL FALLBACK")}",
                labelStyle);
            GUI.Label(
                new Rect(32f, 128f, 390f, 22f),
                $"GROUNDED: {(controller != null && controller.IsGrounded ? "YES" : "NO")}",
                labelStyle);
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
