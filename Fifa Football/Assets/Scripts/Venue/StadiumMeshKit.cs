using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Venue
{
    /// <summary>A small colour atlas: every colour the stadium uses is one cell of a 16x16 point-filtered texture, so the whole venue shares one material.</summary>
    public sealed class StadiumPalette
    {
        const int Side = 16;
        readonly List<Color32> colors = new List<Color32>();
        readonly Dictionary<int, int> map = new Dictionary<int, int>();

        public int Get(Color c)
        {
            Color32 k = c;
            int key = k.r | (k.g << 8) | (k.b << 16);
            if (map.TryGetValue(key, out int i)) return i;
            if (colors.Count >= Side * Side) return 0;
            i = colors.Count; colors.Add(k); map[key] = i;
            return i;
        }

        public Vector2 Uv(int index) { return new Vector2((index % Side + 0.5f) / Side, (index / Side + 0.5f) / Side); }

        public Texture2D Bake()
        {
            var tex = new Texture2D(Side, Side, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "StadiumPalette" };
            var px = new Color32[Side * Side];
            for (int i = 0; i < px.Length; i++) px[i] = i < colors.Count ? colors[i] : new Color32(255, 0, 255, 255);
            tex.SetPixels32(px); tex.Apply(false, true);
            return tex;
        }
    }

    /// <summary>Collects axis-aligned boxes into one mesh. A face mask leaves out faces nobody sees (the back and the underside of a spectator).</summary>
    public sealed class StadiumMeshKit
    {
        public const int PX = 1, NX = 2, PY = 4, NY = 8, PZ = 16, NZ = 32, All = 63;
        readonly StadiumPalette palette;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> norms = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();

        public StadiumMeshKit(StadiumPalette p) { palette = p; }
        public int VertexCount => verts.Count;

        public void Box(Vector3 c, Vector3 size, int color, int mask = All)
        {
            Vector3 h = size * 0.5f;
            Vector2 uv = palette.Uv(color);
            if ((mask & PX) != 0) Quad(c + new Vector3(h.x, -h.y, -h.z), c + new Vector3(h.x, h.y, -h.z), c + new Vector3(h.x, h.y, h.z), c + new Vector3(h.x, -h.y, h.z), Vector3.right, uv);
            if ((mask & NX) != 0) Quad(c + new Vector3(-h.x, -h.y, h.z), c + new Vector3(-h.x, h.y, h.z), c + new Vector3(-h.x, h.y, -h.z), c + new Vector3(-h.x, -h.y, -h.z), Vector3.left, uv);
            if ((mask & PY) != 0) Quad(c + new Vector3(-h.x, h.y, -h.z), c + new Vector3(-h.x, h.y, h.z), c + new Vector3(h.x, h.y, h.z), c + new Vector3(h.x, h.y, -h.z), Vector3.up, uv);
            if ((mask & NY) != 0) Quad(c + new Vector3(-h.x, -h.y, h.z), c + new Vector3(-h.x, -h.y, -h.z), c + new Vector3(h.x, -h.y, -h.z), c + new Vector3(h.x, -h.y, h.z), Vector3.down, uv);
            if ((mask & PZ) != 0) Quad(c + new Vector3(h.x, -h.y, h.z), c + new Vector3(h.x, h.y, h.z), c + new Vector3(-h.x, h.y, h.z), c + new Vector3(-h.x, -h.y, h.z), Vector3.forward, uv);
            if ((mask & NZ) != 0) Quad(c + new Vector3(-h.x, -h.y, -h.z), c + new Vector3(-h.x, h.y, -h.z), c + new Vector3(h.x, h.y, -h.z), c + new Vector3(h.x, -h.y, -h.z), Vector3.back, uv);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 uv)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            for (int k = 0; k < 4; k++) { norms.Add(normal); uvs.Add(uv); }
            // Unity draws clockwise triangles as front faces: flip when this order runs counter-clockwise seen from outside
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f) { tris.Add(i); tris.Add(i + 2); tris.Add(i + 1); tris.Add(i); tris.Add(i + 3); tris.Add(i + 2); }
            else { tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3); }
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateBounds(); m.UploadMeshData(true);
            return m;
        }
    }
}
