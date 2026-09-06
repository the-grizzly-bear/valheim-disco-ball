using System.Collections.Generic;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace DiscoBall
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.jotunn.jotunn")]
    public class DiscoBallPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "mishka.valheim.discoball";
        public const string PluginName = "DiscoBall";
        public const string PluginVersion = "1.0.0";

        private const float ChainScale = 0.5f;
        private const float BallRadius = 0.35f;
        private const int ChainSegmentCount = 4;

        private static readonly Color[] LightColors =
        {
            Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan,
        };

        private void Awake()
        {
            PrefabManager.OnVanillaPrefabsAvailable += AddDiscoBall;
        }

        private void AddDiscoBall()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddDiscoBall;

            GameObject item = PrefabManager.Instance.CreateClonedPrefab("piece_discoball", "piece_brazierceiling01");

            Piece piece = item.GetComponent<Piece>();
            // piece.m_comfort = 2;
            piece.m_comfort = 0;

            Fireplace fireplace = item.GetComponent<Fireplace>();
            if (fireplace != null)
            {
                Object.DestroyImmediate(fireplace);
            }

            foreach (string childName in new[] { "ashlayer", "_enabled", "_enabled_high", "_enabled_low", "New" })
            {
                Transform child = item.transform.Find(childName);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
            foreach (EffectArea area in item.GetComponentsInChildren<EffectArea>(true))
            {
                Object.DestroyImmediate(area);
            }

            Vector3 ballPosition = Vector3.up * (BallRadius * 0.5f);
            float ballTop = ballPosition.y + BallRadius;
            float chainSegmentHeight = GetChainSegmentHeight();
            AddChainSegment(item.transform, 0, ballTop, chainSegmentHeight);

            GameObject ball = new GameObject("DiscoBallMesh");
            ball.transform.SetParent(item.transform, false);
            ball.transform.localPosition = ballPosition;
            ball.transform.localScale = Vector3.one * BallRadius;
            Mesh facetedSphere = CreateLowPolySphere(rings: 6, segments: 9, radius: 1f);
            MakeFlatShaded(facetedSphere);
            ball.AddComponent<MeshFilter>().mesh = facetedSphere;
            ball.AddComponent<MeshRenderer>().sharedMaterial = CreateMirrorMaterial();
            ball.AddComponent<SphereCollider>().radius = 1f;
            ball.AddComponent<DiscoSpin>();

            GameObject lightGo = new GameObject("DiscoLight");
            lightGo.transform.SetParent(ball.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 1.5f;
            light.color = Color.white;
            lightGo.AddComponent<DiscoLightCycle>();

            LODGroup lodGroup = item.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                Object.DestroyImmediate(lodGroup);
            }

            Sprite icon = RenderManager.Instance.Render(item);

            for (int i = 1; i < ChainSegmentCount; i++)
            {
                AddChainSegment(item.transform, i, ballTop, chainSegmentHeight);
            }

            PieceManager.Instance.AddPiece(new CustomPiece(item, fixReference: true, new PieceConfig
            {
                Name = "Disco Ball",
                Description = "Reflects the light of a thousand lost dreams. Purely decorative.",
                PieceTable = "Hammer",
                Category = "Furniture",
                CraftingStation = "piece_workbench",
                Icon = icon,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Bronze", Amount = 5 },
                    new RequirementConfig { Item = "FineWood", Amount = 4 },
                    new RequirementConfig { Item = "Chain", Amount = 1 },
                },
            }));
        }

        private static Mesh CreateLowPolySphere(int rings, int segments, float radius)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float phi = v * Mathf.PI;
                for (int s = 0; s <= segments; s++)
                {
                    float u = (float)s / segments;
                    float theta = u * Mathf.PI * 2f;
                    float x = Mathf.Sin(phi) * Mathf.Cos(theta);
                    float y = Mathf.Cos(phi);
                    float z = Mathf.Sin(phi) * Mathf.Sin(theta);
                    vertices.Add(new Vector3(x, y, z) * radius);
                    uvs.Add(new Vector2(u, v));
                }
            }

            int rowLength = segments + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * rowLength + s;
                    int b = a + rowLength;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(b);
                    triangles.Add(b + 1);
                }
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void MakeFlatShaded(Mesh mesh)
        {
            Vector3[] oldVertices = mesh.vertices;
            Vector2[] oldUv = mesh.uv;
            int[] triangles = mesh.triangles;

            Vector3[] newVertices = new Vector3[triangles.Length];
            Vector2[] newUv = new Vector2[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                newVertices[i] = oldVertices[triangles[i]];
                newUv[i] = oldUv[triangles[i]];
                triangles[i] = i;
            }

            mesh.vertices = newVertices;
            mesh.uv = newUv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }

        private static float GetChainSegmentHeight()
        {
            GameObject chainSource = PrefabManager.Instance.GetPrefab("Chain");
            Transform sourceModel = chainSource != null ? chainSource.transform.Find("model") : null;
            MeshFilter sourceFilter = sourceModel != null ? sourceModel.GetComponent<MeshFilter>() : null;
            return sourceFilter != null ? sourceFilter.sharedMesh.bounds.size.y * ChainScale : 0f;
        }

        private static void AddChainSegment(Transform parent, int index, float baseHeight, float segmentHeight)
        {
            GameObject chainSource = PrefabManager.Instance.GetPrefab("Chain");
            Transform sourceModel = chainSource != null ? chainSource.transform.Find("model") : null;
            if (sourceModel == null)
            {
                return;
            }

            GameObject chainVisual = Object.Instantiate(sourceModel.gameObject, parent, false);
            chainVisual.name = "DiscoBallChain" + index;
            Collider chainCollider = chainVisual.GetComponent<Collider>();
            if (chainCollider != null)
            {
                Object.DestroyImmediate(chainCollider);
            }
            LODGroup chainLod = chainVisual.GetComponent<LODGroup>();
            if (chainLod != null)
            {
                Object.DestroyImmediate(chainLod);
            }

            chainVisual.transform.localPosition = Vector3.up * (baseHeight + segmentHeight * (index + 0.5f));
            chainVisual.transform.localRotation = Quaternion.identity;
            chainVisual.transform.localScale = Vector3.one * ChainScale;
        }

        private static Material CreateMirrorMaterial()
        {
            Material material;
            GameObject silver = PrefabManager.Instance.GetPrefab("Silver");
            MeshRenderer silverRenderer = silver != null ? silver.GetComponentInChildren<MeshRenderer>() : null;
            material = silverRenderer != null ? new Material(silverRenderer.sharedMaterial) : new Material(Shader.Find("Sprites/Default"));

            material.color = new Color(0.85f, 0.87f, 0.9f);
            material.mainTexture = CreateFacetTexture();
            material.mainTextureScale = new Vector2(6f, 3f);
            return material;
        }

        private static Texture2D CreateFacetTexture()
        {
            const int size = 2;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Point;
            texture.SetPixels(new[]
            {
                Color.white, new Color(0.75f, 0.8f, 0.85f),
                new Color(0.75f, 0.8f, 0.85f), Color.white,
            });
            texture.Apply();
            return texture;
        }

        private class DiscoSpin : MonoBehaviour
        {
            private void Update()
            {
                transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
            }
        }

        private class DiscoLightCycle : MonoBehaviour
        {
            private Light light;
            private float timer;
            private int colorIndex;

            private void Awake()
            {
                light = GetComponent<Light>();
            }

            private void Update()
            {
                if (light == null)
                {
                    return;
                }
                timer += Time.deltaTime;
                if (timer >= 0.5f)
                {
                    timer = 0f;
                    colorIndex = (colorIndex + 1) % LightColors.Length;
                    light.color = LightColors[colorIndex];
                }
            }
        }
    }
}
