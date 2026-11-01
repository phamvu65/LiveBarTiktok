using UnityEngine;

namespace TikTokLiveGame
{
    public enum MovingHeadStyle
    {
        Spot,
        Wash,
        Beam,
        GoboDots,
        GoboStar
    }

    public sealed class MovingHeadFixture : MonoBehaviour
    {
        private const int FloorLightingLayer = 8;
        private static Texture2D softSpotCookie;
        private static Texture2D dotGoboCookie;
        private static Texture2D starGoboCookie;

        private Vector3 origin;
        private Color baseColor;
        private float phase;
        private MovingHeadStyle style;
        private Light spot;
        private LineRenderer beam;
        private float baseStartWidth;
        private float baseEndWidth;
        private Vector3 coverageCenter;
        private Vector2 coverageSize = new(6.5f, 4.85f);

        public void SetCoverage(Vector3 center, Vector2 sweepSize)
        {
            coverageCenter = center;
            coverageSize = sweepSize;
        }

        public void Initialize(Vector3 fixtureOrigin, Color color, float fixturePhase, MovingHeadStyle fixtureStyle, bool castsLight)
        {
            origin = fixtureOrigin;
            baseColor = color;
            phase = fixturePhase;
            style = fixtureStyle;

            bool wash = style == MovingHeadStyle.Wash;
            bool beamStyle = style == MovingHeadStyle.Beam;
            bool gobo = style == MovingHeadStyle.GoboDots || style == MovingHeadStyle.GoboStar;

            spot = gameObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = wash ? 25f : 28f;
            spot.spotAngle = wash ? 52f : beamStyle ? 12f : gobo ? 28f : 32f;
            spot.innerSpotAngle = spot.spotAngle * 0.72f;
            spot.intensity = wash ? 0.85f : beamStyle ? 3.4f : gobo ? 2.8f : 2.5f;
            spot.color = color;
            spot.cullingMask = -1; // Chiếu sáng mọi layer, bao gồm cả backdrop 2D
            spot.cookie = CookieForStyle(style);
            spot.renderMode = LightRenderMode.ForcePixel;
            spot.enabled = castsLight;

            beam = gameObject.AddComponent<LineRenderer>();
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.numCapVertices = 4;
            baseStartWidth = wash ? 0.38f : beamStyle ? 0.055f : gobo ? 0.1f : 0.14f;
            baseEndWidth = wash ? 3.4f : beamStyle ? 0.32f : gobo ? 0.9f : 1.5f;
            beam.startWidth = baseStartWidth;
            beam.endWidth = baseEndWidth;
            beam.sharedMaterial = GameMaterials.BackgroundSprite();
            UpdateBeamColor(0f);
            beam.enabled = true;
        }

        private void Update()
        {
            if (spot == null || beam == null) return;
            float time = Time.time;
            float beat = ClubBeatClock.Beat;
            float beatPulse = Mathf.Exp(-(beat - Mathf.Floor(beat)) * 6.5f);
            bool wash = style == MovingHeadStyle.Wash;
            bool beamStyle = style == MovingHeadStyle.Beam;
            bool gobo = style == MovingHeadStyle.GoboDots || style == MovingHeadStyle.GoboStar;
            float speed = wash ? 0.29f : beamStyle ? 0.71f : gobo ? 0.41f : 0.58f;
            Vector3 target = new(
                coverageCenter.x + Mathf.Sin(time * speed + phase) * coverageSize.x,
                0.08f + coverageCenter.y,
                coverageCenter.z + 0.65f + Mathf.Sin(time * (speed * 0.81f) + phase * 1.23f) * coverageSize.y
            );

            UpdateBeamColor(beatPulse);
            spot.color = baseColor;
            float baseIntensity = wash ? 2.6f : beamStyle ? 5.2f : gobo ? 4.5f : 4.2f;
            float pulseIntensity = wash ? 1.2f : beamStyle ? 2.4f : gobo ? 2f : 1.8f;
            spot.intensity = baseIntensity + beatPulse * pulseIntensity;
            transform.position = origin;
            transform.LookAt(target);
            if (gobo) transform.Rotate(Vector3.forward, time * (style == MovingHeadStyle.GoboStar ? 21f : -15f) + phase * 18f, Space.Self);
            beam.startWidth = baseStartWidth * (0.82f + beatPulse * 0.45f);
            beam.endWidth = baseEndWidth * (0.82f + beatPulse * 0.45f);
            beam.SetPosition(0, origin);
            beam.SetPosition(1, target);
        }

        private void UpdateBeamColor(float beatPulse)
        {
            float opacity = style == MovingHeadStyle.Wash ? 0.3f : style == MovingHeadStyle.Beam ? 0.6f : 0.42f;
            opacity *= 0.85f + beatPulse * 0.3f;
            beam.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, opacity);
            beam.endColor = new Color(baseColor.r, baseColor.g, baseColor.b, opacity * 0.35f);
        }

        private static Texture2D CookieForStyle(MovingHeadStyle fixtureStyle)
        {
            return fixtureStyle switch
            {
                MovingHeadStyle.GoboDots => DotGoboCookie(),
                MovingHeadStyle.GoboStar => StarGoboCookie(),
                _ => SoftSpotCookie()
            };
        }

        private static Texture2D SoftSpotCookie()
        {
            if (softSpotCookie != null) return softSpotCookie;
            const int size = 64;
            softSpotCookie = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Soft Moving Head Cookie",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(u * u + v * v);
                float value = 1f - Mathf.SmoothStep(0.28f, 1f, radius);
                pixels[y * size + x] = new Color(value, value, value, value);
            }
            softSpotCookie.SetPixels(pixels);
            softSpotCookie.Apply(false, true);
            return softSpotCookie;
        }

        private static Texture2D DotGoboCookie()
        {
            if (dotGoboCookie != null) return dotGoboCookie;
            dotGoboCookie = CreateCookie("Seven Dot Gobo", (u, v) =>
            {
                float value = Disc(u, v, 0f, 0f, 0.19f);
                for (int index = 0; index < 6; index++)
                {
                    float angle = index * Mathf.PI / 3f;
                    value = Mathf.Max(value, Disc(u, v, Mathf.Cos(angle) * 0.55f, Mathf.Sin(angle) * 0.55f, 0.16f));
                }
                return value;
            });
            return dotGoboCookie;
        }

        private static Texture2D StarGoboCookie()
        {
            if (starGoboCookie != null) return starGoboCookie;
            starGoboCookie = CreateCookie("Rotating Star Gobo", (u, v) =>
            {
                float radius = Mathf.Sqrt(u * u + v * v);
                if (radius > 0.92f) return 0f;
                float angle = Mathf.Atan2(v, u);
                float starEdge = Mathf.Lerp(0.25f, 0.82f, Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 5f)), 0.7f));
                float star = 1f - Mathf.SmoothStep(starEdge - 0.08f, starEdge + 0.03f, radius);
                float ring = 1f - Mathf.SmoothStep(0.035f, 0.09f, Mathf.Abs(radius - 0.78f));
                return Mathf.Max(star, ring * 0.72f);
            });
            return starGoboCookie;
        }

        private static Texture2D CreateCookie(string name, System.Func<float, float, float> sampler)
        {
            const int size = 96;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float value = Mathf.Clamp01(sampler(u, v));
                pixels[y * size + x] = new Color(value, value, value, value);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float Disc(float u, float v, float centerX, float centerY, float radius)
        {
            float distance = Mathf.Sqrt((u - centerX) * (u - centerX) + (v - centerY) * (v - centerY));
            return 1f - Mathf.SmoothStep(radius * 0.72f, radius, distance);
        }
    }
}
