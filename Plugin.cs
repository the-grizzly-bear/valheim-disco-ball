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
            piece.m_comfort = 2;
            piece.m_comfortGroup = Piece.ComfortGroup.None;
            piece.m_comfortObject = null;

            Fireplace fireplace = item.GetComponent<Fireplace>();
            if (fireplace != null)
            {
                Object.DestroyImmediate(fireplace);
            }

            Vector3 ballPosition = Vector3.up * 0.3f;
            Transform ashLayer = item.transform.Find("ashlayer");
            if (ashLayer != null)
            {
                ballPosition = ashLayer.localPosition;
            }

            foreach (string childName in new[] { "ashlayer", "_enabled", "_enabled_high", "_enabled_low" })
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

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            ball.name = "DiscoBallMesh";
            ball.transform.SetParent(item.transform, false);
            ball.transform.localPosition = ballPosition;
            ball.transform.localScale = Vector3.one * 0.5f;
            ball.GetComponent<MeshRenderer>().sharedMaterial = CreateMirrorMaterial();
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

            PieceManager.Instance.AddPiece(new CustomPiece(item, fixReference: true, new PieceConfig
            {
                Name = "Disco Ball",
                Description = "Reflects the light of a thousand lost dreams. Purely decorative, wildly comforting.",
                PieceTable = "Hammer",
                Category = "Furniture",
                CraftingStation = "piece_workbench",
                Icon = icon,
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Bronze", Amount = 5 },
                    new RequirementConfig { Item = "FineWood", Amount = 4 },
                },
            }));
        }

        private static Material CreateMirrorMaterial()
        {
            Material material;
            GameObject silver = PrefabManager.Instance.GetPrefab("Silver");
            MeshRenderer silverRenderer = silver != null ? silver.GetComponentInChildren<MeshRenderer>() : null;
            material = silverRenderer != null ? new Material(silverRenderer.sharedMaterial) : new Material(Shader.Find("Standard"));

            material.mainTexture = CreateFacetTexture();
            material.mainTextureScale = new Vector2(12f, 6f);
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
                Color.white, new Color(0.55f, 0.6f, 0.65f),
                new Color(0.55f, 0.6f, 0.65f), Color.white,
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
