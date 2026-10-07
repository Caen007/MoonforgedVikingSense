using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Moonforged.VikingSense
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public sealed class MoonforgedVikingSense : BaseUnityPlugin
    {
        public const string PluginGUID = "Moonforged.VikingSense";
        public const string PluginName = "Moonforged Viking Sense";
        public const string PluginVersion = "1.0.0";

        private const string ScanButtonName = "MoonforgedVikingSense_Scan";
        private const int PulseSegments = 96;
        private const int CurrentHelpVersion = 4;

        private static readonly string[] DefaultMineableTokens =
        {
            "rock4_copper",
            "minerock_tin",
            "copper",
            "tin",
            "obsidian",
            "flametal",
            "magma",
            "meteorite",
            "blackmarble",
            "black_marble",
            "softtissue",
            "soft_tissue",
            "ancientroot",
            "ancient_root",
            "guck",
            "crystal",
            "leviathan",
            "chitin"
        };

        private static readonly string[] ProgressionLockedMineableTokens =
        {
            "silvervein",
            "silver_vein",
            "silver",
            "mudpile",
            "mud_pile",
            "iron"
        };

        private static readonly string[] MistlandsLandmarkTokens =
        {
            "giant_sword",
            "giantsword",
            "giant_helmet",
            "gianthelmet",
            "giant_armor",
            "giantarmor",
            "giant_ribs",
            "giantribs",
            "giant_skull",
            "giantskull",
            "giant_brain",
            "giantbrain"
        };

        private static readonly string[] DefaultFloraTokens =
        {
            "raspberry",
            "blueberry",
            "cloudberry",
            "berrybush",
            "berry_bush",
            "mushroom",
            "dandelion",
            "thistle",
            "fiddlehead",
            "vineberry",
            "flower",
            "carrot",
            "turnip",
            "onion",
            "barley",
            "flax",
            "seed"
        };

        private static readonly string[] IgnoredPickableTokens =
        {
            "pickable_branch",
            "pickable_stone",
            "pickable_flint"
        };

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<KeyCode> _scanKey;
        private ConfigEntry<float> _scanRadius;
        private ConfigEntry<float> _scanTravelSeconds;
        private ConfigEntry<float> _highlightDuration;
        private ConfigEntry<float> _cooldownSeconds;
        private ConfigEntry<float> _highlightIntensity;
        private ConfigEntry<string> _pulseColorHex;
        private ConfigEntry<string> _highlightColorHex;
        private ConfigEntry<string> _mineableColorHex;
        private ConfigEntry<string> _buildingColorHex;
        private ConfigEntry<string> _creatureColorHex;
        private ConfigEntry<string> _otherPlayerColorHex;
        private ConfigEntry<bool> _detectPickables;
        private ConfigEntry<bool> _detectDroppedItems;
        private ConfigEntry<bool> _detectMineables;
        private ConfigEntry<bool> _detectGenericMineables;
        private ConfigEntry<bool> _detectBuildings;
        private ConfigEntry<bool> _detectCavesAndDungeons;
        private ConfigEntry<bool> _detectMistlandsLandmarks;
        private ConfigEntry<bool> _detectWorldSpawners;
        private ConfigEntry<bool> _detectCreatures;
        private ConfigEntry<bool> _detectOtherPlayers;
        private ConfigEntry<string> _additionalPrefabTokens;
        private ConfigEntry<int> _maximumTargets;
        private ConfigEntry<bool> _showMessages;
        private ConfigEntry<bool> _showFirstRunHelp;
        private ConfigEntry<float> _firstRunHelpDuration;
        private ConfigEntry<bool> _firstRunHelpShown;
        private ConfigEntry<int> _firstRunHelpVersion;

        private ButtonConfig _scanButton;

        private bool _scanActive;
        private bool _welcomeHandledThisSession;
        private bool _helpPanelVisible;
        private PulseSurfaceMode _pulseSurfaceMode;
        private float _welcomeReadyAt = -1f;
        private float _helpPanelUntil;
        private float _scanStartedAt;
        private float _nextScanAllowedAt;
        private Vector3 _scanOrigin;
        private readonly List<ScanTarget> _scanTargets = new List<ScanTarget>();
        private readonly Dictionary<int, ResourceHighlight> _activeHighlights = new Dictionary<int, ResourceHighlight>();

        private GameObject _pulseObject;
        private LineRenderer _pulseRenderer;
        private Material _pulseMaterial;

        private GUIStyle _helpTitleStyle;
        private GUIStyle _helpKeyStyle;
        private GUIStyle _helpBodyStyle;
        private GUIStyle _helpLegendStyle;
        private GUIStyle _cooldownTitleStyle;
        private GUIStyle _cooldownTimeStyle;

        private void Awake()
        {
            BindConfig();
            RegisterInput();
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void BindConfig()
        {
            Config.SaveOnConfigSet = true;

            _enabled = Config.Bind(
                "01 - General",
                "Enabled",
                true,
                "Enable Moonforged Viking Sense.");

            _scanKey = Config.Bind(
                "01 - General",
                "Scan Key",
                KeyCode.Z,
                "Key used to activate Viking Sense.");

            _scanRadius = Config.Bind(
                "02 - Scan",
                "Scan Radius",
                60f,
                new ConfigDescription(
                    "Maximum scan radius in metres.",
                    new AcceptableValueRange<float>(5f, 100f)));

            _scanTravelSeconds = Config.Bind(
                "02 - Scan",
                "Pulse Travel Seconds",
                1.5f,
                new ConfigDescription(
                    "How long the visible scan wave takes to travel from the player to the maximum radius.",
                    new AcceptableValueRange<float>(0.25f, 8f)));

            _cooldownSeconds = Config.Bind(
                "02 - Scan",
                "Cooldown Seconds",
                10f,
                new ConfigDescription(
                    "Time before Viking Sense can be activated again.",
                    new AcceptableValueRange<float>(0f, 120f)));

            _maximumTargets = Config.Bind(
                "02 - Scan",
                "Maximum Targets Per Scan",
                400,
                new ConfigDescription(
                    "Safety limit for the number of resource objects processed by one scan.",
                    new AcceptableValueRange<int>(25, 2000)));

            _highlightDuration = Config.Bind(
                "03 - Highlight",
                "Highlight Duration Seconds",
                10f,
                new ConfigDescription(
                    "How long a detected object remains highlighted after the scan wave reaches it.",
                    new AcceptableValueRange<float>(0.5f, 30f)));

            _highlightIntensity = Config.Bind(
                "03 - Highlight",
                "Highlight Intensity",
                0.65f,
                new ConfigDescription(
                    "Strength of the temporary scan tint. Lower values are softer and less bright.",
                    new AcceptableValueRange<float>(0.10f, 2f)));

            _highlightColorHex = Config.Bind(
                "03 - Highlight",
                "Highlight Color",
                "C9CDD2",
                "Resource highlight color as RGB or RGBA hex. Default is ASKA-style light grey.");

            _pulseColorHex = Config.Bind(
                "03 - Highlight",
                "Pulse Color",
                "4E9DB8",
                "Expanding scan pulse color as RGB or RGBA hex. Example: 25D9FF.");

            _mineableColorHex = Config.Bind(
                "03 - Highlight",
                "Ore And Vein Color",
                "C9CDD2",
                "Highlight color for ore veins and mineable resource deposits.");

            _buildingColorHex = Config.Bind(
                "03 - Highlight",
                "Building Color",
                "527CAB",
                "Highlight color for player-buildable pieces and structures.");

            _creatureColorHex = Config.Bind(
                "03 - Highlight",
                "Creature Color",
                "C9CDD2",
                "Highlight color for creatures, animals, enemies and bosses.");

            _otherPlayerColorHex = Config.Bind(
                "03 - Highlight",
                "Other Player Color",
                "65A978",
                "Highlight color for other players. The local player is never highlighted.");

            _detectPickables = Config.Bind(
                "04 - Resources",
                "Detect Pickables",
                true,
                "Detect Valheim Pickable objects. This includes most berries, mushrooms, branches, stones, flint and many modded pickables.");

            _detectDroppedItems = Config.Bind(
                "04 - Resources",
                "Detect Dropped Items",
                true,
                "Detect ItemDrop objects lying in the world.");

            _detectMineables = Config.Bind(
                "04 - Resources",
                "Detect Mineable Resources",
                true,
                "Detect visible ore and resource deposits such as copper, tin, obsidian and later-biome resource nodes. Silver veins and hidden iron remain excluded so normal progression is preserved.");

            _detectGenericMineables = Config.Bind(
                "04 - Resources",
                "Detect Generic Mineable Rocks",
                false,
                "Also highlight every MineRock/MineRock5 object, including ordinary mineable stone formations.");

            _detectBuildings = Config.Bind(
                "04 - Resources",
                "Detect Buildings",
                true,
                "Detect Valheim Piece objects, including player-built structures and placed build pieces.");

            _detectCavesAndDungeons = Config.Bind(
                "04 - Resources",
                "Detect Caves And Dungeons",
                true,
                "Detect nearby cave and dungeon location entrances. They use the same blue highlight as buildings.");

            _detectMistlandsLandmarks = Config.Bind(
                "04 - Resources",
                "Detect Mistlands Ancient Objects",
                true,
                "Detect ancient giant objects such as Ancient Swords and other giant remains. They use the same blue highlight as buildings.");

            _detectWorldSpawners = Config.Bind(
                "04 - Resources",
                "Detect World Spawners",
                true,
                "Detect world spawners such as Greydwarf Nests and Monuments of Torment. They use the same blue highlight as caves and landmarks.");

            _detectCreatures = Config.Bind(
                "04 - Resources",
                "Detect Creatures",
                true,
                "Detect all Character objects except players, including animals, enemies, bosses and tamed creatures.");

            _detectOtherPlayers = Config.Bind(
                "04 - Resources",
                "Detect Other Players",
                true,
                "Detect other players in multiplayer. The local player is excluded.");

            _additionalPrefabTokens = Config.Bind(
                "04 - Resources",
                "Additional Prefab Name Tokens",
                string.Empty,
                "Optional comma-separated prefab-name fragments to detect. Useful for custom/modded resources that are not Pickable or MineRock objects.");

            _showMessages = Config.Bind(
                "05 - Interface",
                "Show Messages",
                true,
                "Show a small message when Viking Sense activates or is still on cooldown.");

            _showFirstRunHelp = Config.Bind(
                "05 - Interface",
                "Show First Run Help",
                true,
                "Show a one-time description of Viking Sense when this config is first created.");

            _firstRunHelpDuration = Config.Bind(
                "05 - Interface",
                "First Run Help Duration Seconds",
                12f,
                new ConfigDescription(
                    "How long the first-run Viking Sense information panel stays on screen.",
                    new AcceptableValueRange<float>(5f, 30f)));

            _firstRunHelpShown = Config.Bind(
                "05 - Interface",
                "First Run Help Shown",
                false,
                "Internal flag used to remember that the one-time Viking Sense help message has already been displayed. Set to false to show it again.");

            _firstRunHelpVersion = Config.Bind(
                "05 - Interface",
                "First Run Help Version",
                0,
                "Internal version for the one-time help message.");

            UpgradeOldDefaults();
        }

        private void UpgradeOldDefaults()
        {
            bool changed = false;

            if (Mathf.Approximately(_scanRadius.Value, 30f))
            {
                _scanRadius.Value = 60f;
                changed = true;
            }

            if (Mathf.Approximately(_highlightDuration.Value, 8f))
            {
                _highlightDuration.Value = 10f;
                changed = true;
            }

            if (_highlightIntensity.Value >= 3.9f)
            {
                _highlightIntensity.Value = 0.65f;
                changed = true;
            }

            changed |= ReplaceOldColor(_highlightColorHex, "FFD45A", "C9CDD2");
            changed |= ReplaceOldColor(_highlightColorHex, "D6B75F", "C9CDD2");
            changed |= ReplaceOldColor(_pulseColorHex, "25D9FF", "4E9DB8");
            changed |= ReplaceOldColor(_mineableColorHex, "C06CFF", "C9CDD2");
            changed |= ReplaceOldColor(_mineableColorHex, "9473B8", "C9CDD2");
            changed |= ReplaceOldColor(_buildingColorHex, "3A8DFF", "527CAB");
            changed |= ReplaceOldColor(_creatureColorHex, "FF5A5A", "C9CDD2");
            changed |= ReplaceOldColor(_creatureColorHex, "B95F5F", "C9CDD2");
            changed |= ReplaceOldColor(_otherPlayerColorHex, "55FF7A", "65A978");

            if (changed)
            {
                Config.Save();
            }
        }

        private static bool ReplaceOldColor(ConfigEntry<string> entry, string oldValue, string newValue)
        {
            if (entry == null || !string.Equals(entry.Value, oldValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            entry.Value = newValue;
            return true;
        }

        private void RegisterInput()
        {
            _scanButton = new ButtonConfig
            {
                Name = ScanButtonName,
                Config = _scanKey,
                ActiveInGUI = false,
                ActiveInCustomGUI = false,
                BlockOtherInputs = false
            };

            InputManager.Instance.AddButton(PluginGUID, _scanButton);
        }

        private void Update()
        {
            if (!_enabled.Value)
            {
                if (_scanActive || _activeHighlights.Count > 0 || _pulseObject != null)
                {
                    ClearAllVisuals();
                }
                return;
            }

            UpdateActiveHighlights();

            if (_scanActive)
            {
                UpdateScanWave();
            }

            Player player = Player.m_localPlayer;
            if (player == null || _scanButton == null)
            {
                _welcomeReadyAt = -1f;
                return;
            }

            TryShowFirstRunHelp();

            if (!UnityInput.Current.GetKeyDown(_scanKey.Value))
            {
                return;
            }

            TryStartScan(player);
        }

        private void TryStartScan(Player player)
        {
            if (_scanActive)
            {
                return;
            }

            float now = Time.time;
            if (now < _nextScanAllowedAt)
            {
                return;
            }

            _scanOrigin = player.transform.position;
            _pulseSurfaceMode = DeterminePulseSurfaceMode(_scanOrigin);
            _scanStartedAt = now;
            _nextScanAllowedAt = now + Mathf.Max(0f, _cooldownSeconds.Value);
            _scanActive = true;

            CollectScanTargets(_scanOrigin);
            CreatePulseVisual();

            if (_showMessages.Value)
            {
                ShowCenterMessage("Viking Sense");
            }
        }

        private void TryShowFirstRunHelp()
        {
            if (_welcomeHandledThisSession)
            {
                return;
            }

            if (!_showFirstRunHelp.Value)
            {
                _welcomeHandledThisSession = true;
                return;
            }

            if (_firstRunHelpShown.Value && _firstRunHelpVersion.Value >= CurrentHelpVersion)
            {
                _welcomeHandledThisSession = true;
                return;
            }

            if (MessageHud.instance == null)
            {
                return;
            }

            if (_welcomeReadyAt < 0f)
            {
                _welcomeReadyAt = Time.time + 4f;
                return;
            }

            if (Time.time < _welcomeReadyAt)
            {
                return;
            }

            _helpPanelVisible = true;
            _helpPanelUntil = Time.time + Mathf.Max(5f, _firstRunHelpDuration.Value);

            _firstRunHelpShown.Value = true;
            _firstRunHelpVersion.Value = CurrentHelpVersion;
            Config.Save();
            _welcomeHandledThisSession = true;
        }

        private void OnGUI()
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            DrawCooldownHud();

            if (!_helpPanelVisible)
            {
                return;
            }

            float remaining = _helpPanelUntil - Time.time;
            if (remaining <= 0f)
            {
                _helpPanelVisible = false;
                return;
            }

            EnsureHelpStyles();

            float fade = remaining < 1.5f ? Mathf.Clamp01(remaining / 1.5f) : 1f;
            float width = Mathf.Min(720f, Screen.width - 40f);
            float height = 218f;
            float left = (Screen.width - width) * 0.5f;
            float top = Mathf.Max(36f, Screen.height * 0.08f);
            Rect panel = new Rect(left, top, width, height);

            Color oldColor = GUI.color;
            Color oldBackground = GUI.backgroundColor;

            GUI.color = new Color(0.035f, 0.045f, 0.055f, 0.94f * fade);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);

            GUI.color = new Color(0.86f, 0.68f, 0.30f, fade);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.yMax - 2f, panel.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.y, 2f, panel.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.xMax - 2f, panel.y, 2f, panel.height), Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, fade);

            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 16f, panel.width - 48f, 32f),
                "MOONFORGED VIKING SENSE",
                _helpTitleStyle);

            string keyName = _scanKey.Value.ToString();
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 56f, 106f, 34f),
                "PRESS  " + keyName,
                _helpKeyStyle);

            GUI.Label(
                new Rect(panel.x + 145f, panel.y + 59f, panel.width - 169f, 30f),
                "Scan your surroundings for useful objects and nearby threats.",
                _helpBodyStyle);

            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 103f, panel.width - 48f, 48f),
                "Detects resources, visible ore deposits, buildings, caves, world spawners, creatures, dropped items and nearby players.",
                _helpBodyStyle);

            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 165f, panel.width - 48f, 30f),
                "<color=#D6B75F>Resources</color>   •   <color=#9473B8>Ore</color>   •   <color=#527CAB>Buildings & Caves</color>   •   <color=#C9CDD2>Creatures</color>   •   <color=#65A978>Players</color>",
                _helpLegendStyle);

            GUI.color = oldColor;
            GUI.backgroundColor = oldBackground;
        }

        private void DrawCooldownHud()
        {
            if (!_showMessages.Value)
            {
                return;
            }

            float cooldown = Mathf.Max(0f, _cooldownSeconds.Value);
            float remaining = _nextScanAllowedAt - Time.time;
            if (cooldown <= 0f || remaining <= 0f)
            {
                return;
            }

            EnsureCooldownStyles();

            float width = 180f;
            float height = 38f;
            float left = (Screen.width - width) * 0.5f;
            float top = (Screen.height * 0.5f) + 34f;
            Rect panel = new Rect(left, top, width, height);

            Color oldColor = GUI.color;

            GUI.color = new Color(0.025f, 0.032f, 0.040f, 0.86f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);

            GUI.color = new Color(0.72f, 0.58f, 0.28f, 0.90f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.yMax - 1f, panel.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.y, 1f, panel.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.xMax - 1f, panel.y, 1f, panel.height), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(
                new Rect(panel.x + 8f, panel.y + 4f, 108f, 20f),
                "VIKING SENSE",
                _cooldownTitleStyle);

            GUI.Label(
                new Rect(panel.x + 114f, panel.y + 4f, 58f, 20f),
                remaining.ToString("0.0") + "s",
                _cooldownTimeStyle);

            float progress = Mathf.Clamp01(remaining / cooldown);
            Rect barBack = new Rect(panel.x + 8f, panel.y + 28f, panel.width - 16f, 3f);
            GUI.color = new Color(0.18f, 0.20f, 0.22f, 0.95f);
            GUI.DrawTexture(barBack, Texture2D.whiteTexture);

            GUI.color = new Color(0.72f, 0.58f, 0.28f, 0.95f);
            GUI.DrawTexture(
                new Rect(barBack.x, barBack.y, barBack.width * progress, barBack.height),
                Texture2D.whiteTexture);

            GUI.color = oldColor;
        }

        private void EnsureCooldownStyles()
        {
            if (_cooldownTitleStyle != null)
            {
                return;
            }

            _cooldownTitleStyle = new GUIStyle(GUI.skin.label);
            _cooldownTitleStyle.fontSize = 12;
            _cooldownTitleStyle.fontStyle = FontStyle.Bold;
            _cooldownTitleStyle.alignment = TextAnchor.MiddleLeft;
            _cooldownTitleStyle.normal.textColor = new Color(0.88f, 0.78f, 0.52f, 1f);

            _cooldownTimeStyle = new GUIStyle(GUI.skin.label);
            _cooldownTimeStyle.fontSize = 12;
            _cooldownTimeStyle.fontStyle = FontStyle.Bold;
            _cooldownTimeStyle.alignment = TextAnchor.MiddleRight;
            _cooldownTimeStyle.normal.textColor = new Color(0.92f, 0.94f, 0.96f, 1f);
        }

        private void EnsureHelpStyles()
        {
            if (_helpTitleStyle != null)
            {
                return;
            }

            _helpTitleStyle = new GUIStyle(GUI.skin.label);
            _helpTitleStyle.fontSize = 22;
            _helpTitleStyle.fontStyle = FontStyle.Bold;
            _helpTitleStyle.alignment = TextAnchor.MiddleLeft;
            _helpTitleStyle.normal.textColor = new Color(0.96f, 0.82f, 0.48f, 1f);

            _helpKeyStyle = new GUIStyle(GUI.skin.box);
            _helpKeyStyle.fontSize = 16;
            _helpKeyStyle.fontStyle = FontStyle.Bold;
            _helpKeyStyle.alignment = TextAnchor.MiddleCenter;
            _helpKeyStyle.normal.textColor = Color.white;

            _helpBodyStyle = new GUIStyle(GUI.skin.label);
            _helpBodyStyle.fontSize = 15;
            _helpBodyStyle.wordWrap = true;
            _helpBodyStyle.alignment = TextAnchor.UpperLeft;
            _helpBodyStyle.normal.textColor = new Color(0.90f, 0.92f, 0.94f, 1f);

            _helpLegendStyle = new GUIStyle(GUI.skin.label);
            _helpLegendStyle.fontSize = 14;
            _helpLegendStyle.fontStyle = FontStyle.Bold;
            _helpLegendStyle.richText = true;
            _helpLegendStyle.alignment = TextAnchor.MiddleLeft;
            _helpLegendStyle.normal.textColor = Color.white;
        }

        private void CollectScanTargets(Vector3 origin)
        {
            _scanTargets.Clear();

            float radius = Mathf.Max(1f, _scanRadius.Value);
            HashSet<int> seen = new HashSet<int>();
            int maximumTargets = Mathf.Max(1, _maximumTargets.Value);

            // Collect broadly first, then apply a fair final limit. The previous
            // resource-first hard cap could fill the scan list before creatures
            // or buildings were ever considered.
            int collectionLimit = Mathf.Max(20000, maximumTargets * 10);

            CollectPickables(origin, radius, seen, collectionLimit);
            CollectPlantsAndFlora(origin, radius, seen, collectionLimit);
            CollectDroppedItems(origin, radius, seen, collectionLimit);
            CollectMineRocks(origin, radius, seen, collectionLimit);
            CollectMineablesAndLandmarks(origin, radius, seen, collectionLimit);
            CollectCharacters(origin, radius, seen, collectionLimit);
            CollectLocationProxies(origin, radius, seen, collectionLimit);
            CollectWorldSpawners(origin, radius, seen, collectionLimit);
            CollectBuildings(origin, radius, seen, collectionLimit);

            // Final collider pass catches unusual/modded objects and custom name tokens
            // which are not represented by the normal Valheim resource components.
            if (_scanTargets.Count < collectionLimit)
            {
                Collider[] colliders = Physics.OverlapSphere(origin, radius, Physics.AllLayers, QueryTriggerInteraction.Collide);
                for (int i = 0; i < colliders.Length; ++i)
                {
                    Collider collider = colliders[i];
                    if (collider == null)
                    {
                        continue;
                    }

                    GameObject target;
                    ScanCategory category;
                    if (!TryResolveResource(collider, out target, out category))
                    {
                        continue;
                    }

                    AddScanTarget(target, category, origin, radius, seen, collectionLimit);
                    if (_scanTargets.Count >= collectionLimit)
                    {
                        break;
                    }
                }
            }

            BalanceScanTargets(maximumTargets);
            _scanTargets.Sort(CompareScanTargetsByDistance);
        }

        private void BalanceScanTargets(int maximumTargets)
        {
            if (_scanTargets.Count <= maximumTargets)
            {
                return;
            }

            Dictionary<ScanCategory, List<ScanTarget>> buckets = new Dictionary<ScanCategory, List<ScanTarget>>();
            Array categories = Enum.GetValues(typeof(ScanCategory));
            for (int i = 0; i < categories.Length; ++i)
            {
                ScanCategory category = (ScanCategory)categories.GetValue(i);
                buckets[category] = new List<ScanTarget>();
            }

            for (int i = 0; i < _scanTargets.Count; ++i)
            {
                ScanTarget target = _scanTargets[i];
                buckets[target.Category].Add(target);
            }

            int nonEmptyCategoryCount = 0;
            foreach (KeyValuePair<ScanCategory, List<ScanTarget>> pair in buckets)
            {
                pair.Value.Sort(CompareScanTargetsByDistance);
                if (pair.Value.Count > 0)
                {
                    nonEmptyCategoryCount++;
                }
            }

            if (nonEmptyCategoryCount == 0)
            {
                _scanTargets.Clear();
                return;
            }

            // Reserve half of the configured target budget equally across all categories
            // that actually have results. This guarantees nearby creatures/buildings do
            // not disappear just because many plants or build pieces exist in the area.
            int reservedPerCategory = Mathf.Max(1, maximumTargets / (nonEmptyCategoryCount * 2));
            List<ScanTarget> selected = new List<ScanTarget>(maximumTargets);
            HashSet<int> selectedIds = new HashSet<int>();

            foreach (KeyValuePair<ScanCategory, List<ScanTarget>> pair in buckets)
            {
                List<ScanTarget> bucket = pair.Value;
                int take = Mathf.Min(reservedPerCategory, bucket.Count);
                for (int i = 0; i < take && selected.Count < maximumTargets; ++i)
                {
                    ScanTarget target = bucket[i];
                    int id = target.Target != null ? target.Target.GetInstanceID() : 0;
                    if (id != 0 && selectedIds.Add(id))
                    {
                        selected.Add(target);
                    }
                }
            }

            // Fill the remaining capacity by distance, regardless of category.
            List<ScanTarget> remainder = new List<ScanTarget>(_scanTargets);
            remainder.Sort(CompareScanTargetsByDistance);

            for (int i = 0; i < remainder.Count && selected.Count < maximumTargets; ++i)
            {
                ScanTarget target = remainder[i];
                if (target.Target == null)
                {
                    continue;
                }

                int id = target.Target.GetInstanceID();
                if (selectedIds.Add(id))
                {
                    selected.Add(target);
                }
            }

            _scanTargets.Clear();
            _scanTargets.AddRange(selected);
        }

        private void CollectMineRocks(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectMineables.Value)
            {
                return;
            }

            MineRock5[] mineRocks5 = UnityEngine.Object.FindObjectsByType<MineRock5>(FindObjectsSortMode.None);
            for (int i = 0; i < mineRocks5.Length; ++i)
            {
                MineRock5 mineRock5 = mineRocks5[i];
                if (mineRock5 == null || !mineRock5.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GameObject target = FindNetworkRoot(mineRock5.gameObject);
                if (target != null && (IsWantedMineable(target) || IsWantedMineable(mineRock5.gameObject)))
                {
                    AddScanTarget(target, ScanCategory.Mineable, origin, radius, seen, maximumTargets);
                }

                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }

            MineRock[] mineRocks = UnityEngine.Object.FindObjectsByType<MineRock>(FindObjectsSortMode.None);
            for (int i = 0; i < mineRocks.Length; ++i)
            {
                MineRock mineRock = mineRocks[i];
                if (mineRock == null || !mineRock.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GameObject target = FindNetworkRoot(mineRock.gameObject);
                if (target != null && (IsWantedMineable(target) || IsWantedMineable(mineRock.gameObject)))
                {
                    AddScanTarget(target, ScanCategory.Mineable, origin, radius, seen, maximumTargets);
                }

                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectPickables(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectPickables.Value)
            {
                return;
            }

            Pickable[] pickables = UnityEngine.Object.FindObjectsByType<Pickable>(FindObjectsSortMode.None);
            for (int i = 0; i < pickables.Length; ++i)
            {
                Pickable pickable = pickables[i];
                if (pickable == null || !pickable.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddScanTarget(FindNetworkRoot(pickable.gameObject), ScanCategory.Resource, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }

            PickableItem[] pickableItems = UnityEngine.Object.FindObjectsByType<PickableItem>(FindObjectsSortMode.None);
            for (int i = 0; i < pickableItems.Length; ++i)
            {
                PickableItem pickableItem = pickableItems[i];
                if (pickableItem == null || !pickableItem.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddScanTarget(FindNetworkRoot(pickableItem.gameObject), ScanCategory.Resource, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectPlantsAndFlora(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectPickables.Value)
            {
                return;
            }

            Plant[] plants = UnityEngine.Object.FindObjectsByType<Plant>(FindObjectsSortMode.None);
            for (int i = 0; i < plants.Length; ++i)
            {
                Plant plant = plants[i];
                if (plant == null || !plant.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddScanTarget(FindNetworkRoot(plant.gameObject), ScanCategory.Resource, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }

            // Some wild flora prefabs expose their pickable component on a child, or use
            // vegetation-specific setups which make component-only discovery unreliable.
            // Matching the network prefab root catches those without scanning generic trees.
            ZNetView[] networkViews = UnityEngine.Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None);
            for (int i = 0; i < networkViews.Length; ++i)
            {
                ZNetView view = networkViews[i];
                if (view == null || !view.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GameObject candidate = view.gameObject;
                if (!NameContainsAnyToken(candidate.name, DefaultFloraTokens))
                {
                    continue;
                }

                AddScanTarget(candidate, ScanCategory.Resource, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectBuildings(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectBuildings.Value)
            {
                return;
            }

            Piece[] pieces = UnityEngine.Object.FindObjectsByType<Piece>(FindObjectsSortMode.None);
            for (int i = 0; i < pieces.Length; ++i)
            {
                Piece piece = pieces[i];
                if (piece == null || !piece.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddScanTarget(FindNetworkRoot(piece.gameObject), ScanCategory.Building, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectDroppedItems(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectDroppedItems.Value)
            {
                return;
            }

            ItemDrop[] drops = UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
            for (int i = 0; i < drops.Length; ++i)
            {
                ItemDrop itemDrop = drops[i];
                if (!IsLooseWorldItem(itemDrop))
                {
                    continue;
                }

                AddScanTarget(FindNetworkRoot(itemDrop.gameObject), ScanCategory.Resource, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectCharacters(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectCreatures.Value && !_detectOtherPlayers.Value)
            {
                return;
            }

            Character[] characters = UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; ++i)
            {
                Character character = characters[i];
                if (character == null || !character.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Player player = character as Player;
                if (player != null)
                {
                    if (player == Player.m_localPlayer || !_detectOtherPlayers.Value)
                    {
                        continue;
                    }

                    AddScanTarget(FindNetworkRoot(player.gameObject), ScanCategory.OtherPlayer, origin, radius, seen, maximumTargets);
                }
                else if (_detectCreatures.Value)
                {
                    AddScanTarget(FindNetworkRoot(character.gameObject), ScanCategory.Creature, origin, radius, seen, maximumTargets);
                }

                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectLocationProxies(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectCavesAndDungeons.Value)
            {
                return;
            }

            LocationProxy[] locations = UnityEngine.Object.FindObjectsByType<LocationProxy>(FindObjectsSortMode.None);
            for (int i = 0; i < locations.Length; ++i)
            {
                LocationProxy location = locations[i];
                if (location == null || !location.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddScanTarget(FindNetworkRoot(location.gameObject), ScanCategory.Landmark, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectWorldSpawners(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (!_detectWorldSpawners.Value)
            {
                return;
            }

            SpawnArea[] spawners = UnityEngine.Object.FindObjectsByType<SpawnArea>(FindObjectsSortMode.None);
            for (int i = 0; i < spawners.Length; ++i)
            {
                SpawnArea spawnArea = spawners[i];
                if (spawnArea == null || !spawnArea.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddScanTarget(FindNetworkRoot(spawnArea.gameObject), ScanCategory.Landmark, origin, radius, seen, maximumTargets);
                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void CollectMineablesAndLandmarks(Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            Destructible[] destructibles = UnityEngine.Object.FindObjectsByType<Destructible>(FindObjectsSortMode.None);
            for (int i = 0; i < destructibles.Length; ++i)
            {
                Destructible destructible = destructibles[i];
                if (destructible == null || !destructible.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GameObject target = FindNetworkRoot(destructible.gameObject);
                if (target == null)
                {
                    continue;
                }

                if (_detectMistlandsLandmarks.Value && NameContainsAnyToken(target.name, MistlandsLandmarkTokens))
                {
                    AddScanTarget(target, ScanCategory.Landmark, origin, radius, seen, maximumTargets);
                }
                else if (_detectMineables.Value && IsWantedMineable(target))
                {
                    AddScanTarget(target, ScanCategory.Mineable, origin, radius, seen, maximumTargets);
                }

                if (_scanTargets.Count >= maximumTargets)
                {
                    return;
                }
            }
        }

        private void AddScanTarget(GameObject target, ScanCategory category, Vector3 origin, float radius, HashSet<int> seen, int maximumTargets)
        {
            if (target == null || !target.activeInHierarchy || _scanTargets.Count >= maximumTargets)
            {
                return;
            }

            if (category == ScanCategory.Resource && ShouldIgnorePickable(target))
            {
                return;
            }

            int instanceId = target.GetInstanceID();
            if (!seen.Add(instanceId))
            {
                return;
            }

            float distance = HorizontalDistance(origin, target.transform.position);
            if (distance > radius)
            {
                return;
            }

            _scanTargets.Add(new ScanTarget(target, distance, category));
        }

        private bool TryResolveResource(Collider collider, out GameObject target, out ScanCategory category)
        {
            target = null;
            category = ScanCategory.Resource;

            Player foundPlayer = collider.GetComponentInParent<Player>();
            if (foundPlayer != null)
            {
                if (foundPlayer == Player.m_localPlayer)
                {
                    return false;
                }

                if (_detectOtherPlayers.Value)
                {
                    target = FindNetworkRoot(foundPlayer.gameObject);
                    category = ScanCategory.OtherPlayer;
                    return target != null;
                }

                return false;
            }

            if (_detectCreatures.Value)
            {
                Character character = collider.GetComponentInParent<Character>();
                if (character != null)
                {
                    target = FindNetworkRoot(character.gameObject);
                    category = ScanCategory.Creature;
                    return target != null;
                }

            }

            if (_detectCavesAndDungeons.Value)
            {
                LocationProxy locationProxy = collider.GetComponentInParent<LocationProxy>();
                if (locationProxy != null)
                {
                    target = FindNetworkRoot(locationProxy.gameObject);
                    category = ScanCategory.Landmark;
                    return target != null;
                }
            }

            if (_detectWorldSpawners.Value)
            {
                SpawnArea spawnArea = collider.GetComponentInParent<SpawnArea>();
                if (spawnArea != null)
                {
                    target = FindNetworkRoot(spawnArea.gameObject);
                    category = ScanCategory.Landmark;
                    return target != null;
                }
            }

            GameObject candidate = FindNetworkRoot(collider.gameObject);

            if (_detectMistlandsLandmarks.Value && candidate != null && NameContainsAnyToken(candidate.name, MistlandsLandmarkTokens))
            {
                target = candidate;
                category = ScanCategory.Landmark;
                return true;
            }

            if (_detectPickables.Value && candidate != null && NameContainsAnyToken(candidate.name, DefaultFloraTokens))
            {
                target = candidate;
                category = ScanCategory.Resource;
                return true;
            }

            if (_detectBuildings.Value)
            {
                Piece piece = collider.GetComponentInParent<Piece>();
                if (piece != null)
                {
                    target = FindNetworkRoot(piece.gameObject);
                    category = ScanCategory.Building;
                    return target != null;
                }
            }

            if (_detectDroppedItems.Value)
            {
                ItemDrop itemDrop = collider.GetComponentInParent<ItemDrop>();
                if (itemDrop != null && IsLooseWorldItem(itemDrop))
                {
                    target = FindNetworkRoot(itemDrop.gameObject);
                    category = ScanCategory.Resource;
                    return target != null;
                }
            }

            if (_detectPickables.Value)
            {
                Pickable pickable = collider.GetComponentInParent<Pickable>();
                if (pickable != null)
                {
                    target = FindNetworkRoot(pickable.gameObject);
                    category = ScanCategory.Resource;
                    return target != null;
                }

                PickableItem pickableItem = collider.GetComponentInParent<PickableItem>();
                if (pickableItem != null)
                {
                    target = FindNetworkRoot(pickableItem.gameObject);
                    category = ScanCategory.Resource;
                    return target != null;
                }
            }

            if (_detectMineables.Value)
            {
                MineRock5 mineRock5 = collider.GetComponentInParent<MineRock5>();
                if (mineRock5 != null)
                {
                    GameObject mineTarget = FindNetworkRoot(mineRock5.gameObject);
                    if (mineTarget != null && (IsWantedMineable(mineTarget) || IsWantedMineable(mineRock5.gameObject)))
                    {
                        target = mineTarget;
                        category = ScanCategory.Mineable;
                        return true;
                    }
                }

                MineRock mineRock = collider.GetComponentInParent<MineRock>();
                if (mineRock != null)
                {
                    GameObject mineTarget = FindNetworkRoot(mineRock.gameObject);
                    if (mineTarget != null && (IsWantedMineable(mineTarget) || IsWantedMineable(mineRock.gameObject)))
                    {
                        target = mineTarget;
                        category = ScanCategory.Mineable;
                        return true;
                    }
                }

                Destructible destructible = collider.GetComponentInParent<Destructible>();
                if (destructible != null)
                {
                    GameObject mineTarget = FindNetworkRoot(destructible.gameObject);
                    if (mineTarget != null && (IsWantedMineable(mineTarget) || IsWantedMineable(destructible.gameObject)))
                    {
                        target = mineTarget;
                        category = ScanCategory.Mineable;
                        return true;
                    }
                }
            }

            string customTokens = _additionalPrefabTokens.Value;
            if (!string.IsNullOrWhiteSpace(customTokens) && candidate != null)
            {
                if (NameContainsAnyToken(candidate.name, SplitTokens(customTokens)))
                {
                    target = candidate;
                    category = ScanCategory.Resource;
                    return true;
                }
            }

            return false;
        }

        private bool IsLooseWorldItem(ItemDrop itemDrop)
        {
            if (itemDrop == null || !itemDrop.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (itemDrop.GetComponentInParent<ItemStand>() != null)
            {
                return false;
            }

            return true;
        }

        private bool IsWantedMineable(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (NameContainsAnyToken(target.name, ProgressionLockedMineableTokens))
            {
                return false;
            }

            if (_detectGenericMineables.Value)
            {
                return true;
            }

            return NameContainsAnyToken(target.name, DefaultMineableTokens);
        }

        private static bool ShouldIgnorePickable(GameObject target)
        {
            return target != null && NameContainsAnyToken(target.name, IgnoredPickableTokens);
        }

        private static bool IsFloraAuraTarget(GameObject target)
        {
            return target != null && NameContainsAnyToken(target.name, DefaultFloraTokens);
        }

        private static bool NameContainsAnyToken(string objectName, string[] tokens)
        {
            if (string.IsNullOrEmpty(objectName) || tokens == null || tokens.Length == 0)
            {
                return false;
            }

            string normalizedName = NormalizePrefabName(objectName).ToLowerInvariant();
            for (int i = 0; i < tokens.Length; ++i)
            {
                string token = tokens[i];
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (normalizedName.Contains(token.Trim().ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] SplitTokens(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new string[0];
            }

            return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizePrefabName(string objectName)
        {
            return objectName.Replace("(Clone)", string.Empty).Trim();
        }

        private static GameObject FindNetworkRoot(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            ZNetView networkView = source.GetComponentInParent<ZNetView>();
            if (networkView != null)
            {
                return networkView.gameObject;
            }

            return source;
        }

        private void UpdateScanWave()
        {
            float travelSeconds = Mathf.Max(0.05f, _scanTravelSeconds.Value);
            float progress = Mathf.Clamp01((Time.time - _scanStartedAt) / travelSeconds);
            float currentRadius = Mathf.SmoothStep(0f, Mathf.Max(1f, _scanRadius.Value), progress);

            UpdatePulseVisual(currentRadius, progress);

            for (int i = 0; i < _scanTargets.Count; ++i)
            {
                ScanTarget scanTarget = _scanTargets[i];
                if (scanTarget.Triggered)
                {
                    continue;
                }

                if (scanTarget.Target == null || !scanTarget.Target.activeInHierarchy)
                {
                    scanTarget.Triggered = true;
                    continue;
                }

                if (scanTarget.Distance <= currentRadius || progress >= 1f)
                {
                    scanTarget.Triggered = true;
                    HighlightResource(scanTarget.Target, scanTarget.Category);
                }
            }

            if (progress >= 1f)
            {
                _scanActive = false;
                _scanTargets.Clear();
                DestroyPulseVisual();
            }
        }

        private void HighlightResource(GameObject target, ScanCategory category)
        {
            if (target == null)
            {
                return;
            }

            int instanceId = target.GetInstanceID();
            ResourceHighlight existing;
            if (_activeHighlights.TryGetValue(instanceId, out existing))
            {
                existing.Refresh(Time.time, Mathf.Max(0.1f, _highlightDuration.Value), category);
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if ((renderers == null || renderers.Length == 0) && category != ScanCategory.Landmark)
            {
                return;
            }

            if (renderers == null)
            {
                renderers = new Renderer[0];
            }

            ResourceHighlight highlight = new ResourceHighlight(
                target,
                renderers,
                Time.time,
                Mathf.Max(0.1f, _highlightDuration.Value),
                category);

            if (!highlight.HasRendererStates)
            {
                highlight.Restore();
                return;
            }

            _activeHighlights.Add(instanceId, highlight);
        }

        private void UpdateActiveHighlights()
        {
            if (_activeHighlights.Count == 0)
            {
                return;
            }

            float intensity = Mathf.Clamp(_highlightIntensity.Value, 0.10f, 1.25f);
            float now = Time.time;
            List<int> finished = null;

            foreach (KeyValuePair<int, ResourceHighlight> pair in _activeHighlights)
            {
                ResourceHighlight highlight = pair.Value;
                if (highlight == null || highlight.Target == null || now >= highlight.EndTime)
                {
                    if (finished == null)
                    {
                        finished = new List<int>();
                    }
                    finished.Add(pair.Key);
                    continue;
                }

                Color highlightColor = GetHighlightColor(highlight.Target, highlight.Category);
                float strength = highlight.GetStrength(now);
                highlight.Apply(highlightColor, intensity, strength);
            }

            if (finished == null)
            {
                return;
            }

            for (int i = 0; i < finished.Count; ++i)
            {
                int key = finished[i];
                ResourceHighlight highlight;
                if (_activeHighlights.TryGetValue(key, out highlight))
                {
                    if (highlight != null)
                    {
                        highlight.Restore();
                    }
                    _activeHighlights.Remove(key);
                }
            }
        }

        private Color GetHighlightColor(GameObject target, ScanCategory category)
        {
            switch (category)
            {
                case ScanCategory.Resource:
                    return GetResourceHighlightColor(target);
                case ScanCategory.Mineable:
                    return GetMineableHighlightColor(target);
                case ScanCategory.Building:
                case ScanCategory.Landmark:
                    return ParseColor(_buildingColorHex.Value, new Color(0.32f, 0.49f, 0.67f, 1f));
                case ScanCategory.Creature:
                    return ParseColor(_creatureColorHex.Value, new Color(0.79f, 0.80f, 0.82f, 1f));
                case ScanCategory.OtherPlayer:
                    return ParseColor(_otherPlayerColorHex.Value, new Color(0.40f, 0.66f, 0.47f, 1f));
                default:
                    return ParseColor(_highlightColorHex.Value, new Color(0.84f, 0.72f, 0.37f, 1f));
            }
        }

        private Color GetResourceHighlightColor(GameObject target)
        {
            string name = target != null ? NormalizePrefabName(target.name).ToLowerInvariant() : string.Empty;

            if (name.Contains("raspberry"))
            {
                return new Color(0.82f, 0.25f, 0.28f, 1f);
            }

            if (name.Contains("blueberry"))
            {
                return new Color(0.34f, 0.50f, 0.92f, 1f);
            }

            if (name.Contains("cloudberry"))
            {
                return new Color(0.93f, 0.67f, 0.22f, 1f);
            }

            if (name.Contains("vineberry"))
            {
                return new Color(0.62f, 0.36f, 0.88f, 1f);
            }

            if (name.Contains("thistle"))
            {
                return new Color(0.42f, 0.88f, 0.90f, 1f);
            }

            if (name.Contains("dandelion") || name.Contains("flower"))
            {
                return new Color(0.92f, 0.82f, 0.27f, 1f);
            }

            if (name.Contains("mushroomyellow"))
            {
                return new Color(0.95f, 0.82f, 0.24f, 1f);
            }

            if (name.Contains("mushroomblue"))
            {
                return new Color(0.34f, 0.50f, 0.92f, 1f);
            }

            if (name.Contains("mushroomjotun"))
            {
                return new Color(0.45f, 0.72f, 0.94f, 1f);
            }

            if (name.Contains("mushroommagecap"))
            {
                return new Color(0.62f, 0.36f, 0.88f, 1f);
            }

            if (name.Contains("mushroom"))
            {
                return new Color(0.85f, 0.35f, 0.26f, 1f);
            }

            if (name.Contains("fiddlehead"))
            {
                return new Color(0.48f, 0.84f, 0.36f, 1f);
            }

            return ParseColor(_highlightColorHex.Value, new Color(0.84f, 0.72f, 0.37f, 1f));
        }

        private Color GetMineableHighlightColor(GameObject target)
        {
            string name = target != null ? NormalizePrefabName(target.name).ToLowerInvariant() : string.Empty;

            if (name.Contains("copper"))
            {
                return new Color(0.87f, 0.53f, 0.24f, 1f);
            }

            if (name.Contains("tin"))
            {
                return new Color(0.82f, 0.85f, 0.88f, 1f);
            }

            if (name.Contains("obsidian"))
            {
                return new Color(0.46f, 0.44f, 0.56f, 1f);
            }

            if (name.Contains("flametal") || name.Contains("magma") || name.Contains("meteorite"))
            {
                return new Color(0.90f, 0.40f, 0.18f, 1f);
            }

            if (name.Contains("crystal"))
            {
                return new Color(0.58f, 0.90f, 0.96f, 1f);
            }

            if (name.Contains("guck"))
            {
                return new Color(0.48f, 0.84f, 0.36f, 1f);
            }

            return ParseColor(_mineableColorHex.Value, new Color(0.58f, 0.45f, 0.72f, 1f));
        }

        private PulseSurfaceMode DeterminePulseSurfaceMode(Vector3 origin)
        {
            if (ZoneSystem.instance == null)
            {
                return PulseSurfaceMode.Terrain;
            }

            float waterLevel = ZoneSystem.instance.m_waterLevel;
            float groundHeight;
            bool hasGround = ZoneSystem.instance.GetGroundHeight(origin, out groundHeight);
            bool waterAboveGround = !hasGround || waterLevel > groundHeight + 0.35f;

            if (!waterAboveGround)
            {
                return PulseSurfaceMode.Terrain;
            }

            if (origin.y < waterLevel - 1f)
            {
                return PulseSurfaceMode.Underwater;
            }

            if (origin.y <= waterLevel + 1.5f)
            {
                return PulseSurfaceMode.WaterSurface;
            }

            return PulseSurfaceMode.Terrain;
        }

        private void CreatePulseVisual()
        {
            DestroyPulseVisual();

            _pulseObject = new GameObject("MVS_ScanPulse");
            _pulseRenderer = _pulseObject.AddComponent<LineRenderer>();
            _pulseRenderer.useWorldSpace = true;
            _pulseRenderer.loop = true;
            _pulseRenderer.positionCount = PulseSegments;
            _pulseRenderer.startWidth = 0.10f;
            _pulseRenderer.endWidth = 0.10f;
            _pulseRenderer.numCornerVertices = 2;
            _pulseRenderer.numCapVertices = 2;
            _pulseRenderer.textureMode = LineTextureMode.Stretch;
            _pulseRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pulseRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                _pulseMaterial = new Material(shader);
                _pulseMaterial.name = "MVS_ScanPulse_Material";
                _pulseRenderer.sharedMaterial = _pulseMaterial;
            }

            UpdatePulseVisual(0.2f, 0f);
        }

        private void UpdatePulseVisual(float radius, float progress)
        {
            if (_pulseRenderer == null)
            {
                return;
            }

            Color pulseColor = ParseColor(_pulseColorHex.Value, new Color(0.31f, 0.62f, 0.72f, 1f));
            pulseColor.a = Mathf.Lerp(0.70f, 0.08f, progress);
            _pulseRenderer.startColor = pulseColor;
            _pulseRenderer.endColor = pulseColor;

            for (int i = 0; i < PulseSegments; ++i)
            {
                float angle = ((float)i / PulseSegments) * Mathf.PI * 2f;
                Vector3 point = _scanOrigin + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (_pulseSurfaceMode == PulseSurfaceMode.WaterSurface && ZoneSystem.instance != null)
                {
                    point.y = ZoneSystem.instance.m_waterLevel + 0.12f;
                }
                else if (_pulseSurfaceMode == PulseSurfaceMode.Underwater)
                {
                    point.y = _scanOrigin.y + 0.12f;
                }
                else
                {
                    float groundHeight;
                    if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(point, out groundHeight))
                    {
                        point.y = groundHeight + 0.12f;
                    }
                    else
                    {
                        point.y = _scanOrigin.y + 0.12f;
                    }
                }

                _pulseRenderer.SetPosition(i, point);
            }
        }

        private void DestroyPulseVisual()
        {
            if (_pulseObject != null)
            {
                Destroy(_pulseObject);
                _pulseObject = null;
                _pulseRenderer = null;
            }

            if (_pulseMaterial != null)
            {
                Destroy(_pulseMaterial);
                _pulseMaterial = null;
            }
        }

        private void ClearAllVisuals()
        {
            _scanActive = false;
            _scanTargets.Clear();
            DestroyPulseVisual();

            foreach (KeyValuePair<int, ResourceHighlight> pair in _activeHighlights)
            {
                if (pair.Value != null)
                {
                    pair.Value.Restore();
                }
            }

            _activeHighlights.Clear();
        }

        private void OnDestroy()
        {
            ClearAllVisuals();
        }

        private static int CompareScanTargetsByDistance(ScanTarget left, ScanTarget right)
        {
            return left.Distance.CompareTo(right.Distance);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt((x * x) + (z * z));
        }

        private static Color ParseColor(string raw, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            string value = raw.Trim();
            if (!value.StartsWith("#", StringComparison.Ordinal))
            {
                value = "#" + value;
            }

            Color parsed;
            if (ColorUtility.TryParseHtmlString(value, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static void ShowCenterMessage(string text)
        {
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
            }
        }

        private static void ShowTopLeftMessage(string text)
        {
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
            }
        }

        private enum PulseSurfaceMode
        {
            Terrain,
            WaterSurface,
            Underwater
        }

        private enum ScanCategory
        {
            Resource,
            Mineable,
            Building,
            Landmark,
            Creature,
            OtherPlayer
        }

        private sealed class ScanTarget
        {
            public readonly GameObject Target;
            public readonly float Distance;
            public readonly ScanCategory Category;
            public bool Triggered;

            public ScanTarget(GameObject target, float distance, ScanCategory category)
            {
                Target = target;
                Distance = distance;
                Category = category;
                Triggered = false;
            }
        }

        private sealed class ResourceHighlight
        {
            private readonly List<RendererState> _rendererStates = new List<RendererState>();
            private readonly List<ScanFillOverlay> _fillOverlays = new List<ScanFillOverlay>();
            private readonly List<OutlineShell> _outlineShells = new List<OutlineShell>();
            private readonly List<FloraGlowOverlay> _floraGlows = new List<FloraGlowOverlay>();
            private float _startTime;
            private float _duration;

            public GameObject Target { get; private set; }
            public ScanCategory Category { get; private set; }
            public float EndTime { get { return _startTime + _duration; } }

            public bool HasRendererStates
            {
                get
                {
                    return _rendererStates.Count > 0 ||
                           _fillOverlays.Count > 0 ||
                           _outlineShells.Count > 0 ||
                           _floraGlows.Count > 0;
                }
            }

            public ResourceHighlight(GameObject target, Renderer[] renderers, float startTime, float duration, ScanCategory category)
            {
                Target = target;
                Category = category;
                _startTime = startTime;
                _duration = duration;

                if (renderers == null)
                {
                    renderers = new Renderer[0];
                }

                bool floraAura = category == ScanCategory.Resource && IsFloraAuraTarget(target);
                if (floraAura)
                {
                    // A texture-alpha glow stays visible at night without filling leaf planes.
                    // Include every LOD and follow its source renderer's visibility each frame.
                    for (int i = 0; i < renderers.Length; ++i)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer == null || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                        {
                            continue;
                        }

                        FloraGlowOverlay glow = new FloraGlowOverlay(renderer);
                        if (glow.IsValid)
                        {
                            _floraGlows.Add(glow);
                        }
                        else
                        {
                            glow.Dispose();
                        }
                    }

                    return;
                }

                List<Renderer> visualRenderers = SelectVisualRenderers(renderers, category);

                // Keep the object's real renderer intact and tint it through a property block.
                // This gives a stable base highlight without swapping or cloning its material.
                for (int i = 0; i < renderers.Length; ++i)
                {
                    Renderer renderer = renderers[i];
                    if (!IsSupportedRenderer(renderer))
                    {
                        continue;
                    }

                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        continue;
                    }

                    for (int materialIndex = 0; materialIndex < materials.Length; ++materialIndex)
                    {
                        Material material = materials[materialIndex];
                        if (material == null)
                        {
                            continue;
                        }

                        RendererState state = new RendererState(renderer, material, materialIndex);
                        if (state.CanHighlight)
                        {
                            _rendererStates.Add(state);
                        }
                    }
                }

                bool strongFill = (category == ScanCategory.Resource && !floraAura) || category == ScanCategory.Mineable;
                bool subtleFill = category == ScanCategory.Building ||
                                  category == ScanCategory.Landmark ||
                                  category == ScanCategory.Creature ||
                                  category == ScanCategory.OtherPlayer;

                bool wantsOutline = category == ScanCategory.Mineable ||
                                    category == ScanCategory.Building ||
                                    category == ScanCategory.Landmark ||
                                    category == ScanCategory.Creature ||
                                    category == ScanCategory.OtherPlayer;

                float outlineScale = GetOutlineScale(category);

                for (int i = 0; i < visualRenderers.Count; ++i)
                {
                    Renderer renderer = visualRenderers[i];

                    if (strongFill || subtleFill)
                    {
                        bool throughOccluders = category == ScanCategory.Resource ||
                                                category == ScanCategory.Mineable;

                        float baseAlpha = strongFill ? 0.18f : 0.035f;
                        float peakAlpha = strongFill ? 0.38f : 0.085f;

                        ScanFillOverlay fill = new ScanFillOverlay(
                            renderer,
                            throughOccluders,
                            baseAlpha,
                            peakAlpha);

                        if (fill.IsValid)
                        {
                            _fillOverlays.Add(fill);
                        }
                        else
                        {
                            fill.Dispose();
                        }
                    }

                    if (wantsOutline)
                    {
                        OutlineShell outline = new OutlineShell(renderer, outlineScale);
                        if (outline.IsValid)
                        {
                            _outlineShells.Add(outline);
                        }
                        else
                        {
                            outline.Dispose();
                        }
                    }
                }
            }

            private static bool IsSupportedRenderer(Renderer renderer)
            {
                return renderer != null &&
                       renderer.enabled &&
                       renderer.gameObject.activeInHierarchy &&
                       (renderer is MeshRenderer || renderer is SkinnedMeshRenderer);
            }

            private static List<Renderer> SelectVisualRenderers(Renderer[] renderers, ScanCategory category)
            {
                List<Renderer> candidates = new List<Renderer>();
                float largestVolume = 0f;

                for (int i = 0; i < renderers.Length; ++i)
                {
                    Renderer renderer = renderers[i];
                    if (!IsSupportedRenderer(renderer) || ShouldSkipRenderer(renderer))
                    {
                        continue;
                    }

                    Bounds bounds = renderer.bounds;
                    float volume = Mathf.Abs(bounds.size.x * bounds.size.y * bounds.size.z);
                    largestVolume = Mathf.Max(largestVolume, volume);
                    candidates.Add(renderer);
                }

                if (candidates.Count <= 1)
                {
                    return candidates;
                }

                candidates.Sort(delegate (Renderer a, Renderer b)
                {
                    float av = GetRendererVolume(a);
                    float bv = GetRendererVolume(b);
                    return bv.CompareTo(av);
                });

                int maxRenderers;
                float minimumRelativeVolume;

                switch (category)
                {
                    case ScanCategory.Creature:
                    case ScanCategory.OtherPlayer:
                        maxRenderers = 8;
                        minimumRelativeVolume = 0.01f;
                        break;

                    case ScanCategory.Building:
                    case ScanCategory.Landmark:
                        maxRenderers = 12;
                        minimumRelativeVolume = 0.003f;
                        break;

                    case ScanCategory.Resource:
                    case ScanCategory.Mineable:
                    default:
                        maxRenderers = 10;
                        minimumRelativeVolume = 0f;
                        break;
                }

                List<Renderer> selected = new List<Renderer>();
                for (int i = 0; i < candidates.Count && selected.Count < maxRenderers; ++i)
                {
                    Renderer renderer = candidates[i];
                    if (largestVolume > 0f &&
                        minimumRelativeVolume > 0f &&
                        GetRendererVolume(renderer) < largestVolume * minimumRelativeVolume)
                    {
                        continue;
                    }

                    selected.Add(renderer);
                }

                return selected;
            }

            private static float GetRendererVolume(Renderer renderer)
            {
                if (renderer == null)
                {
                    return 0f;
                }

                Bounds bounds = renderer.bounds;
                return Mathf.Abs(bounds.size.x * bounds.size.y * bounds.size.z);
            }

            private static bool ShouldSkipRenderer(Renderer renderer)
            {
                if (renderer == null)
                {
                    return true;
                }

                string name = renderer.gameObject.name.ToLowerInvariant();
                return name.Contains("collider") ||
                       name.Contains("collision") ||
                       name.Contains("trigger") ||
                       name.Contains("hitbox") ||
                       name.Contains("bounds") ||
                       name.Contains("bounding") ||
                       name.Contains("helper") ||
                       name.Contains("occlud") ||
                       name.Contains("shadow") ||
                       name.Contains("lod1") ||
                       name.Contains("lod2") ||
                       name.Contains("lod3") ||
                       name.Contains("lod4") ||
                       name.Contains("lod5") ||
                       name.Contains("eye") ||
                       name.Contains("pupil") ||
                       name.Contains("teeth") ||
                       name.Contains("weapon") ||
                       name.Contains("attach") ||
                       name.Contains("particle") ||
                       name.Contains("vfx");
            }

            private static float GetOutlineScale(ScanCategory category)
            {
                switch (category)
                {
                    case ScanCategory.Creature:
                    case ScanCategory.OtherPlayer:
                        return 1.045f;

                    case ScanCategory.Mineable:
                        return 1.025f;

                    case ScanCategory.Building:
                    case ScanCategory.Landmark:
                        return 1.018f;

                    default:
                        return 1.02f;
                }
            }

            public void Refresh(float startTime, float duration, ScanCategory category)
            {
                _startTime = startTime;
                _duration = duration;
                Category = category;
            }

            public float GetStrength(float now)
            {
                if (_duration <= 0f)
                {
                    return 0f;
                }

                float normalized = Mathf.Clamp01((now - _startTime) / _duration);

                // A short soft entry avoids a harsh pop, then the highlight remains
                // steady and fades away near the end of the scan duration.
                if (normalized < 0.08f)
                {
                    return Mathf.SmoothStep(0f, 1f, normalized / 0.08f);
                }

                if (normalized <= 0.72f)
                {
                    return 1f;
                }

                return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, normalized));
            }

            public void Apply(Color highlightColor, float intensity, float strength)
            {
                for (int i = 0; i < _floraGlows.Count; ++i)
                {
                    _floraGlows[i].Apply(highlightColor, intensity, strength);
                }

                for (int i = 0; i < _rendererStates.Count; ++i)
                {
                    _rendererStates[i].Apply(highlightColor, intensity, strength);
                }

                for (int i = 0; i < _fillOverlays.Count; ++i)
                {
                    _fillOverlays[i].Apply(highlightColor, intensity, strength);
                }

                for (int i = 0; i < _outlineShells.Count; ++i)
                {
                    _outlineShells[i].Apply(highlightColor, intensity, strength);
                }
            }

            public void Restore()
            {
                for (int i = 0; i < _floraGlows.Count; ++i)
                {
                    _floraGlows[i].Dispose();
                }

                _floraGlows.Clear();
                for (int i = 0; i < _rendererStates.Count; ++i)
                {
                    _rendererStates[i].Restore();
                }

                for (int i = 0; i < _fillOverlays.Count; ++i)
                {
                    _fillOverlays[i].Dispose();
                }

                for (int i = 0; i < _outlineShells.Count; ++i)
                {
                    _outlineShells[i].Dispose();
                }

                _rendererStates.Clear();
                _fillOverlays.Clear();
                _outlineShells.Clear();
                Target = null;
            }
        }

        private sealed class FloraGlowOverlay
        {
            private readonly Renderer _source;
            private readonly Mesh _mesh;
            private readonly GameObject _object;
            private readonly Renderer _renderer;
            private readonly List<Material> _materials = new List<Material>();

            public bool IsValid
            {
                get { return _source != null && _object != null && _renderer != null && _materials.Count > 0; }
            }

            public FloraGlowOverlay(Renderer source)
            {
                _source = source;
                Shader shader = Shader.Find("GUI/Text Shader");
                if (source == null || shader == null)
                {
                    return;
                }

                Material[] sourceMaterials = source.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                {
                    return;
                }

                _object = new GameObject("MVS_FloraGlow");
                _object.transform.SetParent(source.transform, false);
                _object.transform.localPosition = Vector3.zero;
                _object.transform.localRotation = Quaternion.identity;
                _object.transform.localScale = Vector3.one;

                SkinnedMeshRenderer skinned = source as SkinnedMeshRenderer;
                if (skinned != null && skinned.sharedMesh != null)
                {
                    SkinnedMeshRenderer glow = _object.AddComponent<SkinnedMeshRenderer>();
                    _mesh = MakeGlowMesh(skinned.sharedMesh);
                    glow.sharedMesh = _mesh;
                    glow.rootBone = skinned.rootBone;
                    glow.bones = skinned.bones;
                    glow.localBounds = skinned.localBounds;
                    glow.updateWhenOffscreen = true;
                    _renderer = glow;
                }
                else
                {
                    MeshFilter filter = source.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        return;
                    }

                    _mesh = MakeGlowMesh(filter.sharedMesh);
                    _object.AddComponent<MeshFilter>().sharedMesh = _mesh;
                    _renderer = _object.AddComponent<MeshRenderer>();
                }

                Material[] glowMaterials = new Material[sourceMaterials.Length];
                for (int i = 0; i < sourceMaterials.Length; ++i)
                {
                    Material original = sourceMaterials[i];
                    Material glow = new Material(shader);
                    glow.name = "MVS_FloraGlow";
                    glow.renderQueue = 3004;
                    glow.SetColor("_Color", Color.clear);

                    // The font shader reads texture alpha only, avoiding brown RGB tinting.
                    string textureProperty = original != null && original.HasProperty("_MainTex")
                        ? "_MainTex"
                        : (original != null && original.HasProperty("_BaseMap") ? "_BaseMap" : null);
                    if (textureProperty != null && original.GetTexture(textureProperty) != null)
                    {
                        glow.SetTexture("_MainTex", original.GetTexture(textureProperty));
                        glow.SetTextureScale("_MainTex", original.GetTextureScale(textureProperty));
                        glow.SetTextureOffset("_MainTex", original.GetTextureOffset(textureProperty));
                    }

                    glowMaterials[i] = glow;
                    _materials.Add(glow);
                }

                ConfigureRenderer(_renderer);
                _renderer.sharedMaterials = glowMaterials;
                _renderer.enabled = false;
            }

            private static Mesh MakeGlowMesh(Mesh source)
            {
                Mesh mesh = UnityEngine.Object.Instantiate(source);
                mesh.name = "MVS_FloraGlowMesh";
                if (!mesh.isReadable)
                {
                    return mesh;
                }

                Color32[] colors = new Color32[mesh.vertexCount];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = new Color32(255, 255, 255, 255);
                }

                mesh.colors32 = colors;
                return mesh;
            }

            public void Apply(Color color, float intensity, float strength)
            {
                if (!IsValid)
                {
                    return;
                }

                _renderer.enabled = _source.enabled && _source.gameObject.activeInHierarchy && strength > 0.01f;
                float alpha = Mathf.Lerp(0.35f, 0.70f, Mathf.InverseLerp(0.10f, 1.25f, intensity));
                color.a = alpha * Mathf.Clamp01(strength);
                for (int i = 0; i < _materials.Count; ++i)
                {
                    _materials[i].SetColor("_Color", color);
                }
            }

            public void Dispose()
            {
                if (_object != null)
                {
                    UnityEngine.Object.Destroy(_object);
                }

                for (int i = 0; i < _materials.Count; ++i)
                {
                    UnityEngine.Object.Destroy(_materials[i]);
                }

                _materials.Clear();
                if (_mesh != null)
                {
                    UnityEngine.Object.Destroy(_mesh);
                }
            }
        }

        private sealed class RendererState
        {
            private static readonly int ColorProperty = Shader.PropertyToID("_Color");
            private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
            private static readonly int SkinColorProperty = Shader.PropertyToID("_SkinColor");
            private static readonly int TintColorProperty = Shader.PropertyToID("_TintColor");
            private static readonly int MainColorProperty = Shader.PropertyToID("_MainColor");
            private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
            private static readonly int EmissiveColorProperty = Shader.PropertyToID("_EmissiveColor");

            private readonly Renderer _renderer;
            private readonly int _materialIndex;
            private readonly MaterialPropertyBlock _originalBlock;
            private readonly MaterialPropertyBlock _workingBlock;

            private readonly bool _hasColor;
            private readonly bool _hasBaseColor;
            private readonly bool _hasSkinColor;
            private readonly bool _hasTintColor;
            private readonly bool _hasMainColor;
            private readonly bool _hasEmission;
            private readonly bool _hasEmissive;

            private readonly Color _baseColor;
            private readonly Color _baseBaseColor;
            private readonly Color _baseSkinColor;
            private readonly Color _baseTintColor;
            private readonly Color _baseMainColor;
            private readonly Color _baseEmissionColor;
            private readonly Color _baseEmissiveColor;

            public bool CanHighlight
            {
                get
                {
                    return _renderer != null &&
                           (_hasColor ||
                            _hasBaseColor ||
                            _hasSkinColor ||
                            _hasTintColor ||
                            _hasMainColor ||
                            _hasEmission ||
                            _hasEmissive);
                }
            }

            public RendererState(Renderer renderer, Material material, int materialIndex)
            {
                _renderer = renderer;
                _materialIndex = materialIndex;
                _originalBlock = new MaterialPropertyBlock();
                _workingBlock = new MaterialPropertyBlock();

                renderer.GetPropertyBlock(_originalBlock, materialIndex);
                renderer.GetPropertyBlock(_workingBlock, materialIndex);

                _hasColor = material.HasProperty(ColorProperty);
                _hasBaseColor = material.HasProperty(BaseColorProperty);
                _hasSkinColor = material.HasProperty(SkinColorProperty);
                _hasTintColor = material.HasProperty(TintColorProperty);
                _hasMainColor = material.HasProperty(MainColorProperty);
                _hasEmission = material.HasProperty(EmissionColorProperty);
                _hasEmissive = material.HasProperty(EmissiveColorProperty);

                _baseColor = _hasColor ? material.GetColor(ColorProperty) : Color.white;
                _baseBaseColor = _hasBaseColor ? material.GetColor(BaseColorProperty) : Color.white;
                _baseSkinColor = _hasSkinColor ? material.GetColor(SkinColorProperty) : Color.white;
                _baseTintColor = _hasTintColor ? material.GetColor(TintColorProperty) : Color.white;
                _baseMainColor = _hasMainColor ? material.GetColor(MainColorProperty) : Color.white;
                _baseEmissionColor = _hasEmission ? material.GetColor(EmissionColorProperty) : Color.black;
                _baseEmissiveColor = _hasEmissive ? material.GetColor(EmissiveColorProperty) : Color.black;
            }

            public void Apply(Color highlightColor, float intensity, float strength)
            {
                if (_renderer == null)
                {
                    return;
                }

                float normalizedIntensity = Mathf.InverseLerp(0.10f, 1.25f, Mathf.Clamp(intensity, 0.10f, 1.25f));
                float tintStrength = Mathf.Clamp01(strength * Mathf.Lerp(0.38f, 0.68f, normalizedIntensity));
                float emissionStrength = Mathf.Clamp(strength * Mathf.Lerp(0.12f, 0.42f, normalizedIntensity), 0f, 0.48f);

                if (_hasColor)
                {
                    _workingBlock.SetColor(ColorProperty, Color.Lerp(_baseColor, highlightColor, tintStrength));
                }

                if (_hasBaseColor)
                {
                    _workingBlock.SetColor(BaseColorProperty, Color.Lerp(_baseBaseColor, highlightColor, tintStrength));
                }

                if (_hasSkinColor)
                {
                    _workingBlock.SetColor(SkinColorProperty, Color.Lerp(_baseSkinColor, highlightColor, tintStrength));
                }

                if (_hasTintColor)
                {
                    _workingBlock.SetColor(TintColorProperty, Color.Lerp(_baseTintColor, highlightColor, tintStrength));
                }

                if (_hasMainColor)
                {
                    _workingBlock.SetColor(MainColorProperty, Color.Lerp(_baseMainColor, highlightColor, tintStrength));
                }

                if (_hasEmission)
                {
                    Color emission = _baseEmissionColor + (highlightColor * emissionStrength);
                    emission.a = 1f;
                    _workingBlock.SetColor(EmissionColorProperty, emission);
                }

                if (_hasEmissive)
                {
                    Color emissive = _baseEmissiveColor + (highlightColor * emissionStrength);
                    emissive.a = 1f;
                    _workingBlock.SetColor(EmissiveColorProperty, emissive);
                }

                _renderer.SetPropertyBlock(_workingBlock, _materialIndex);
            }

            public void Restore()
            {
                if (_renderer != null)
                {
                    _renderer.SetPropertyBlock(_originalBlock, _materialIndex);
                }
            }
        }

        private sealed class ScanFillOverlay
        {
            private readonly GameObject _overlayObject;
            private readonly Renderer _overlayRenderer;
            private readonly Material _overlayMaterial;
            private readonly float _baseAlpha;
            private readonly float _peakAlpha;

            public bool IsValid
            {
                get
                {
                    return _overlayObject != null &&
                           _overlayRenderer != null &&
                           _overlayMaterial != null;
                }
            }

            public ScanFillOverlay(Renderer source, bool throughOccluders, float baseAlpha, float peakAlpha)
            {
                _baseAlpha = baseAlpha;
                _peakAlpha = peakAlpha;

                if (source == null)
                {
                    return;
                }

                Shader shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                if (shader == null)
                {
                    return;
                }

                _overlayMaterial = new Material(shader);
                _overlayMaterial.name = "MVS_ScanFill";
                _overlayMaterial.renderQueue = 3002;

                ConfigureMaterial(
                    _overlayMaterial,
                    UnityEngine.Rendering.CullMode.Off,
                    throughOccluders
                        ? UnityEngine.Rendering.CompareFunction.Always
                        : UnityEngine.Rendering.CompareFunction.LessEqual);

                _overlayObject = new GameObject("MVS_ScanFill");
                CopyTransformExact(source.transform, _overlayObject.transform);

                SkinnedMeshRenderer sourceSkinned = source as SkinnedMeshRenderer;
                if (sourceSkinned != null && sourceSkinned.sharedMesh != null)
                {
                    SkinnedMeshRenderer overlaySkinned = _overlayObject.AddComponent<SkinnedMeshRenderer>();
                    overlaySkinned.sharedMesh = sourceSkinned.sharedMesh;
                    overlaySkinned.rootBone = sourceSkinned.rootBone;
                    overlaySkinned.bones = sourceSkinned.bones;
                    overlaySkinned.localBounds = sourceSkinned.localBounds;
                    overlaySkinned.updateWhenOffscreen = true;
                    ConfigureRenderer(overlaySkinned);
                    overlaySkinned.sharedMaterials = MakeMaterialArray(sourceSkinned.sharedMaterials.Length, _overlayMaterial);
                    _overlayRenderer = overlaySkinned;
                    return;
                }

                MeshRenderer sourceMeshRenderer = source as MeshRenderer;
                if (sourceMeshRenderer != null)
                {
                    MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                    if (sourceFilter == null || sourceFilter.sharedMesh == null)
                    {
                        return;
                    }

                    MeshFilter overlayFilter = _overlayObject.AddComponent<MeshFilter>();
                    overlayFilter.sharedMesh = sourceFilter.sharedMesh;

                    MeshRenderer overlayMeshRenderer = _overlayObject.AddComponent<MeshRenderer>();
                    ConfigureRenderer(overlayMeshRenderer);
                    overlayMeshRenderer.sharedMaterials = MakeMaterialArray(sourceMeshRenderer.sharedMaterials.Length, _overlayMaterial);
                    _overlayRenderer = overlayMeshRenderer;
                }
            }

            public void Apply(Color color, float intensity, float strength)
            {
                if (!IsValid)
                {
                    return;
                }

                float intensityScale = Mathf.Lerp(
                    0.75f,
                    1.15f,
                    Mathf.InverseLerp(0.10f, 1.25f, Mathf.Clamp(intensity, 0.10f, 1.25f)));

                Color fillColor = color;
                fillColor.a = Mathf.Clamp01(Mathf.Lerp(_baseAlpha, _peakAlpha, strength) * intensityScale);

                if (_overlayMaterial.HasProperty("_Color"))
                {
                    _overlayMaterial.SetColor("_Color", fillColor);
                }

                _overlayRenderer.enabled = strength > 0.01f;
            }

            public void Dispose()
            {
                if (_overlayObject != null)
                {
                    UnityEngine.Object.Destroy(_overlayObject);
                }

                if (_overlayMaterial != null)
                {
                    UnityEngine.Object.Destroy(_overlayMaterial);
                }
            }
        }

        private sealed class OutlineShell
        {
            private readonly GameObject _outlineObject;
            private readonly Renderer _outlineRenderer;
            private readonly Material _outlineMaterial;

            public bool IsValid
            {
                get
                {
                    return _outlineObject != null &&
                           _outlineRenderer != null &&
                           _outlineMaterial != null;
                }
            }

            public OutlineShell(Renderer source, float outlineScale)
            {
                if (source == null)
                {
                    return;
                }

                Shader shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader == null)
                {
                    return;
                }

                _outlineMaterial = new Material(shader);
                _outlineMaterial.name = "MVS_Outline";
                _outlineMaterial.renderQueue = 3003;

                ConfigureMaterial(
                    _outlineMaterial,
                    UnityEngine.Rendering.CullMode.Front,
                    UnityEngine.Rendering.CompareFunction.LessEqual);

                _outlineObject = new GameObject("MVS_Outline");
                CopyTransformScaled(source.transform, _outlineObject.transform, outlineScale);

                SkinnedMeshRenderer sourceSkinned = source as SkinnedMeshRenderer;
                if (sourceSkinned != null && sourceSkinned.sharedMesh != null)
                {
                    SkinnedMeshRenderer outlineSkinned = _outlineObject.AddComponent<SkinnedMeshRenderer>();
                    outlineSkinned.sharedMesh = sourceSkinned.sharedMesh;
                    outlineSkinned.rootBone = sourceSkinned.rootBone;
                    outlineSkinned.bones = sourceSkinned.bones;
                    outlineSkinned.localBounds = sourceSkinned.localBounds;
                    outlineSkinned.updateWhenOffscreen = true;
                    ConfigureRenderer(outlineSkinned);
                    outlineSkinned.sharedMaterials = MakeMaterialArray(sourceSkinned.sharedMaterials.Length, _outlineMaterial);
                    _outlineRenderer = outlineSkinned;
                    return;
                }

                MeshRenderer sourceMeshRenderer = source as MeshRenderer;
                if (sourceMeshRenderer != null)
                {
                    MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                    if (sourceFilter == null || sourceFilter.sharedMesh == null)
                    {
                        return;
                    }

                    MeshFilter outlineFilter = _outlineObject.AddComponent<MeshFilter>();
                    outlineFilter.sharedMesh = sourceFilter.sharedMesh;

                    MeshRenderer outlineMeshRenderer = _outlineObject.AddComponent<MeshRenderer>();
                    ConfigureRenderer(outlineMeshRenderer);
                    outlineMeshRenderer.sharedMaterials = MakeMaterialArray(sourceMeshRenderer.sharedMaterials.Length, _outlineMaterial);
                    _outlineRenderer = outlineMeshRenderer;
                }
            }

            public void Apply(Color color, float intensity, float strength)
            {
                if (!IsValid)
                {
                    return;
                }

                Color outlineColor = color;
                float intensityScale = Mathf.Lerp(
                    0.80f,
                    1.20f,
                    Mathf.InverseLerp(0.10f, 1.25f, Mathf.Clamp(intensity, 0.10f, 1.25f)));

                outlineColor.a = Mathf.Clamp01((0.55f + strength * 0.40f) * intensityScale);

                if (_outlineMaterial.HasProperty("_Color"))
                {
                    _outlineMaterial.SetColor("_Color", outlineColor);
                }

                _outlineRenderer.enabled = strength > 0.01f;
            }

            public void Dispose()
            {
                if (_outlineObject != null)
                {
                    UnityEngine.Object.Destroy(_outlineObject);
                }

                if (_outlineMaterial != null)
                {
                    UnityEngine.Object.Destroy(_outlineMaterial);
                }
            }
        }

        private static void ConfigureMaterial(
            Material material,
            UnityEngine.Rendering.CullMode cull,
            UnityEngine.Rendering.CompareFunction zTest)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)cull);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_ZTest"))
            {
                material.SetInt("_ZTest", (int)zTest);
            }
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private static Material[] MakeMaterialArray(int count, Material material)
        {
            int materialCount = Mathf.Max(1, count);
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materialCount; ++i)
            {
                materials[i] = material;
            }

            return materials;
        }

        private static void CopyTransformExact(Transform source, Transform destination)
        {
            Transform parent = source.parent;
            if (parent != null)
            {
                destination.SetParent(parent, false);
                destination.localPosition = source.localPosition;
                destination.localRotation = source.localRotation;
                destination.localScale = source.localScale;
            }
            else
            {
                destination.position = source.position;
                destination.rotation = source.rotation;
                destination.localScale = source.lossyScale;
            }
        }

        private static void CopyTransformScaled(Transform source, Transform destination, float scaleMultiplier)
        {
            Transform parent = source.parent;
            if (parent != null)
            {
                destination.SetParent(parent, false);
                destination.localPosition = source.localPosition;
                destination.localRotation = source.localRotation;
                destination.localScale = source.localScale * scaleMultiplier;
            }
            else
            {
                destination.position = source.position;
                destination.rotation = source.rotation;
                destination.localScale = source.lossyScale * scaleMultiplier;
            }
        }

    }
}
