using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class DjBoothLighting : MonoBehaviour
    {
        private Renderer[] strips;
        private Vector3[] scales, positions;
        private MaterialPropertyBlock properties;

        private void Start()
        {
            var selected = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                if (renderer.name.StartsWith("Booth Equalizer") || renderer.name.Contains("Rail") || renderer.name.StartsWith("Booth Angle Light")) selected.Add(renderer);
            strips = selected.ToArray();
            scales = new Vector3[strips.Length];
            positions = new Vector3[strips.Length];
            for (int i = 0; i < strips.Length; i++)
            {
                scales[i] = strips[i].transform.localScale;
                positions[i] = strips[i].transform.localPosition;
            }
            properties = new MaterialPropertyBlock();
        }

        private void Update()
        {
            float beat = ClubBeatClock.Beat;
            float pulse = Mathf.Exp(-Mathf.Repeat(beat, 1f) * 5f);
            for (int i = 0; i < strips.Length; i++)
            {
                Renderer strip = strips[i];
                if (strip == null) continue;
                Color color = Color.HSVToRGB(Mathf.Repeat(beat * 0.08f + i * 0.065f, 1f), 0.95f, 1f);
                float chase = 0.5f + 0.5f * Mathf.Sin(beat * Mathf.PI * 2f - i * 0.65f);
                float brightness = 0.18f + chase * 0.48f + pulse * 0.34f;
                properties.SetColor("_Color", color * brightness * 0.2f);
                properties.SetColor("_EmissionColor", color * brightness);
                strip.SetPropertyBlock(properties);
                if (!strip.name.StartsWith("Booth Equalizer")) continue;
                float height = scales[i].y * (0.3f + chase * 0.8f + pulse * 0.45f);
                strip.transform.localScale = new Vector3(scales[i].x, height, scales[i].z);
                strip.transform.localPosition = positions[i] + Vector3.up * (height - scales[i].y) * 0.5f;
            }
        }
    }
}
