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

            foreach (ParticleSystem ps in item.GetComponentsInChildren<ParticleSystem>(true))
            {
                Object.DestroyImmediate(ps.gameObject);
            }
            foreach (EffectArea area in item.GetComponentsInChildren<EffectArea>(true))
            {
                Object.DestroyImmediate(area);
            }

            Transform mount = item.transform;
            foreach (MeshRenderer renderer in item.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform != item.transform)
                {
                    mount = renderer.transform.parent != null ? renderer.transform.parent : item.transform;
                    Object.DestroyImmediate(renderer.gameObject);
                }
            }

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            ball.name = "DiscoBallMesh";
            ball.transform.SetParent(mount, false);
            ball.transform.localPosition = Vector3.zero;
            ball.transform.localScale = Vector3.one * 0.5f;
            Material ballMaterial = GetMirrorMaterial();
            if (ballMaterial != null)
            {
                ball.GetComponent<MeshRenderer>().sharedMaterial = ballMaterial;
            }
            ball.AddComponent<DiscoSpin>();

            foreach (Light light in item.GetComponentsInChildren<Light>(true))
            {
                light.color = Color.white;
                light.range = Mathf.Max(light.range, 6f);
                light.intensity = Mathf.Max(light.intensity, 1.2f);
                light.gameObject.AddComponent<DiscoLightCycle>();
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

        private static Material GetMirrorMaterial()
        {
            GameObject silver = PrefabManager.Instance.GetPrefab("Silver");
            MeshRenderer renderer = silver != null ? silver.GetComponentInChildren<MeshRenderer>() : null;
            return renderer != null ? renderer.sharedMaterial : null;
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
