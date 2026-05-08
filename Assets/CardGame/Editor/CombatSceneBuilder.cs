using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LichLord.CardGame.UI;

namespace LichLord.CardGame.Editor
{
    /// <summary>
    /// Builds the CombatScene from scratch and creates the CardPrefab.
    /// Run via the menu: CardGame ▶ Build Combat Scene
    /// </summary>
    public static class CombatSceneBuilder
    {
        private const string ScenePath  = "Assets/Scenes/CombatScene.unity";
        private const string PrefabPath = "Assets/CardGame/Prefabs/CardPrefab.prefab";

        // Colours
        private static readonly Color BgColor     = new Color(0.08f, 0.08f, 0.10f);
        private static readonly Color PanelColor  = new Color(0.13f, 0.14f, 0.18f);
        private static readonly Color HeaderColor = new Color(0.20f, 0.22f, 0.28f);
        private static readonly Color BtnGreen    = new Color(0.15f, 0.50f, 0.25f);
        private static readonly Color TextWhite   = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color TextDim     = new Color(0.70f, 0.72f, 0.75f);

        [MenuItem("CardGame/Build Combat Scene")]
        public static void Build()
        {
            // ── 1. Create card prefab first (we need the asset before the scene) ──
            CardView cardPrefab = CreateCardPrefab();

            // ── 2. Create a new empty scene ───────────────────────────────────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 3. Camera ─────────────────────────────────────────────────────────
            GameObject cameraGo = new GameObject("Main Camera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags  = CameraClearFlags.SolidColor;
            cam.backgroundColor = BgColor;
            cam.cullingMask = 0; // only UI; nothing 3D in this scene
            cameraGo.AddComponent<AudioListener>();
            cameraGo.tag = "MainCamera";

            // ── 4. EventSystem ────────────────────────────────────────────────────
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // ── 5. Canvas ─────────────────────────────────────────────────────────
            GameObject canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1280, 720);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-canvas background image
            var bg = MakeImage(canvasGo, "Background", BgColor);
            StretchFull(bg.GetComponent<RectTransform>());

            // ── 6. Layout ─────────────────────────────────────────────────────────
            //
            //  Canvas (1280 × 720)
            //  ┌────────────────────────────────────────────┐
            //  │  EnemyPanel          (top, h=220)          │
            //  ├─────────────────────┬──────────────────────┤
            //  │  CombatLog (h=160)  │  PlayerDicePanel     │
            //  ├─────────────────────┴──────────────────────┤
            //  │  PlayerStatsPanel   (h=60)                 │
            //  ├────────────────────────────────────────────┤
            //  │  HandPanel          (h=180)                │
            //  └──────────────────────────────────────────EndTurnBtn┘

            // ── Enemy panel ───────────────────────────────────────────────────────
            GameObject enemyPanel = MakePanel(canvasGo, "EnemyPanel", PanelColor);
            SetAnchored(enemyPanel.GetComponent<RectTransform>(),
                new Vector2(0, 0.695f), new Vector2(1, 1), new Vector2(0, 0));

            MakeLabel(enemyPanel, "EnemyTitle", "ENEMY", 13, TextDim, FontStyles.Bold,
                new Vector2(0, 0.8f), new Vector2(1, 1), new Vector2(0, 0));

            // Enemy stats
            GameObject enemyStatsGo = MakePanel(enemyPanel, "EnemyStats", Color.clear);
            SetAnchored(enemyStatsGo.GetComponent<RectTransform>(),
                new Vector2(0.01f, 0.35f), new Vector2(0.55f, 0.8f), new Vector2(0, 0));
            var enemyStatsView = enemyStatsGo.AddComponent<CombatantStatsView>();
            enemyStatsView.nameText  = MakeLabel(enemyStatsGo, "EnemyName",  "Enemy Name", 17, TextWhite, FontStyles.Bold,   new Vector2(0, 0.7f), new Vector2(1, 1),    Vector2.zero);
            enemyStatsView.hpText    = MakeLabel(enemyStatsGo, "EnemyHP",    "HP: 0/0",    14, TextWhite, FontStyles.Normal, new Vector2(0, 0.35f), new Vector2(0.5f, 0.7f), Vector2.zero);
            enemyStatsView.blockText = MakeLabel(enemyStatsGo, "EnemyBlock", "Block: 0",   14, TextWhite, FontStyles.Normal, new Vector2(0.5f, 0.35f), new Vector2(1, 0.7f), Vector2.zero);
            enemyStatsView.statusText = MakeLabel(enemyStatsGo, "EnemyStatus", "",         12, new Color(1f, 0.85f, 0.3f), FontStyles.Normal, new Vector2(0, 0f), new Vector2(1, 0.35f), Vector2.zero);

            // Enemy dice
            GameObject enemyDiceGo = MakePanel(enemyPanel, "EnemyDicePanel", Color.clear);
            SetAnchored(enemyDiceGo.GetComponent<RectTransform>(),
                new Vector2(0.01f, 0.0f), new Vector2(1, 0.35f), new Vector2(0, 0));
            var enemyDiceView = enemyDiceGo.AddComponent<DiceRowView>();
            enemyDiceView.diceLabel = MakeLabel(enemyDiceGo, "EnemyDice", "[ — ]", 18,
                new Color(0.8f, 0.9f, 1f), FontStyles.Bold,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero);
            enemyDiceView.diceLabel.alignment = TextAlignmentOptions.MidlineLeft;

            // Enemy passive info
            MakeLabel(enemyPanel, "EnemyPassiveLabel", "PASSIVES", 11, TextDim, FontStyles.Bold,
                new Vector2(0.56f, 0.0f), new Vector2(1f, 1.0f), new Vector2(0, 0))
                .alignment = TextAlignmentOptions.TopLeft;

            // ── Middle row: CombatLog + PlayerDice ────────────────────────────────
            float midTop    = 0.695f;
            float midBottom = 0.375f;

            // Combat log (left 55%)
            GameObject logPanel = MakePanel(canvasGo, "CombatLogPanel", PanelColor);
            SetAnchored(logPanel.GetComponent<RectTransform>(),
                new Vector2(0, midBottom), new Vector2(0.55f, midTop), new Vector2(0, 0));

            MakeLabel(logPanel, "LogTitle", "COMBAT LOG", 11, TextDim, FontStyles.Bold,
                new Vector2(0, 0.88f), new Vector2(1, 1), Vector2.zero);

            GameObject scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(logPanel.transform, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            var scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0, 0, 0, 0.3f);
            SetAnchored(scrollGo.GetComponent<RectTransform>(),
                new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.87f), Vector2.zero);

            GameObject logContent = new GameObject("LogContent");
            logContent.transform.SetParent(scrollGo.transform, false);
            var contentRect = logContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentFitter = logContent.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var logTxt = logContent.AddComponent<TextMeshProUGUI>();
            logTxt.fontSize  = 12;
            logTxt.color     = TextWhite;
            logTxt.alignment = TextAlignmentOptions.BottomLeft;
            logTxt.text      = "";
            scrollRect.content = contentRect;

            var combatLogView = logPanel.AddComponent<CombatLogView>();
            combatLogView.logText    = logTxt;
            combatLogView.scrollRect = scrollRect;

            // Player dice (right 45%)
            GameObject playerDicePanel = MakePanel(canvasGo, "PlayerDicePanel", PanelColor);
            SetAnchored(playerDicePanel.GetComponent<RectTransform>(),
                new Vector2(0.55f, midBottom), new Vector2(1, midTop), new Vector2(0, 0));

            MakeLabel(playerDicePanel, "PlayerDiceTitle", "YOUR DICE", 11, TextDim, FontStyles.Bold,
                new Vector2(0, 0.8f), new Vector2(1, 1), Vector2.zero);

            var playerDiceView = playerDicePanel.AddComponent<DiceRowView>();
            playerDiceView.diceLabel = MakeLabel(playerDicePanel, "PlayerDice", "[ — ]", 22,
                new Color(0.6f, 1f, 0.6f), FontStyles.Bold,
                new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.8f), Vector2.zero);
            playerDiceView.diceLabel.alignment = TextAlignmentOptions.Midline;

            // ── Player stats strip ────────────────────────────────────────────────
            float statsTop    = midBottom;
            float statsBottom = 0.29f;

            GameObject playerStatsPanel = MakePanel(canvasGo, "PlayerStatsPanel", HeaderColor);
            SetAnchored(playerStatsPanel.GetComponent<RectTransform>(),
                new Vector2(0, statsBottom), new Vector2(0.80f, statsTop), Vector2.zero);

            var playerStatsView = playerStatsPanel.AddComponent<CombatantStatsView>();
            playerStatsView.hpText     = MakeLabel(playerStatsPanel, "PlayerHP",     "HP: 30/30",   14, TextWhite, FontStyles.Bold,   new Vector2(0, 0), new Vector2(0.25f, 1), Vector2.zero);
            playerStatsView.blockText  = MakeLabel(playerStatsPanel, "PlayerBlock",  "Block: 0",    14, TextWhite, FontStyles.Normal, new Vector2(0.25f, 0), new Vector2(0.50f, 1), Vector2.zero);
            playerStatsView.energyText = MakeLabel(playerStatsPanel, "PlayerEnergy", "Energy: 3/3", 14, new Color(0.4f, 0.8f, 1f), FontStyles.Normal, new Vector2(0.50f, 0), new Vector2(0.75f, 1), Vector2.zero);
            playerStatsView.statusText = MakeLabel(playerStatsPanel, "PlayerStatus", "",            12, new Color(1f, 0.85f, 0.3f), FontStyles.Normal, new Vector2(0.75f, 0), new Vector2(1f, 1), Vector2.zero);

            // End-turn button (right 20% of stats strip)
            GameObject endTurnGo  = new GameObject("EndTurnButton");
            endTurnGo.transform.SetParent(canvasGo.transform, false);
            var endTurnImg = endTurnGo.AddComponent<Image>();
            endTurnImg.color = BtnGreen;
            var endTurnBtn = endTurnGo.AddComponent<Button>();
            var endTurnNav = endTurnBtn.navigation;
            endTurnNav.mode = Navigation.Mode.None;
            endTurnBtn.navigation = endTurnNav;
            SetAnchored(endTurnGo.GetComponent<RectTransform>(),
                new Vector2(0.80f, statsBottom), new Vector2(1f, statsTop), Vector2.zero);
            MakeLabel(endTurnGo, "EndTurnText", "END TURN", 16, Color.white, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero)
                .alignment = TextAlignmentOptions.Center;

            // ── Hand panel ────────────────────────────────────────────────────────
            GameObject handPanel = MakePanel(canvasGo, "HandPanel", PanelColor);
            SetAnchored(handPanel.GetComponent<RectTransform>(),
                new Vector2(0, 0), new Vector2(1, 0.29f), Vector2.zero);

            MakeLabel(handPanel, "HandTitle", "HAND", 11, TextDim, FontStyles.Bold,
                new Vector2(0.01f, 0.82f), new Vector2(0.15f, 1f), Vector2.zero);

            // Card container inside hand panel
            GameObject cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(handPanel.transform, false);
            SetAnchored(cardContainer.GetComponent<RectTransform>(),
                new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.82f), Vector2.zero);

            var hlg = cardContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 8;
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(4, 4, 4, 4);

            var handView = handPanel.AddComponent<HandView>();
            handView.cardPrefab     = cardPrefab;
            handView.cardContainer  = cardContainer.transform;

            // ── CombatBootstrapper ─────────────────────────────────────────────────
            GameObject controllerGo = new GameObject("CombatController");
            var bootstrapper = controllerGo.AddComponent<CombatBootstrapper>();
            bootstrapper.playerStatsView = playerStatsView;
            bootstrapper.enemyStatsView  = enemyStatsView;
            bootstrapper.playerDiceView  = playerDiceView;
            bootstrapper.enemyDiceView   = enemyDiceView;
            bootstrapper.handView        = handView;
            bootstrapper.combatLog       = combatLogView;
            bootstrapper.endTurnButton   = endTurnBtn;

            // ── 7. Save scene ──────────────────────────────────────────────────────
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            // ── 8. Add to Build Settings ───────────────────────────────────────────
            AddSceneToBuildSettings(ScenePath);

            AssetDatabase.Refresh();
            Debug.Log($"[CombatSceneBuilder] Scene saved to {ScenePath}");
            Debug.Log($"[CombatSceneBuilder] Card prefab saved to {PrefabPath}");
        }

        // ── CardPrefab creation ────────────────────────────────────────────────────

        private static CardView CreateCardPrefab()
        {
            Directory.CreateDirectory("Assets/CardGame/Prefabs");

            // Root: Button + Image
            GameObject root = new GameObject("CardPrefab");
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.18f, 0.22f, 0.32f);

            var btn = root.AddComponent<Button>();
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            var rootRT = root.GetComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(160, 200);

            // Persistent badge (top-left, shown only for persistent cards)
            // — omitted for prototype; description will mention Persistent

            // Cost badge (top-right)
            GameObject costGo = new GameObject("CostBadge");
            costGo.transform.SetParent(root.transform, false);
            var costBadgeImg = costGo.AddComponent<Image>();
            costBadgeImg.color = new Color(0.1f, 0.3f, 0.6f);
            var costRT = costGo.GetComponent<RectTransform>();
            costRT.anchorMin = new Vector2(0.78f, 0.82f);
            costRT.anchorMax = new Vector2(1f,    1f);
            costRT.offsetMin = new Vector2(2, 2);
            costRT.offsetMax = new Vector2(-2, -2);

            var costTxt = costGo.AddComponent<TextMeshProUGUI>();
            costTxt.fontSize  = 18;
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.color     = Color.white;
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.text      = "1";

            // Name text (upper strip)
            GameObject nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(root.transform, false);
            var nameRT = nameGo.GetComponent<RectTransform>();
            nameRT.anchorMin = Vector2.zero;
            nameRT.anchorMax = new Vector2(0.78f, 1f);
            nameRT.offsetMin = new Vector2(4, -44);
            nameRT.offsetMax = new Vector2(-2, -4);
            var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
            nameTxt.fontSize  = 13;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.color     = Color.white;
            nameTxt.alignment = TextAlignmentOptions.TopLeft;
            nameTxt.text      = "Card Name";
            nameTxt.enableWordWrapping = true;

            // Divider
            GameObject divGo = new GameObject("Divider");
            divGo.transform.SetParent(root.transform, false);
            var divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(0.4f, 0.5f, 0.7f, 0.6f);
            var divRT = divGo.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.03f, 0.72f);
            divRT.anchorMax = new Vector2(0.97f, 0.725f);
            divRT.offsetMin = Vector2.zero;
            divRT.offsetMax = Vector2.zero;

            // Description text (lower 70%)
            GameObject descGo = new GameObject("DescriptionText");
            descGo.transform.SetParent(root.transform, false);
            var descRT = descGo.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0, 0);
            descRT.anchorMax = new Vector2(1, 0.72f);
            descRT.offsetMin = new Vector2(4, 4);
            descRT.offsetMax = new Vector2(-4, -4);
            var descTxt = descGo.AddComponent<TextMeshProUGUI>();
            descTxt.fontSize  = 11;
            descTxt.color     = new Color(0.85f, 0.87f, 0.90f);
            descTxt.alignment = TextAlignmentOptions.TopLeft;
            descTxt.text      = "Card description goes here.";
            descTxt.enableWordWrapping = true;

            // Add CardView script and wire refs
            var cardView = root.AddComponent<CardView>();
            cardView.costText        = costTxt;
            cardView.nameText        = nameTxt;
            cardView.descriptionText = descTxt;
            cardView.playButton      = btn;
            cardView.background      = rootImg;

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            return prefab.GetComponent<CardView>();
        }

        // ── Utility helpers ────────────────────────────────────────────────────────

        private static GameObject MakePanel(GameObject parent, string goName, Color color)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            go.AddComponent<RectTransform>();
            return go;
        }

        private static Image MakeImage(GameObject parent, string goName, Color color)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI MakeLabel(GameObject parent, string goName,
            string text, float fontSize, Color color, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = pivot;
            rt.offsetMin = new Vector2(4, 2);
            rt.offsetMax = new Vector2(-4, -2);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static void SetAnchored(RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = pivot;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == scenePath) return; // already added

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(newScenes, 0);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
