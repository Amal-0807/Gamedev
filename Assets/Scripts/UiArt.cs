using System;
using UnityEngine;

namespace BlockBlaster
{
    // Interface art is drawn into textures at startup from distance functions, so the project ships no image files.
    public static class UiArt
    {
        public enum Icon { Heart, Pause, Shield, Life, Triple, Slow }
        static readonly Sprite[] icons = new Sprite[6];
        static Sprite panel, outline, gradient;

        public static Sprite Panel => panel != null ? panel : panel = Rounded(0);
        public static Sprite Outline => outline != null ? outline : outline = Rounded(3);
        public static Sprite Get(Icon icon) => icons[(int)icon] != null ? icons[(int)icon] : icons[(int)icon] = Draw(icon);

        // Deep-space backdrop: ink at the bottom rising to a dark blue.
        public static Sprite Gradient
        {
            get
            {
                if (gradient != null) return gradient;
                var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 64; y++)
                    texture.SetPixel(0, y, Color.Lerp(Visuals.Ink, new Color(0.07f, 0.14f, 0.30f), Mathf.Pow(y / 63f, 1.6f)));
                texture.Apply();
                return gradient = Sprite.Create(texture, new Rect(0, 0, 1, 64), Vector2.one * 0.5f);
            }
        }

        // A 9-sliced rounded rectangle: filled when stroke is 0, otherwise an inner outline of that width.
        static Sprite Rounded(float stroke)
        {
            const int size = 64;
            const float radius = 22;
            var texture = Paint(size, p =>
            {
                var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (size * 0.5f - radius - 1);
                float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                return stroke > 0 ? Mathf.Abs(d + stroke * 0.5f) - stroke * 0.5f : d;
            });
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 26);
        }

        static Sprite Draw(Icon icon)
        {
            const int size = 96;
            Func<Vector2, float> shape = icon switch
            {
                Icon.Heart => Heart,
                Icon.Pause => p => Mathf.Min(Box(p, -0.34f, 0, 0.16f, 0.6f), Box(p, 0.34f, 0, 0.16f, 0.6f)),
                Icon.Shield => p => Mathf.Max(Mathf.Max(Mathf.Abs(p.x) - 0.66f, p.y - 0.8f), (Mathf.Abs(p.x) * 1.35f - p.y - 0.9f) * 0.6f),
                Icon.Life => p => Mathf.Min(Box(p, 0, 0, 0.7f, 0.22f), Box(p, 0, 0, 0.22f, 0.7f)),
                Icon.Triple => p => Mathf.Min(Box(p, 0, 0.1f, 0.11f, 0.7f), Mathf.Min(Box(p, -0.55f, -0.1f, 0.11f, 0.5f), Box(p, 0.55f, -0.1f, 0.11f, 0.5f))),
                // Hourglass: two cones meeting at the centre, capped top and bottom.
                _ => p => Mathf.Min(
                    Mathf.Max(Mathf.Abs(p.y) - 0.62f, (Mathf.Abs(p.x) - Mathf.Abs(p.y) * 0.8f - 0.07f) * 0.78f),
                    Mathf.Min(Box(p, 0, 0.72f, 0.62f, 0.1f), Box(p, 0, -0.72f, 0.62f, 0.1f)))
            };
            // Shapes are written in a -1..1 square; convert their distances to pixels for anti-aliasing.
            var texture = Paint(size, p => shape(p / (size * 0.5f)) * size * 0.5f);
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }

        // White texture whose alpha is the shape: distance(p) is in pixels from the centre, negative inside.
        static Texture2D Paint(int size, Func<Vector2, float> distance)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance(new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f))));
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static float Box(Vector2 p, float cx, float cy, float hx, float hy)
            => Mathf.Max(Mathf.Abs(p.x - cx) - hx, Mathf.Abs(p.y - cy) - hy);

        // Inigo Quilez's heart distance, rescaled so the tip and lobes fill the icon square.
        static float Heart(Vector2 p)
        {
            p = new Vector2(Mathf.Abs(p.x) * 0.62f, (p.y + 0.9f) * 0.58f);
            if (p.x + p.y > 1) return ((p - new Vector2(0.25f, 0.75f)).magnitude - 0.35355f) / 0.6f;
            float m = 0.5f * Mathf.Max(p.x + p.y, 0);
            float nearest = Mathf.Min((p - new Vector2(0, 1)).sqrMagnitude, (p - new Vector2(m, m)).sqrMagnitude);
            return Mathf.Sqrt(nearest) * Mathf.Sign(p.x - p.y) / 0.6f;
        }
    }
}
