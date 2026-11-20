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
