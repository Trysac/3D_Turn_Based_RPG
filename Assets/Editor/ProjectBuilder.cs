using System;
using System.IO;
using TurnBasedRpg.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace TurnBasedRpg.EditorTools
{
    /// <summary>
    /// Generates the materials, art asset, UI panel settings, scene, app icon and player settings
    /// from code, and builds the players, so every binary asset can be recreated from text.
    /// <code>
    /// Unity -batchmode -quit -projectPath . -executeMethod TurnBasedRpg.EditorTools.ProjectBuilder.SetUpProject
    /// Unity -batchmode -quit -projectPath . -executeMethod TurnBasedRpg.EditorTools.ProjectBuilder.BuildWindows -buildOutput &lt;folder&gt;
    /// Unity -batchmode -quit -projectPath . -executeMethod TurnBasedRpg.EditorTools.ProjectBuilder.BuildWebGL -buildOutput &lt;folder&gt;
    /// Unity -batchmode -projectPath . -executeMethod TurnBasedRpg.EditorTools.ProjectBuilder.CaptureScreenshots -screenshot &lt;folder&gt;
    /// </code>
    /// </summary>
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Scenes/Battle.unity";
        const string ArtFolder = "Assets/Art";
        const string MaterialFolder = "Assets/Art/Materials";
        const string ArtPath = "Assets/Art/GameArt.asset";
        const string SoftDotPath = "Assets/Art/SoftDot.png";
        const string IconPath = "Assets/Art/AppIcon.png";
        const string AudioFolder = "Assets/Audio";
        const string ThemePath = "Assets/UI/HudTheme.tss";
        const string PanelSettingsPath = "Assets/UI/HudPanelSettings.asset";

        [MenuItem("Embervale/Regenerate Art, Scene and Player Settings")]
        public static void SetUpProject()
        {
            GameArt art = CreateArt();
            PanelSettings panel = CreatePanelSettings();
            CreateScene(art, panel);
            ConfigurePlayer(CreateIcon());
            AssetDatabase.SaveAssets();
            Debug.Log("Embervale project set up.");
        }

        /// <summary>Enters play mode and lets ScreenshotScenarios capture the README screenshots, then exits.</summary>
        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Embervale.exe");

        public static void BuildWebGL() => Build(BuildTarget.WebGL, string.Empty);

        // ---- Art ----

        static GameArt CreateArt()
        {
            Directory.CreateDirectory(MaterialFolder);
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<GameArt>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }

            art.Lit = Lit("Lit");
            art.Ring = Transparent("Ring");
            Texture2D dot = CreateSoftDot();
            art.ParticleAdditive = Particle("ParticleAdditive", dot, additive: true);
            art.ParticleAlpha = Particle("ParticleAlpha", dot, additive: false);

            art.Cube = PrimitiveMesh(PrimitiveType.Cube);
            art.Sphere = PrimitiveMesh(PrimitiveType.Sphere);
            art.Cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            art.Capsule = PrimitiveMesh(PrimitiveType.Capsule);
            art.Quad = PrimitiveMesh(PrimitiveType.Quad);

            art.RoadTheme = Clip("road_theme", music: true);
            art.BattleTheme = Clip("battle_theme", music: true);
            art.BossTheme = Clip("boss_theme", music: true);
            art.Move = Clip("move");
            art.Confirm = Clip("confirm");
            art.Cancel = Clip("cancel");
            art.Slash = Clip("slash");
            art.Critical = Clip("critical");
            art.Fire = Clip("fire");
            art.Heal = Clip("heal");
            art.Buff = Clip("buff");
            art.Curse = Clip("curse");
            art.Stun = Clip("stun");
            art.Guard = Clip("guard");
            art.Item = Clip("item");
            art.UnitDown = Clip("unit_down");
            art.YourTurn = Clip("your_turn");
            art.LevelUp = Clip("level_up");
            art.Victory = Clip("victory");
            art.Defeat = Clip("defeat");
            art.Roar = Clip("roar");

            EditorUtility.SetDirty(art);
            return art;
        }

        static Mesh PrimitiveMesh(PrimitiveType type)
        {
            GameObject probe = GameObject.CreatePrimitive(type);
            Mesh mesh = probe.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(probe);
            return mesh;
        }

        /// <summary>One Standard material for every model part; colour and emission come from property blocks.</summary>
        static Material Lit(string name)
        {
            Material material = MaterialAt(name, Shader.Find("Standard"));
            material.color = Color.white;
            material.SetFloat("_Glossiness", 0.25f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", Color.black);
            material.enableInstancing = false;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Transparent(string name)
        {
            Material material = MaterialAt(name, Shader.Find("Sprites/Default"));
            material.color = Color.white;
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Particle(string name, Texture2D texture, bool additive)
        {
            Material material = MaterialAt(name, Shader.Find("Legacy Shaders/Particles/" + (additive ? "Additive" : "Alpha Blended")));
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material MaterialAt(string name, Shader shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            return material;
        }

        static Texture2D CreateSoftDot()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha * (3f - 2f * alpha)));
                }
            }
            texture.Apply();
            File.WriteAllBytes(SoftDotPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SoftDotPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SoftDotPath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(SoftDotPath);
        }

        static AudioClip Clip(string name, bool music = false)
        {
            string path = $"{AudioFolder}/{name}.ogg";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing {path}: run python tools/audio/build.py first.");
                return null;
            }
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---- UI ----

        static PanelSettings CreatePanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        // ---- Scene ----

        static void CreateScene(GameArt art, PanelSettings panel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Shapes.Hex("#9ccff0");
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 80f;
            cameraObject.AddComponent<AudioListener>();
            CameraRig rig = cameraObject.AddComponent<CameraRig>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 5.4f, -12.5f), Quaternion.Euler(19f, 0f, 0f));

            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Shapes.Hex("#fff1d6");
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Shapes.Hex("#8aa0b8");
            // Fog is set per stage at run time; keeping it on here keeps the fog shader variants in builds.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 42f;

            var arenaObject = new GameObject("Arena");
            Arena arena = arenaObject.AddComponent<Arena>();

            var ui = new GameObject("HUD");
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = panel;
            Hud hud = ui.AddComponent<Hud>();

            var audioObject = new GameObject("Audio");
            AudioDirector audio = audioObject.AddComponent<AudioDirector>();

            var game = new GameObject("Game");
            var director = game.AddComponent<GameDirector>();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("art").objectReferenceValue = art;
            serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.FindProperty("rig").objectReferenceValue = rig;
            serialized.FindProperty("arena").objectReferenceValue = arena;
            serialized.FindProperty("sound").objectReferenceValue = audio;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer(Texture2D icon)
        {
            PlayerSettings.companyName = "Trysac";
            PlayerSettings.productName = "Embervale";
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
            var standaloneIcons = new Texture2D[sizes.Length];
            Array.Fill(standaloneIcons, icon);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, standaloneIcons, IconKind.Any);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            // Static hosts such as GitHub Pages do not send Content-Encoding for .br/.gz files.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
        }

        // ---- Icon: an ember flame over a sword on a dark tile ----

        static Texture2D CreateIcon()
        {
            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = IconPixel(x + 0.5f - size / 2f, y + 0.5f - size / 2f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        }

        static Color IconPixel(float x, float y)
        {
            const float aa = 1.5f;
            Color color = Shapes.Hex("#141a2a");
            float tile = RoundedBox(x, y, 470f, 470f, 170f);
            color.a = Coverage(-tile, aa);
            color = Over(color, Shapes.Hex("#ff9a3c"), Coverage(12f - Mathf.Abs(tile + 22f), aa));

            // Flame: a circle with a tapered tip, inner core lighter.
            float flame = Mathf.Min(Length(x, y + 70f) - 230f, Tip(x, y + 70f, 230f, 190f));
            color = Over(color, Shapes.Hex("#ff6a1f"), Coverage(-flame, aa));
            float core = Mathf.Min(Length(x, y + 120f) - 130f, Tip(x, y + 120f, 130f, 110f));
            color = Over(color, Shapes.Hex("#ffd36b"), Coverage(-core, aa));

            // Sword: blade, guard and grip, pointing up through the flame.
            float sword = RoundedBox(x, y - 60f, 34f, 300f, 14f);
            sword = Mathf.Min(sword, RoundedBox(x, y + 260f, 140f, 26f, 12f));
            sword = Mathf.Min(sword, RoundedBox(x, y + 330f, 26f, 55f, 10f));
            sword = Mathf.Min(sword, Length(x, y + 395f) - 34f);
            color = Over(color, Shapes.Hex("#0b0f1a"), Coverage(-(sword - 16f), aa));
            color = Over(color, Shapes.Hex("#e6ecf4"), Coverage(-sword, aa));
            return color;
        }

        /// <summary>Signed distance-ish field of a point above a circle: a triangle from the circle's sides to a tip.</summary>
        static float Tip(float x, float y, float radius, float height)
        {
            if (y < 0f || y > radius + height)
            {
                return float.MaxValue;
            }
            float halfWidth = radius * (1f - Mathf.Clamp01(y / (radius + height)));
            return Mathf.Abs(x) - halfWidth;
        }

        static float Length(float x, float y) => Mathf.Sqrt(x * x + y * y);

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static float Coverage(float inside, float width) => Mathf.Clamp01(inside / width + 0.5f);

        static Color Over(Color below, Color above, float amount)
        {
            Color mixed = Color.Lerp(below, above, amount);
            mixed.a = below.a;
            return mixed;
        }

        // ---- Builds ----

        static void Build(BuildTarget target, string fileName)
        {
            string folder = Argument("-buildOutput");
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("Pass -buildOutput <folder> outside the repository.");
            }
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = string.IsNullOrEmpty(fileName) ? folder : Path.Combine(folder, fileName),
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024f):0.0} MB, {summary.totalErrors} errors.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
