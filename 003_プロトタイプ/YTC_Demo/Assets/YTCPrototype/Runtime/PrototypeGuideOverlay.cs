using UnityEngine;

namespace YTCPrototype
{
    public sealed class PrototypeGuideOverlay : MonoBehaviour
    {
        [SerializeField] private PrototypePlayerController player;
        [SerializeField] private PrototypePlayerHealth playerHealth;
        [SerializeField] private PrototypePlayerCombat playerCombat;
        [SerializeField] private PrototypeCombatDirector combatDirector;
        [SerializeField] private string playerAssetLabel = "Primitive fallback";
        [SerializeField] private string fieldAssetLabel = "Primitive fallback";

        private GUIStyle boxStyle;
        private GUIStyle textStyle;

        public void Configure(
            PrototypePlayerController trackedPlayer,
            PrototypePlayerHealth trackedHealth,
            PrototypePlayerCombat trackedCombat,
            PrototypeCombatDirector director,
            string resolvedPlayerAsset,
            string resolvedFieldAsset)
        {
            player = trackedPlayer;
            playerHealth = trackedHealth;
            playerCombat = trackedCombat;
            combatDirector = director;
            playerAssetLabel = resolvedPlayerAsset;
            fieldAssetLabel = resolvedFieldAsset;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawDamageFeedback();
            DrawStatusPanel();
            DrawHealthPanel();
            DrawEnemyCounter();
            DrawControls();
            DrawCrosshair();
            DrawVictoryMessage();
        }

        private void DrawStatusPanel()
        {
            const float width = 390f;
            const float height = 112f;
            Rect panel = new Rect(48f, 48f, width, height);
            GUI.Box(panel, GUIContent.none, boxStyle);

            string state = player == null
                ? "PLAYER: not assigned"
                : $"STATE: {(player.IsFlying ? "FLYING" : player.IsGrounded ? "GROUNDED" : "AIRBORNE")}  LANE Z={player.CurrentDepth:0.00}";
            string jet = player == null
                ? "JET: unavailable"
                : $"JET: {(player.IsFlying ? "ENGAGED" : player.CurrentJetEnergy <= 0.01f ? "EMPTY" : "READY")}  ENERGY {player.CurrentJetEnergy:0}/{player.MaximumJetEnergy:0}";
            string status =
                "YTC COMBAT PROTOTYPE\n"
                + state + "\n"
                + jet + "\n"
                + $"K1: {playerAssetLabel}";

            GUI.Label(new Rect(panel.x + 14f, panel.y + 9f, width - 24f, height - 16f), status, textStyle);
        }

        private void DrawHealthPanel()
        {
            const float width = 330f;
            const float height = 66f;
            Rect panel = new Rect(48f, Screen.height - 132f, width, height);
            GUI.Box(panel, GUIContent.none, boxStyle);

            float healthNormalized = playerHealth != null ? playerHealth.HealthNormalized : 0f;
            Color previous = GUI.color;
            GUI.color = new Color(0.95f, 0.42f, 0.08f, 1f);
            GUI.DrawTexture(new Rect(panel.x + 15f, panel.y + 33f, width - 30f, 18f), Texture2D.whiteTexture);
            GUI.color = healthNormalized < 0.3f && Mathf.PingPong(Time.time * 5f, 1f) > 0.5f
                ? new Color(1f, 0.12f, 0.08f, 1f)
                : Color.white;
            GUI.DrawTexture(
                new Rect(panel.x + 18f, panel.y + 36f, (width - 36f) * healthNormalized, 12f),
                Texture2D.whiteTexture);
            GUI.color = previous;

            string healthText = playerHealth == null
                ? "ARMOR / HP unavailable"
                : $"ARMOR / HP  {playerHealth.CurrentHealth:0} / {playerHealth.MaximumHealth:0}"
                    + (playerHealth.IsRespawning ? "   RESPAWNING" : string.Empty);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 7f, width - 30f, 24f), healthText, textStyle);
        }

        private void DrawEnemyCounter()
        {
            Rect panel = new Rect(Screen.width - 278f, 48f, 230f, 64f);
            GUI.Box(panel, GUIContent.none, boxStyle);
            int remaining = combatDirector != null ? combatDirector.AliveEnemyCount : 0;
            GUIStyle enemyStyle = new GUIStyle(textStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                normal = { textColor = new Color(1f, 0.28f, 0.26f) }
            };
            GUI.Label(panel, $"▼  残敵 {remaining}", enemyStyle);
        }

        private void DrawControls()
        {
            float width = Mathf.Min(1120f, Screen.width - 96f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height - 58f, width, 42f);
            GUI.Box(panel, GUIContent.none, boxStyle);
            GUIStyle controlStyle = new GUIStyle(textStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17
            };
            GUI.Label(
                panel,
                "A/D 移動   W/S 奥行き   Space ジャンプ/飛行   LMB/J 射撃   R リスタート   Esc 終了",
                controlStyle);
        }

        private void DrawDamageFeedback()
        {
            if (playerHealth == null || playerHealth.HitFeedback <= 0f)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.03f, 0.01f, playerHealth.HitFeedback * 0.2f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            if (playerHealth.DamageDirectionFeedback > 0f)
            {
                bool fromRight = playerHealth.LastDamageDirection > 0f;
                GUIStyle directionStyle = new GUIStyle(textStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 52,
                    normal = { textColor = new Color(1f, 0.12f, 0.1f, playerHealth.DamageDirectionFeedback) }
                };
                Rect directionRect = new Rect(
                    fromRight ? Screen.width - 98f : 48f,
                    Screen.height * 0.5f - 45f,
                    50f,
                    90f);
                GUI.Label(directionRect, fromRight ? ">" : "<", directionStyle);
            }
        }

        private void DrawCrosshair()
        {
            if (playerCombat == null)
            {
                return;
            }

            Vector3 mouse = Input.mousePosition;
            GUI.Label(
                new Rect(mouse.x - 12f, Screen.height - mouse.y - 15f, 28f, 28f),
                "+",
                textStyle);
        }

        private void DrawVictoryMessage()
        {
            if (combatDirector == null || !combatDirector.AllEnemiesDefeated)
            {
                return;
            }

            GUIStyle victoryStyle = new GUIStyle(textStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34
            };
            Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 65f, 520f, 130f);
            GUI.Box(panel, GUIContent.none, boxStyle);
            GUI.Label(panel, "MISSION CLEAR\nPress R to restart", victoryStyle);

            Color previous = GUI.color;
            GUI.color = new Color(0.95f, 0.42f, 0.08f, 1f);
            GUI.DrawTexture(new Rect(panel.x + 72f, panel.y + 84f, panel.width - 144f, 3f), Texture2D.whiteTexture);
            GUI.color = previous;
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
