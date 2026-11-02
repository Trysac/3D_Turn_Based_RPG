using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedRpg.Game
{
    /// <summary>
    /// Builds models from primitive meshes. Parts share one material and get their colour from a
    /// MaterialPropertyBlock, so hit flashes can change the emission of a whole model cheaply.
    /// Nothing gets a collider: picking is done in screen space.
    /// </summary>
    public static class Shapes
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static Mesh cone;
        static MaterialPropertyBlock block;

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;

        public static GameObject Part(Transform parent, Mesh mesh, Material material, Color color, Vector3 position, Vector3 scale,
            Vector3 euler = default, Color emission = default)
        {
            var part = new GameObject(mesh != null ? mesh.name : "Part");
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Paint(renderer, color, emission);
            return part;
        }

        public static void Paint(Renderer renderer, Color color, Color emission)
        {
            block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            block.SetColor(EmissionId, emission);
            renderer.SetPropertyBlock(block);
        }

        /// <summary>A cone of height 1 and radius 0.5 standing on its base at the origin.</summary>
        public static Mesh Cone
        {
            get
            {
                if (cone != null)
                {
                    return cone;
                }
                const int sides = 14;
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides;
                    float a1 = (i + 1) * Mathf.PI * 2f / sides;
                    var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                    var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                    int start = vertices.Count;
                    // Flat-shaded side and base triangles.
                    vertices.Add(p0);
                    vertices.Add(Vector3.up);
                    vertices.Add(p1);
                    vertices.Add(p0);
                    vertices.Add(p1);
                    vertices.Add(Vector3.zero);
                    triangles.AddRange(new[] { start, start + 1, start + 2, start + 3, start + 4, start + 5 });
                }
                cone = new Mesh { name = "Cone" };
                cone.SetVertices(vertices);
                cone.SetTriangles(triangles, 0);
                cone.RecalculateNormals();
                cone.RecalculateBounds();
                return cone;
            }
        }
    }
}
