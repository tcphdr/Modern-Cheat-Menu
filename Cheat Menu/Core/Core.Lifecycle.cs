/*
 * Modern Cheat Menu
 * Core.Lifecycle.cs
 *
 * ---------- Commands To Implement ----------
 * setowned, setqueststate, setquestentrystate, setemotion, setunlocked, setrelationship, addemployee
 * setdiscovered
 * ---------- Function Ideas ----------
 * Make every ATM spit out cash of any quantity and dollar amount. (Call it make it rain)
 * Make everyone/person puke
 * Tase everyone/person
 * Arrest everyone/person
 * Give everyone/person wanted level
 * Spam throw cars at people/everyone
 * Control vehicles on the map, maybe remote control someone's.
 * Trash Tornado around player/everyone?
 * Freeze their inputs/character controls
 */

[assembly: MelonInfo(typeof(Modern_Cheat_Menu.Core), Modern_Cheat_Menu.ModInfo.Name, Modern_Cheat_Menu.ModInfo.Version, Modern_Cheat_Menu.ModInfo.Author, null)]
[assembly: MelonGame(Modern_Cheat_Menu.ModInfo.GameDevelopers, Modern_Cheat_Menu.ModInfo.NameOfGame)]
[assembly: HarmonyDontPatchAll]

namespace Modern_Cheat_Menu
{
    public partial class Core : MelonMod
    {
        // HarmonyLib initialization.
        private HarmonyLib.Harmony _harmony;

        // Add dictionary for text fields
        private Dictionary<string, CustomTextField> _textFields = new Dictionary<string, CustomTextField>();

        // Categories and commands
        private List<CommandCategory> _categories = new List<CommandCategory>();
        private Dictionary<string, string> _itemDictionary = new Dictionary<string, string>();
        private Dictionary<string, List<string>> _vehicleCache = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _itemCache = new Dictionary<string, List<string>>();
        private Dictionary<string, bool> _qualitySupportCache = new Dictionary<string, bool>();
        private Dictionary<string, List<string>> _itemQualityCache = new Dictionary<string, List<string>>();

        private Il2CppFishySteamworks.Server.ServerSocket _discoveredServerSocket;
        private MelonPreferences_Category _keybindCategory;
        private MelonPreferences_Entry<string> _menuToggleKeyEntry;
        private MelonPreferences_Entry<string> _explosionKeyEntry;
        private static bool _isCapturingKey = false;
        private MelonPreferences_Entry<string> _currentKeyCaptureEntry;

        // Static fields to be used across the class
        private static KeyCode _currentMenuToggleKey = KeyCode.F10;
        private static KeyCode _currentExplosionAtCrosshairKey = KeyCode.LeftAlt;

        private KeyCode CurrentMenuToggleKey => _currentMenuToggleKey;
        private KeyCode CurrentExplosionAtCrosshairKey => _currentExplosionAtCrosshairKey;

        private Vector2 _playerScrollPosition = Vector2.zero;

        private Vector2 _settingsScrollPosition = Vector2.zero;

        // UI settings
        private bool _uiVisible = false;
        private Rect _windowRect = new Rect(20, 20, 900, 650);
        private Vector2 _scrollPosition = Vector2.zero;
        private int _selectedCategoryIndex = 0;
        private float _fadeInProgress = 0f;
        private bool _isInitialized = false;
        private float _uiScale = 1.0f;
        private bool _showSettings = false;
        private bool _isDragging = false;
        private Vector2 _dragOffset;
        private bool _stylesInitialized = false;
        private bool _needsTextureRecreation = false;
        private bool _needsStyleRecreation = false;
        private MelonPreferences_Entry<float> _menuPosXEntry;
        private MelonPreferences_Entry<float> _menuPosYEntry;

        // Animation timers
        private float _menuAnimationTime = 0f;
        private Dictionary<string, float> _commandAnimations = new Dictionary<string, float>();
        private Dictionary<string, float> _buttonHoverAnimations = new Dictionary<string, float>();
        private Dictionary<string, float> _toggleAnimations = new Dictionary<string, float>();
        private Dictionary<string, Vector2> _itemGridAnimations = new Dictionary<string, Vector2>();

        // Player booleans & shit
        private static bool _staticPlayerGodmodeEnabled = false;
        private bool _playerGodmodeEnabled = false;
        private object _godModeCoroutine = null;
        private bool _playerNeverWantedEnabled = false;
        private object _neverWantedCoroutine = null;
        private static string _localPlayerName = ""; // Store the local player name for checking

        // New weapon cheat settings
        private bool _unlimitedAmmoEnabled = false;
        private object _unlimitedAmmoCoroutine = null;
        private object _perfectAccuracyCoroutine = null;
        private bool _aimbotEnabled = false;
        private object _aimbotCoroutine = null;
        private float _aimbotRange = 50f; // Maximum range to detect enemies
        private bool _autoFireEnabled = false;
        private float _autoFireDelay = 0.5f; // Delay between auto shots
        private bool _perfectAccuracyEnabled = false;
        private bool _noRecoilEnabled = false;
        private object _noRecoilCoroutine = null;
        private bool _oneHitKillEnabled = false;
        private bool _npcsPacifiedEnabled = false;
        private object _pacifyNPCsCoroutine = null;
        private bool _forceCrosshairAlwaysVisible = false;

        // Free camera settings
        private bool _freeCamEnabled = false;

        // IMGUI Styling
        private GUISkin _customSkin;
        private Texture2D _backgroundTexture;
        private Texture2D _panelTexture;
        private Texture2D _buttonNormalTexture;
        private Texture2D _buttonHoverTexture;
        private Texture2D _buttonActiveTexture;
        private Texture2D _toggleOnTexture;
        private Texture2D _toggleOffTexture;
        private Texture2D _sliderThumbTexture;
        private Texture2D _sliderTrackTexture;
        private Texture2D _inputFieldTexture;
        private Texture2D _headerTexture;
        private Texture2D _categoryTabTexture;
        private Texture2D _categoryTabActiveTexture;
        private Texture2D _checkmarkTexture;
        private Texture2D _settingsIconTexture;
        private Texture2D _closeIconTexture;
        private Texture2D _glowTexture;
        private GUIStyle _labelStyle;
        private Texture2D _warningTexture;

        // Styles
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _categoryButtonStyle;
        private GUIStyle _categoryButtonActiveStyle;
        private GUIStyle _commandLabelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _iconButtonStyle;
        private GUIStyle _toggleButtonStyle;
        private GUIStyle _toggleButtonActiveStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _inputFieldStyle;
        private GUIStyle _searchBoxStyle;
        private GUIStyle _tooltipStyle;
        private GUIStyle _itemButtonStyle;
        private GUIStyle _itemSelectedStyle;
        private GUIStyle _closeButtonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _separatorStyle;

        // Colors
        private Color _backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        private Color _panelColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        private Color _accentColor = new Color(0.15f, 0.55f, 0.95f, 1f); // Blue accent
        private Color _secondaryAccentColor = new Color(0.15f, 0.85f, 0.55f); // Green accent
        private Color _warningColor = new Color(0.95f, 0.55f, 0.15f); // Orange warning
        private Color _dangerColor = new Color(0.95f, 0.25f, 0.25f); // Red danger
        private Color _textColor = new Color(0.9f, 0.9f, 0.9f);
        private Color _dimTextColor = new Color(0.7f, 0.7f, 0.75f);
        private Color _headerColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        // Item manager state
        private string _itemSearchText = "";
        private Vector2 _itemScrollPosition = Vector2.zero;
        private int _itemsPerRow = 5;
        private int _selectedItemIndex = -1;
        private int _selectedQualityIndex = 4; // Default to Heavenly (4)
        private string _selectedItemId = "";
        private string _quantityInput = "1";
        private string _slotInput = "1";
        private float _timeScaleValue = 1.0f;
        private float _timeHours = 12.0f;
        private float _timeMinutes = 0.0f;
        private bool _showTooltip = false;
        private string _currentTooltip = "";
        private Vector2 _tooltipPosition;
        private float _tooltipTimer = 0f;
        private int _tooltipItemId = -1;

        // Settings
        private bool _enableBlur = true;
        private bool _enableAnimations = true;
        private bool _enableGlow = true;
        private bool _darkTheme = true;
        private float _uiOpacity = 0.95f;

        // Player Window
        public class OnlinePlayerInfo
        {
            public Il2CppScheduleOne.PlayerScripts.Player Player { get; set; }
            public string Name { get; set; }
            public string SteamID { get; set; }
            public string ServerBindAddress { get; set; }
            public string ClientAddress { get; set; }
            public PlayerHealth Health { get; set; }
            public bool IsLocal { get; set; }
            public bool ExplodeLoop { get; set; }
        }

        private List<OnlinePlayerInfo> _onlinePlayers = new List<OnlinePlayerInfo>();
        private float _lastPlayerRefreshTime = 0f;
        private const float PLAYER_REFRESH_INTERVAL = 7f; // Refresh every 7 seconds
        private NetworkObject _cachedPlayerObject = null;
        private Equippable_RangedWeapon _cachedWeapon = null;
        private float _lastWeaponCheckTime = 0f;
        private const float WEAPON_CACHE_INTERVAL = 1f; // Check weapon cache every second
        private string _packageType = "baggie";

        private GameObject mapTeleportObject;
        private Texture2D _mapTexture;
        private EMapRegion _hoveredRegion = EMapRegion.Downtown; // Default value
        private Dictionary<EMapRegion, Color> _regionColors = new Dictionary<EMapRegion, Color>();
        private Dictionary<EMapRegion, Rect> _regionRects = new Dictionary<EMapRegion, Rect>();
        private Dictionary<string, Vector3> _predefinedTeleports = new Dictionary<string, Vector3>();
        private Rect _mapRect;
        private Vector2 _lastClickPosition = Vector2.zero;
        private Vector2 _dragStartOffset;
        private Vector2 _dragStartPos;
        private Vector2 _mapPanOffset = Vector2.zero;
        private bool _mapInitialized = false;
        private bool _isCapturingMap = false;
        private bool _isDraggingMap = false;
        private float _mapZoom = 1.0f;
        private float _maxZoom = 2.5f;

        private void PatchMethod(Type targetType, string methodName, HarmonyMethod prefix)
        {
            try
            {
                // Get method with explicit flags for Il2Cpp methods
                var method = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    LoggerInstance.Error($"Method {methodName} not found!");
                    return;
                }
                _harmony.Patch(method, prefix: prefix);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error patching {methodName}: {ex.Message}");
            }
        }

        private NetworkObject FindPlayerNetworkObject()
        {
            // Use cached player object if recent
            if (_cachedPlayerObject != null &&
                Time.time - _lastWeaponCheckTime < WEAPON_CACHE_INTERVAL)
            {
                return _cachedPlayerObject;
            }

            // Reset cache time
            _lastWeaponCheckTime = Time.time;

            // Direct search for player NetworkObject
            var playerObjects = Resources.FindObjectsOfTypeAll<NetworkObject>()
            .Where(obj =>
            obj != null &&
            obj.gameObject != null &&
            (obj.gameObject.name.Contains("Player") ||
            obj.name.Contains("Player") ||
            obj.name.Contains(SystemInfo.deviceName)))
            .ToList();

            if (playerObjects.Count > 0)
            {
                _cachedPlayerObject = playerObjects[0];
                return _cachedPlayerObject;
            }

            LoggerInstance.Error("No player NetworkObject found!");
            return null;
        }

        private void ApplyLobbyPatch()
        {
            try
            {
                // Find the FishySteamworks transport
                var fishyTransports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();
                if (fishyTransports != null && fishyTransports.Length > 0)
                {
                    var fishyTransport = fishyTransports[0];
                    if (fishyTransport != null)
                    {
                        // Directly change the maximum clients value
                        LoggerInstance.Msg($"Current maximum clients: {fishyTransport._maximumClients}");
                        fishyTransport._maximumClients = 16; // Change to your desired value
                        LoggerInstance.Msg($"Changed maximum clients to: {fishyTransport._maximumClients}");

                        // Also modify server socket if available
                        if (fishyTransport._server != null)
                        {
                            fishyTransport._server._maximumClients = 16;
                            LoggerInstance.Msg("Also updated server socket maximum clients");
                        }

                        ShowNotification("Lobby Size", "Maximum players increased to 16", NotificationType.Success);
                    }
                }
                else
                {
                    LoggerInstance.Error("Could not find FishySteamworks transport");
                    ShowNotification("Lobby Size", "Failed to modify - transport not found", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error applying lobby size patch: {ex.Message}");
                ShowNotification("Lobby Size", "Failed to modify - " + ex.Message, NotificationType.Error);
            }
        }

        public override void OnInitializeMelon()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony($"{Modern_Cheat_Menu.ModInfo.ComName}");
                _harmony.PatchAll(typeof(Core).Assembly);

                InitializeKeybindConfig();
                InitializeSettingsSystem();
                InitializeHwidPatch();
                RegisterCommands();
                InitializePlayerMap();

                LoggerInstance.Msg($"{Modern_Cheat_Menu.ModInfo.Name} successfully initialized.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize mod: {ex.Message}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                LoggerInstance.Msg("Main scene loaded, initializing cheat menu.");
                _discoveredServerSocket = FindBestServerSocket();
                MelonCoroutines.Start(SetupUI());
            }
        }

        private IEnumerator SetupUI()
        {
            yield return new WaitForSeconds(1f);

            try
            {
                CreateTextures();
                CreateButtonTextures();
                ProtectUIResources();
                CacheGameItems();
                SubscribeToPlayerDeathEvent();

                _isInitialized = true;

                ShowNotification($"{ModInfo.Name} Loaded", $"Press {CurrentMenuToggleKey} to toggle menu visibility", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"UI SETUP FAILED: {ex}");
                _isInitialized = false;
                ShowNotification("Initialization Failed", ex.Message, NotificationType.Error);
            }
        }

        public override void OnUpdate()
        {
            if (!_isInitialized)
                return;

            // Toggle menu visibility
            if (Input.GetKeyDown(CurrentMenuToggleKey) && !_freeCamEnabled)
            {
                ToggleUI(!_uiVisible);
            }

            // Handle ESC key for exiting freecam
            if (_freeCamEnabled && Input.GetKeyDown(KeyCode.Escape))
            {
                // Disable freecam and restore normal controls
                _freeCamEnabled = false;
                togglePlayerControllable(true);
                ShowNotification("Free Camera", "Disabled", NotificationType.Info);
            }

            // Disable explosion key if we're in freecam mode as well as when the menu is open
            if (!((_freeCamEnabled) || (_uiVisible)))
            {
                if (Input.GetKeyDown(CurrentExplosionAtCrosshairKey))
                {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                    RaycastHit hit;
                    Vector3 explosionPosition;
                    if (Physics.Raycast(ray, out hit, 100f))
                    {
                        explosionPosition = hit.point;
                    }
                    else
                    {
                        explosionPosition = Camera.main.transform.position + Camera.main.transform.forward * 5f;
                    }
                    CreateServerSideExplosion(explosionPosition, 99999999999999f, 2f);
                }
            }

            // Update animations for menu
            if (_uiVisible)
            {
                // Menu animation
                _menuAnimationTime += Time.deltaTime * (_enableAnimations ? 1.0f : 10.0f);
                if (_menuAnimationTime > 1.0f)
                    _menuAnimationTime = 1.0f;

                // Update fade in animation
                _fadeInProgress += Time.deltaTime * 5f; // Adjust speed as needed
                if (_fadeInProgress > 1.0f)
                    _fadeInProgress = 1.0f;

                // Update tooltip timer
                if (_showTooltip)
                {
                    _tooltipTimer += Time.deltaTime;
                    if (_tooltipTimer > 0.5f) // Show tooltip after 0.5 sec hover
                    {
                        _showTooltip = true;
                    }
                }

                // Update button hover animations
                List<string> keysToRemove = new List<string>();
                foreach (var key in _buttonHoverAnimations.Keys)
                {
                    float value = _buttonHoverAnimations[key];
                    if (value > 0)
                    {
                        value -= Time.deltaTime * 4f;
                        if (value <= 0)
                        {
                            value = 0;
                            keysToRemove.Add(key);
                        }
                        _buttonHoverAnimations[key] = value;
                    }
                }

                // Clean up completed animations
                foreach (var key in keysToRemove)
                {
                    _buttonHoverAnimations.Remove(key);
                }

                // Update toggle animations
                keysToRemove.Clear();
                foreach (var key in _toggleAnimations.Keys)
                {
                    bool isOn = false;

                    // Determine if toggle is on based on key
                    if (key == "Godmode")
                        isOn = _playerGodmodeEnabled;
                    else if (key == "NeverWanted")
                        isOn = _playerNeverWantedEnabled;
                    else if (key == "FreeCamera")
                        isOn = _freeCamEnabled;

                    float targetValue = isOn ? 1.0f : 0.0f;
                    float currentValue = _toggleAnimations[key];

                    if (currentValue != targetValue)
                    {
                        if (isOn)
                            currentValue += Time.deltaTime * 4f;
                        else
                            currentValue -= Time.deltaTime * 4f;

                        currentValue = Mathf.Clamp01(currentValue);
                        _toggleAnimations[key] = currentValue;

                        if (currentValue == targetValue)
                            keysToRemove.Add(key);
                    }
                }

                // Clean up completed animations
                foreach (var key in keysToRemove)
                {
                    _toggleAnimations.Remove(key);
                }

                // Update notification animations
                UpdateNotifications();
            }
            else
            {
                // Reset menu animation when hidden
                _menuAnimationTime = 0f;
                _fadeInProgress = 0f;

                // Update notification animations even when menu is hidden
                UpdateNotifications();
            }

            // Key capture logic for settings
            if (_isCapturingKey && Input.anyKeyDown)
            {
                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        // Prevent capturing Escape or other special keys
                        if (key != KeyCode.Escape)
                        {
                            SaveKeybind(_currentKeyCaptureEntry, key);
                        }

                        _isCapturingKey = false;
                        _currentKeyCaptureEntry = null;
                        break;
                    }
                }
            }
        }

        private void InitializeSettingsSystem()
        {
            try
            {
                // Create or get the settings category
                _settingsCategory = MelonPreferences.CreateCategory("CheatMenu_Settings");

                // UI Settings
                _uiScaleEntry = _settingsCategory.CreateEntry("UIScale", 1.0f, "UI Scale", "Scale factor for the cheat menu UI");
                _uiOpacityEntry = _settingsCategory.CreateEntry("UIOpacity", 0.95f, "UI Opacity", "Opacity level for the cheat menu UI");
                _enableAnimationsEntry = _settingsCategory.CreateEntry("EnableAnimations", true, "Enable Animations", "Toggle animations in the cheat menu");
                _enableGlowEntry = _settingsCategory.CreateEntry("EnableGlow", true, "Enable Glow Effects", "Toggle glow effects in the cheat menu");
                _enableBlurEntry = _settingsCategory.CreateEntry("EnableBlur", true, "Enable Background Blur", "Toggle background blur in the cheat menu");
                _darkThemeEntry = _settingsCategory.CreateEntry("DarkTheme", true, "Dark Theme", "Use dark theme for the cheat menu");
                _menuPosXEntry = _settingsCategory.CreateEntry("MenuPosX", 20f, is_hidden: true);
                _menuPosYEntry = _settingsCategory.CreateEntry("MenuPosY", 20f, is_hidden: true);

                // Load all settings
                LoadSettings();

                LoggerInstance.Msg("Settings system initialized successfully.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing settings system: {ex.Message}");
                ShowNotification("Error", "Failed to initialize settings", NotificationType.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Load UI settings
                _uiScale = _uiScaleEntry.Value;
                _uiOpacity = _uiOpacityEntry.Value;
                _enableAnimations = _enableAnimationsEntry.Value;
                _enableGlow = _enableGlowEntry.Value;
                _enableBlur = _enableBlurEntry.Value;
                _darkTheme = _darkThemeEntry.Value;

                // Load window position - ADD THIS
                if (_menuPosXEntry != null && _menuPosYEntry != null)
                {
                    LoggerInstance.Msg($"Loading saved window position: X={_menuPosXEntry.Value}, Y={_menuPosYEntry.Value}");
                    _windowRect.x = _menuPosXEntry.Value;
                    _windowRect.y = _menuPosYEntry.Value;
                }
                else
                {
                    LoggerInstance.Error("Menu position entries are null! Cannot load position.");
                }

                // Apply screen bounds checking
                if (_windowRect.x < 0 || _windowRect.x > Screen.width - 100 || _windowRect.y < 0 || _windowRect.y > Screen.height - 100)
                {
                    // Log detailed debug information
                    LoggerInstance.Error($"Menu position out of bounds! Debug info:");
                    LoggerInstance.Error($"Screen resolution: {Screen.width}x{Screen.height}, DPI: {Screen.dpi}");
                    LoggerInstance.Error($"Saved position: X={_windowRect.x}, Y={_windowRect.y}, Width={_windowRect.width}, Height={_windowRect.height}");

                    // Reset to default (center of screen)
                    _windowRect.x = (Screen.width - _windowRect.width) / 2;
                    _windowRect.y = (Screen.height - _windowRect.height) / 2;

                    LoggerInstance.Msg($"Repositioned menu to center: X={_windowRect.x}, Y={_windowRect.y}");
                }
                UpdateKeybinds();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Save UI settings
                _uiScaleEntry.Value = _uiScale;
                _uiOpacityEntry.Value = _uiOpacity;
                _enableAnimationsEntry.Value = _enableAnimations;
                _enableGlowEntry.Value = _enableGlow;
                _enableBlurEntry.Value = _enableBlur;
                _darkThemeEntry.Value = _darkTheme;

                // Save all categories
                _settingsCategory.SaveToFile();
                _keybindCategory.SaveToFile();

                if (_menuPosXEntry != null && _menuPosYEntry != null)
                {
                    _menuPosXEntry.Value = _windowRect.x;
                    _menuPosYEntry.Value = _windowRect.y;
                }

                ShowNotification("Settings", "Settings saved successfully", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error saving settings: {ex.Message}");
                ShowNotification("Error", "Failed to save settings", NotificationType.Error);
            }
        }

        private unsafe void CacheGameItems()
        {
            try
            {
                // Create dictionaries to store discovered items
                var discoveredItems = new Dictionary<string, string>();
                var qualitySupportCache = new Dictionary<string, bool>();
                var qualityItemCache = new Dictionary<string, List<string>>();

                // Get the registry
                var registry = Registry.Instance;
                if (registry == null)
                {
                    LoggerInstance.Error("Registry instance is NULL!");
                    return;
                }

                // Get quality names for reference
                var qualityNames = Enum.GetNames(typeof(EQuality)).ToList();

                // Get ProductManager instance to access drug product definitions
                var productManager = ProductManager.Instance;
                if (productManager == null)
                {
                    LoggerInstance.Error("ProductManager instance is NULL!");
                }

                // Create a managed list to store all products
                var allProducts = new List<ProductDefinition>();
                if (productManager != null)
                {
                    // Add all products from different sources to our local list
                    // Handle Il2Cpp collections properly
                    if (productManager.AllProducts != null)
                    {
                        for (int i = 0; i < productManager.AllProducts.Count; i++)
                        {
                            allProducts.Add(productManager.AllProducts[i]);
                        }
                    }

                    if (ProductManager.DiscoveredProducts != null)
                    {
                        for (int i = 0; i < ProductManager.DiscoveredProducts.Count; i++)
                        {
                            allProducts.Add(ProductManager.DiscoveredProducts[i]);
                        }
                    }

                    if (productManager.DefaultKnownProducts != null)
                    {
                        for (int i = 0; i < productManager.DefaultKnownProducts.Count; i++)
                        {
                            allProducts.Add(productManager.DefaultKnownProducts[i]);
                        }
                    }

                    // Remove duplicates - using a dictionary to track unique items by ID
                    var uniqueProducts = new Dictionary<string, ProductDefinition>();
                    foreach (var product in allProducts)
                    {
                        if (!uniqueProducts.ContainsKey(product.ID))
                        {
                            uniqueProducts[product.ID] = product;
                        }
                    }

                    allProducts = uniqueProducts.Values.ToList();
                    LoggerInstance.Msg($"Found {allProducts.Count} product definitions from ProductManager");
                }

                // Enumerate all items in the registry
                foreach (var entry in registry.ItemDictionary)
                {
                    try
                    {
                        var itemDefinition = entry.Value.Definition;

                        // Skip null definitions
                        if (itemDefinition == null) continue;

                        // Try to get item ID and name
                        string itemId = itemDefinition.ID;
                        string itemName = itemDefinition.Name;

                        // Determine if thQis is a quality item
                        bool isQualityItem = false;
                        List<string> supportedQualities = null;

                        // Check 1: Explicit QualityItemDefinition type
                        var qualityDef = itemDefinition as QualityItemDefinition;
                        if (qualityDef != null)
                        {
                            isQualityItem = true;
                            supportedQualities = qualityNames;
                        }

                        // Check 2: Check if this is a drug product by comparing with ProductManager items
                        if (!isQualityItem && productManager != null)
                        {
                            // Try to find matching product in the product list
                            ProductDefinition matchingProduct = null;
                            foreach (var product in allProducts)
                            {
                                if (product.ID.Equals(itemId, StringComparison.OrdinalIgnoreCase) ||
                                    product.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                                {
                                    matchingProduct = product;
                                    break;
                                }
                            }

                            if (matchingProduct != null)
                            {
                                isQualityItem = true;

                                // Determine drug type and available qualities based on product type
                                string drugType = matchingProduct.DrugType.ToString();

                                // Log product properties if available
                                if (matchingProduct.Properties != null && matchingProduct.Properties.Count > 0)
                                {
                                    // Handle Il2Cpp List without using LINQ
                                    var propertyNames = new List<string>();
                                    for (int i = 0; i < matchingProduct.Properties.Count; i++)
                                    {
                                        var property = matchingProduct.Properties[i];
                                        propertyNames.Add(property.Name);
                                    }

                                }

                                // For now, we'll use all quality levels, but this could be refined based on the product type
                                supportedQualities = qualityNames;
                            }
                        }

                        if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(itemName))
                        {
                            // Add to main dictionary (display name => ID)
                            discoveredItems[itemName] = itemId;

                            // Track quality support
                            qualitySupportCache[itemId] = isQualityItem;

                            // If it's a quality item, cache quality levels
                            if (isQualityItem)
                            {
                                qualityItemCache[itemId] = supportedQualities ?? qualityNames;
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        LoggerInstance.Error($"Error processing item entry: {innerEx.Message}");
                    }
                }

                // Update class-level caches
                _itemDictionary = discoveredItems;
                _qualitySupportCache = qualitySupportCache;
                _itemQualityCache = qualityItemCache;

                // Prepare standard caches
                _itemCache["qualities"] = qualityNames;
                _itemCache["items"] = discoveredItems.Keys.OrderBy(k => k).ToList();
                _itemCache["slots"] = Enumerable.Range(1, 9).Select(x => x.ToString()).ToList();

                // Count quality items using standard .NET method
                int qualityItemCount = 0;
                foreach (var kvp in qualitySupportCache)
                {
                    if (kvp.Value) qualityItemCount++;
                }

                LoggerInstance.Msg($"Item Discovery Complete:");
                LoggerInstance.Msg($"- Total Items: {discoveredItems.Count}");
                LoggerInstance.Msg($"- Quality Items: {qualityItemCount}");

            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Critical error in item discovery: {ex}");
                ShowNotification("Error", "Failed to discover game items", NotificationType.Error);
            }
        }

        private void SubscribeToPlayerDeathEvent()
        {
            try
            {
                var localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    var playerHealth = GetPlayerHealth(localPlayer);
                    if (playerHealth != null && playerHealth.onDie != null)
                    {
                        // Create a Unity action that will be called when the player dies
                        playerHealth.onDie.AddListener(new Action(OnPlayerDeath));
                        LoggerInstance.Msg("Successfully subscribed to player death event");
                    }
                    else
                    {
                        LoggerInstance.Error("Player health or onDie event is null");
                    }
                }
                else
                {
                    LoggerInstance.Error("Local player not found for death event subscription");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to subscribe to player death event: {ex.Message}");
            }
        }

        private void OnPlayerDeath()
        {
            // Disable freecam on death.
            if (_freeCamEnabled)
            {
                togglePlayerControllable(true);
                _freeCamEnabled = false;
            }

            // Close the menu if it's open.
            if(_uiVisible)
            {
                ToggleUI(false);
            }

            _needsStyleRecreation = true;
            _needsStyleRecreation = true;
        }
    }
}
