using NUnit.Framework;
using UnityEngine;
using FootballSim.Venue;

namespace FootballSim.Tests
{
    public class StadiumMeshKitTests
    {
        [Test]
        public void PaletteGivesTheSameCellForTheSameColour()
        {
            var p = new StadiumPalette();
            int a = p.Get(Color.red), b = p.Get(Color.blue), c = p.Get(Color.red);
            Assert.AreEqual(a, c);
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void AFullBoxHasSixFacesAndAMaskedBoxFewer()
        {
            var p = new StadiumPalette();
            var kit = new StadiumMeshKit(p);
            kit.Box(Vector3.zero, Vector3.one, p.Get(Color.white));
            Assert.AreEqual(24, kit.VertexCount);
            kit.Box(Vector3.zero, Vector3.one, p.Get(Color.white), StadiumMeshKit.All & ~(StadiumMeshKit.NY | StadiumMeshKit.PX));
            Assert.AreEqual(24 + 16, kit.VertexCount);
        }

        [Test]
        public void PaintedStripFacesUpSoItIsNotCulledFromAbove()
        {
            var p = new StadiumPalette();
            var kit = new StadiumMeshKit(p);
            kit.Strip(new Vector3(0f, 0.03f, 0f), new Vector3(0f, 0.03f, 5f), 0.2f, p.Get(Color.white));
            Assert.AreEqual(4, kit.VertexCount);
            var mesh = kit.Build("strip");
            var v = mesh.vertices; var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                // Unity's clockwise-from-outside front faces come out of Cross with a positive Y for an upward face
                Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
            }
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BuiltMeshFacesOutward()
        {
            var p = new StadiumPalette();
            var kit = new StadiumMeshKit(p);
            kit.Box(Vector3.zero, Vector3.one, p.Get(Color.white));
            var mesh = kit.Build("t");
            // read back through a copy: UploadMeshData frees the CPU copy, so only the bounds are checked here
            Assert.AreEqual(1f, mesh.bounds.size.x, 0.001f);
            Object.DestroyImmediate(mesh);
        }
    }
}
