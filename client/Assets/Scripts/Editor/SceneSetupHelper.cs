#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mahjong.UI;
using Mahjong.Procedural;
using Mahjong.Visual;
using Mahjong.Network;
using Mahjong.AI;
using Mahjong.Audio;

namespace Mahjong.Editor
{
    /// <summary>
    /// SceneSetupHelper: Membantu menyiapkan seluruh hierarki GameObject Mahjong VIP
    /// secara otomatis di dalam Scene Unity sehingga saat di-Play semua komponen langsung aktif!
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSetupHelper
    {
        static SceneSetupHelper()
        {
            EditorApplication.delayCall += EnsureSceneIsSetup;
        }

        [MenuItem("Mahjong VIP/🀄 Setup Lengkap Hierarchy Scene", false, 1)]
        public static void EnsureSceneIsSetup()
        {
            bool sceneModified = false;

            // 1. Setup Main Camera
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 0.48f, -0.46f);
                cam.transform.rotation = Quaternion.Euler(43f, 0, 0);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.09f, 0.13f);
                cam.fieldOfView = 40f;

                if (cam.GetComponent<CameraController>() == null)
                {
                    cam.gameObject.AddComponent<CameraController>();
                    sceneModified = true;
                }
                if (cam.GetComponent<TouchInputHandler>() == null)
                {
                    cam.gameObject.AddComponent<TouchInputHandler>();
                    sceneModified = true;
                }
            }

            // Setup Ambient Light for 3D Tile volume
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.55f);

            // 2. Setup Directional Light
            Light dirLight = Object.FindAnyObjectByType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);
                sceneModified = true;
            }
            if (dirLight != null)
            {
                dirLight.color = new Color(1f, 0.98f, 0.92f);
                dirLight.intensity = 1.35f;
                dirLight.shadows = LightShadows.Soft;
            }

            // 3. Setup EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                sceneModified = true;
            }

            // 4. Setup 3D Table & Compass & Atlas
            if (Object.FindAnyObjectByType<ProceduralTable>() == null)
            {
                GameObject tableObj = new GameObject("[ProceduralTable]", typeof(ProceduralTable));
                sceneModified = true;
            }

            if (Object.FindAnyObjectByType<TableCompass>() == null)
            {
                GameObject compassObj = new GameObject("[TableCompass]", typeof(TableCompass));
                sceneModified = true;
            }

            if (Object.FindAnyObjectByType<ProceduralTileAtlas>() == null)
            {
                GameObject atlasObj = new GameObject("[ProceduralTileAtlas]", typeof(ProceduralTileAtlas));
                sceneModified = true;
            }

            // 5. Setup Network & Logic Managers
            if (Object.FindAnyObjectByType<GameNetworkManager>() == null)
            {
                GameObject netObj = new GameObject("[GameNetworkManager]", typeof(GameNetworkManager));
                sceneModified = true;
            }

            if (Object.FindAnyObjectByType<TableVisualizer>() == null)
            {
                GameObject visObj = new GameObject("[TableVisualizer]", typeof(TableVisualizer));
                sceneModified = true;
            }

            if (Object.FindAnyObjectByType<SinglePlayerAIManager>() == null)
            {
                GameObject aiObj = new GameObject("[SinglePlayerAIManager]", typeof(SinglePlayerAIManager));
                sceneModified = true;
            }

            if (Object.FindAnyObjectByType<ProceduralAudioSynthesizer>() == null)
            {
                GameObject audioObj = new GameObject("[AudioManager]", typeof(ProceduralAudioSynthesizer));
                sceneModified = true;
            }

            // 6. Setup Canvas UI (Landing Page & HUD)
            if (Object.FindAnyObjectByType<ProceduralLandingAndHUD>() == null)
            {
                GameObject canvasObj = new GameObject("[Canvas_UI]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ProceduralLandingAndHUD));
                sceneModified = true;
            }

            if (sceneModified && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[SceneSetupHelper] Semua GameObject & Canvas Mahjong VIP berhasil disiapkan di Scene!");
            }
        }
    }
}
#endif
