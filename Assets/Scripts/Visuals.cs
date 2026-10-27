using UnityEngine;

namespace BlockBlaster
{
    public static class Visuals
    {
        public static readonly Color Ink = new Color(0.025f, 0.035f, 0.08f);
        public static readonly Color Navy = new Color(0.06f, 0.10f, 0.18f);
        public static readonly Color Cyan = new Color(0.20f, 0.94f, 0.91f);
        public static readonly Color Coral = new Color(1f, 0.36f, 0.40f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.29f);
        public static readonly Color Muted = new Color(0.57f, 0.68f, 0.78f);
        public static readonly Color[] ShapeColors =
        {
            Cyan, Gold, new Color(0.77f, 0.47f, 1f), new Color(0.42f, 0.90f, 0.52f),
            Coral, new Color(0.35f, 0.58f, 1f), new Color(1f, 0.57f, 0.28f)
        };
        static readonly System.Collections.Generic.Dictionary<(Color, float), Material> Materials = new();
        static readonly Material[] IconMaterials = new Material[6];

        // edge = 1 adds the shader's bright rim to each face.
        public static Material Material(Color color, float edge = 0)
        {
            if (Materials.TryGetValue((color, edge), out var material)) return material;
            material = new Material(Resources.Load<Shader>("BlockSurface")) { color = color };
            material.SetFloat("_Edge", edge);
            Materials.Add((color, edge), material);
            return material;
        }

        // The starfighter's four paint slots, left to right in a 4x1 texture: hull, trim, canopy, engine metal.
        public static Material ShipMaterial()
        {
            var palette = new Texture2D(4, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            palette.SetPixels(new[] { new Color(0.86f, 0.90f, 0.95f), Cyan, new Color(0.08f, 0.12f, 0.20f), new Color(0.50f, 0.58f, 0.68f) });
            palette.Apply();
            return new Material(Resources.Load<Shader>("BlockSurface")) { mainTexture = palette };
        }

        public static Material IconMaterial(UiArt.Icon icon)
        {
            int i = (int)icon;
            if (IconMaterials[i] == null)
                IconMaterials[i] = new Material(Resources.Load<Shader>("BlockSurface")) { mainTexture = UiArt.Get(icon).texture };
            return IconMaterials[i];
        }

        public static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Color color, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static void BuildArena(Transform arena)
        {
            Cube("Deck", arena, new Vector3(0, -0.008f, 0.40f), new Vector3(0.60f, 0.012f, 0.80f), Navy);
            for (int i = 0; i <= 12; i++)
                Cube("Grid X", arena, new Vector3(-0.30f + i * 0.05f, 0, 0.40f), new Vector3(0.0008f, 0.0005f, 0.80f), new Color(0.10f, 0.18f, 0.25f));
            for (int i = 0; i <= 16; i++)
                Cube("Grid Z", arena, new Vector3(0, 0, i * 0.05f), new Vector3(0.60f, 0.0005f, 0.0008f), new Color(0.10f, 0.18f, 0.25f));
            Cube("Left Rail", arena, new Vector3(-0.307f, 0.005f, 0.40f), new Vector3(0.008f, 0.014f, 0.81f), Cyan);
            Cube("Right Rail", arena, new Vector3(0.307f, 0.005f, 0.40f), new Vector3(0.008f, 0.014f, 0.81f), Cyan);
            Cube("Spawn Rail", arena, new Vector3(0, 0.005f, 0.803f), new Vector3(0.62f, 0.014f, 0.008f), Muted);
            Cube("Danger Line", arena, new Vector3(0, 0.002f, 0), new Vector3(0.61f, 0.004f, 0.006f), Coral);
        }
    }
}
