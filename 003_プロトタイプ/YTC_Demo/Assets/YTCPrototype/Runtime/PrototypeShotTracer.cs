using UnityEngine;

namespace YTCPrototype
{
    public sealed class PrototypeShotTracer : MonoBehaviour
    {
        private static Material sharedMaterial;

        private LineRenderer[] lines;
        private Color[] colors;
        private float lifetime;
        private float remaining;

        public static void SpawnTelegraph(Vector3 start, Vector3 end, float duration)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.01f)
            {
                return;
            }

            Vector3 direction = delta / distance;
            const float segmentLength = 0.26f;
            const float segmentSpacing = 0.56f;
            int segmentCount = Mathf.Clamp(Mathf.CeilToInt(distance / segmentSpacing), 1, 20);
            for (int i = 0; i < segmentCount; i++)
            {
                float segmentStartDistance = i * segmentSpacing;
                if (segmentStartDistance >= distance)
                {
                    break;
                }

                float segmentEndDistance = Mathf.Min(segmentStartDistance + segmentLength, distance);
                SpawnComposite(
                    "EnemyTelegraphDash",
                    start + direction * segmentStartDistance,
                    start + direction * segmentEndDistance,
                    new[] { new Color(1f, 0.08f, 0.06f, 0.62f) },
                    new[] { 0.024f },
                    Mathf.Max(0.1f, duration));
            }
        }

        public static void SpawnImpact(Vector3 point)
        {
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                SpawnComposite(
                    "HitSpark",
                    point,
                    point + direction * 0.42f,
                    new[] { new Color(1f, 0.52f, 0.08f), Color.white },
                    new[] { 0.055f, 0.022f },
                    0.13f);
            }
        }

        public static void SpawnDefeat(Vector3 point)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                SpawnComposite(
                    "DefeatSpark",
                    point,
                    point + direction * 0.8f,
                    new[] { new Color(1f, 0.34f, 0.05f), Color.white },
                    new[] { 0.075f, 0.028f },
                    0.2f);
            }
        }

        private static void SpawnComposite(
            string objectName,
            Vector3 start,
            Vector3 end,
            Color[] tracerColors,
            float[] widths,
            float duration)
        {
            GameObject tracer = new GameObject(objectName);
            PrototypeShotTracer effect = tracer.AddComponent<PrototypeShotTracer>();
            effect.Initialize(start, end, tracerColors, widths, duration);
        }

        private void Initialize(
            Vector3 start,
            Vector3 end,
            Color[] tracerColors,
            float[] widths,
            float duration)
        {
            colors = tracerColors;
            lifetime = duration;
            remaining = duration;
            lines = new LineRenderer[tracerColors.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                GameObject layer = new GameObject($"TracerLayer_{i}");
                layer.transform.SetParent(transform, false);
                LineRenderer targetLine = layer.AddComponent<LineRenderer>();
                targetLine.useWorldSpace = true;
                targetLine.positionCount = 2;
                targetLine.SetPosition(0, start);
                targetLine.SetPosition(1, end);
                targetLine.startWidth = widths[i];
                targetLine.endWidth = widths[i] * 0.4f;
                targetLine.numCapVertices = 3;
                targetLine.sharedMaterial = GetSharedMaterial();
                targetLine.startColor = tracerColors[i];
                targetLine.endColor = tracerColors[i];
                targetLine.sortingOrder = i;
                lines[i] = targetLine;
            }
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            float alpha = Mathf.Clamp01(remaining / lifetime);
            for (int i = 0; i < lines.Length; i++)
            {
                Color source = colors[i];
                Color faded = new Color(source.r, source.g, source.b, source.a * alpha);
                lines[i].startColor = faded;
                lines[i].endColor = faded;
            }

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private static Material GetSharedMaterial()
        {
            if (sharedMaterial != null)
            {
                return sharedMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            sharedMaterial = new Material(shader)
            {
                name = "YTC Runtime Tracer Material",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedMaterial;
        }
    }
}
