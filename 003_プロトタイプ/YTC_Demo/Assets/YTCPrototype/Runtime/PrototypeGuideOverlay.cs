using UnityEngine;

namespace YTCPrototype
{
    public sealed class PrototypeGuideOverlay : MonoBehaviour
    {
        [SerializeField] private PrototypePlayerController player;
        [SerializeField] private string playerAssetLabel = "Primitive fallback";
        [SerializeField] private string fieldAssetLabel = "Primitive fallback";

        private GUIStyle boxStyle;
        private GUIStyle textStyle;

        public void Configure(
            PrototypePlayerController trackedPlayer,
            string resolvedPlayerAsset,
            string resolvedFieldAsset)
        {
            player = trackedPlayer;
            playerAssetLabel = resolvedPlayerAsset;
            fieldAssetLabel = resolvedFieldAsset;
        }

        private void OnGUI()
        {
            EnsureStyles();

            const float width = 430f;
            const float height = 176f;
            Rect panel = new Rect(18f, 18f, width, height);
            GUI.Box(panel, GUIContent.none, boxStyle);

            string state = player == null
                ? "PLAYER: not assigned"
                : $"STATE: {(player.IsFlying ? "FLYING" : player.IsGrounded ? "GROUNDED" : "AIRBORNE")}  LANE Z={player.CurrentDepth:0.00}";
            string jet = player == null
                ? "JET: unavailable"
                : $"JET: {(player.IsFlying ? "ENGAGED" : player.CurrentJetEnergy <= 0.01f ? "EMPTY" : "READY")}  ENERGY {player.CurrentJetEnergy:0}/{player.MaximumJetEnergy:0}";

            string guide =
                "YTC MOVEMENT PROTOTYPE\n"
                + "A / D : Move left / right\n"
                + "W / S : Limited depth lane\n"
                + "Space : Jump   |   Hold Space : Jet flight\n"
                + "Backspace : Reset to spawn\n"
                + state + "\n"
                + jet + "\n"
                + $"PLAYER ASSET: {playerAssetLabel}\nFIELD ASSET: {fieldAssetLabel}";

            GUI.Label(new Rect(32f, 28f, width - 28f, height - 20f), guide, textStyle);
        }

        private void EnsureStyles()
        {
            if (boxStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box);

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
