
/*
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
    public class CustomTextField
    {
        private string _value;
        private GUIStyle _style;
        private bool _isFocused;
        private int _id;
        private static int _nextId = 1000;
        private float _lastInputTime;
        private const float INPUT_COOLDOWN = 0.1f;

        // Static field to track the currently focused text field
        private static CustomTextField _currentlyFocusedField = null;

        public string Value
        {
            get => _value;
            set => _value = value ?? "";
        }

        public CustomTextField(string initialValue = "", GUIStyle style = null)
        {
            _value = initialValue ?? "";
            _style = style ?? GUI.skin.textField;
            _id = _nextId++;
        }

        public string Draw(Rect position)
        {
            return Draw(position, _value, _style);
        }

        public string Draw(Rect position, string text, GUIStyle style)
        {
            Event current = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

            // Handle focus
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition))
                    {
                        // Unfocus any previously focused field
                        if (_currentlyFocusedField != null && _currentlyFocusedField != this)
                        {
                            _currentlyFocusedField._isFocused = false;
                        }

                        // Focus this field
                        GUIUtility.keyboardControl = controlID;
                        _isFocused = true;
                        _currentlyFocusedField = this;
                        current.Use();
                    }
                    else if (_isFocused)
                    {
                        // Clicked outside, unfocus this field
                        _isFocused = false;
                        if (_currentlyFocusedField == this)
                        {
                            _currentlyFocusedField = null;
                        }
                    }
                    break;

                case EventType.KeyDown:
                    if (_isFocused && GUIUtility.keyboardControl == controlID)
                    {
                        switch (current.keyCode)
                        {
                            case KeyCode.Backspace:
                                if (_value.Length > 0)
                                {
                                    _value = _value.Substring(0, _value.Length - 1);
                                    current.Use();
                                }
                                break;

                            case KeyCode.Return:
                            case KeyCode.KeypadEnter:
                            case KeyCode.Escape:
                                _isFocused = false;
                                GUIUtility.keyboardControl = 0;
                                _currentlyFocusedField = null;
                                current.Use();
                                break;
                        }
                    }
                    break;

                case EventType.Layout:
                    if (_isFocused && _currentlyFocusedField == this)
                    {
                        HandleTextInput(current);
                    }
                    break;
            }

            // Draw the field background
            GUI.Box(position, "", style);

            // Draw the text with cursor
            string displayText = _value;
            if (_isFocused && (Time.time % 1f) < 0.5f)
            {
                displayText += "|"; // Blinking cursor
            }

            GUI.Label(position, displayText, style);

            return _value;
        }

        private void HandleTextInput(Event current)
        {
            // Prevent rapid duplicate input
            if (current.character != '\0' &&
                !char.IsControl(current.character) &&
                Time.time - _lastInputTime > INPUT_COOLDOWN)
            {
                _value += current.character;
                _lastInputTime = Time.time;
                current.Use();
            }
        }

        public string DrawLayout(GUILayoutOption[] options = null)
        {
            Rect rect = GUILayoutUtility.GetRect(40, 20, options ?? new GUILayoutOption[0]);
            return Draw(rect);
        }

        public static implicit operator string(CustomTextField textField)
        {
            return textField.Value;
        }
    }

    //public class Core : MelonMod
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

        #region Settings System

        private MelonPreferences_Category _settingsCategory;
        private MelonPreferences_Entry<float> _uiScaleEntry;
        private MelonPreferences_Entry<float> _uiOpacityEntry;
        private MelonPreferences_Entry<bool> _enableAnimationsEntry;
        private MelonPreferences_Entry<bool> _enableGlowEntry;
        private MelonPreferences_Entry<bool> _enableBlurEntry;
        private MelonPreferences_Entry<bool> _darkThemeEntry;
        private Texture2D _settingsButtonTexture;
        private Texture2D _closeButtonTexture;

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

        // Method to start key capture for a specific keybind
        private void StartCaptureKeybind(MelonPreferences_Entry<string> keybindEntry)
        {
            _isCapturingKey = true;
            _currentKeyCaptureEntry = keybindEntry;
            ShowNotification("Keybind", "Press any key to set binding...", NotificationType.Info);
        }

        #endregion

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

        private void togglePlayerControllable(bool controllable)
        {
            try
            {
                // Toggle cursor state.
                if (controllable == false && _uiVisible == true)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }

                // Toggle camera look controls
                if (Il2CppScheduleOne.PlayerScripts.PlayerCamera.instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerCamera.instance.SetCanLook(controllable);
                }

                // Toggle player movement
                if (Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance.CanMove = controllable;
                }

                // Toggle input system
                if (Il2CppScheduleOne.GameInput.Instance != null &&
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput != null)
                {
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput.m_InputActive = controllable;
                }

                // Toggle inventory system
                //if(PlayerSingleton<PlayerInventory>.Instance != null)
                //{
                //    PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(controllable);
                //}
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error toggling player controls: {ex.Message}");
            }
        }

        private void ToggleUI(bool visible)
        {
            LoggerInstance.Msg($"Toggling UI visibility: {visible}");
            _uiVisible = visible;

            // Reset animation timers
            if (visible)
            {
                _fadeInProgress = 0f;
                _menuAnimationTime = 0f;

                // Ensure initial positioning starts from off-screen
                if (_windowRect.x <= -_windowRect.width)
                {
                    _windowRect.x = -_windowRect.width;
                }
                // Toggle player controls
                togglePlayerControllable(false);
            }
            else
            { 
                togglePlayerControllable(true);
            }
        }

        private void InitializeKeybindConfig()
        {
            try
            {
                // Create a category for keybinds
                _keybindCategory = MelonPreferences.CreateCategory("CheatMenu_Keybinds");

                // Create entries with default values
                _menuToggleKeyEntry = _keybindCategory.CreateEntry(
                    "MenuToggleKey",
                    KeyCode.F10.ToString(),
                    "Menu Toggle Key",
                    "Keybind to open/close the cheat menu"
                );

                _explosionKeyEntry = _keybindCategory.CreateEntry(
                    "ExplosionKey",
                    KeyCode.LeftAlt.ToString(),
                    "Explosion at Cursor Key",
                    "Keybind to create explosion at cursor"
                );

                // Load the saved keybinds
                UpdateKeybinds();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing keybind config: {ex.Message}");
            }
        }

        private void UpdateKeybinds()
        {
            try
            {
                // Parse and update the toggle menu key
                if (Enum.TryParse(_menuToggleKeyEntry.Value, out KeyCode menuToggleKey))
                {
                    _currentMenuToggleKey = menuToggleKey;
                    LoggerInstance.Msg($"Menu toggle key set to: {menuToggleKey}");
                }

                // Parse and update the explosion key
                if (Enum.TryParse(_explosionKeyEntry.Value, out KeyCode explosionKey))
                {
                    _currentExplosionAtCrosshairKey = explosionKey;
                    LoggerInstance.Msg($"Explosion key set to: {explosionKey}");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error updating keybinds: {ex.Message}");
            }
        }

        private void SaveKeybind(MelonPreferences_Entry<string> entry, KeyCode newKey)
        {
            try
            {
                entry.Value = newKey.ToString();
                _keybindCategory.SaveToFile();
                UpdateKeybinds();
                ShowNotification("Keybind", $"Key set to {newKey}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error saving keybind: {ex.Message}");
                ShowNotification("Error", "Failed to save keybind", NotificationType.Error);
            }
        }
        
        #region OnGUI and UI Drawing

        public override void OnGUI()
        {
            if (!_isInitialized)
                return;

            if (!_needsTextureRecreation && !_needsStyleRecreation && (_backgroundTexture == null || _customSkin == null))
            {
                _needsTextureRecreation = true;
                _needsStyleRecreation = true;
            }

            if (_needsTextureRecreation)
            {
                CreateTextures();
                CreateButtonTextures();
                ProtectUIResources();
                _needsStyleRecreation = true;
                _needsTextureRecreation = false;
            }

            if (_needsStyleRecreation)
            {
                InitializeStyles();
                ProtectUIResources();
                _stylesInitialized = true;
                _needsStyleRecreation = false;
            }

            if (_activeNotifications.Count > 0)
            {
                DrawNotifications();
            }

            if (_freeCamEnabled && !_uiVisible)
            {
                DrawFreecamOverlay();
            }

            if (!_uiVisible)
                return;

            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            GUI.skin = _customSkin;

            Color originalColor = GUI.color;
            GUI.color = new Color(1, 1, 1, _fadeInProgress);

            Matrix4x4 originalMatrix = GUI.matrix;
            if (_uiScale != 1.0f)
            {
                Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
                GUI.matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(_uiScale, _uiScale, 1)) * Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);
            }

            float menuAnim = Mathf.SmoothStep(0, 1, _menuAnimationTime);

            if (_windowRect.x <= -_windowRect.width)
            {
                _windowRect.x = Mathf.Lerp(-_windowRect.width, 20, menuAnim);
            }

            _windowRect = GUI.Window(0, _windowRect, DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawWindow), "", _windowStyle);

            if (_showTooltip && _tooltipTimer > 0.5f)
            {
                Vector2 mousePos = Event.current.mousePosition;
                float tooltipWidth = 250;
                float tooltipHeight = GUI.skin.box.CalcHeight(new GUIContent(_currentTooltip), tooltipWidth);

                float tooltipX = mousePos.x + 20;
                if (tooltipX + tooltipWidth > Screen.width)
                    tooltipX = Screen.width - tooltipWidth - 10;

                float tooltipY = mousePos.y + 20;
                if (tooltipY + tooltipHeight > Screen.height)
                    tooltipY = mousePos.y - tooltipHeight - 10;

                Rect tooltipRect = new Rect(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
                GUI.Box(tooltipRect, _currentTooltip, _tooltipStyle ?? GUI.skin.box);
            }

            GUI.matrix = originalMatrix;
            GUI.color = originalColor;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_showSettings)
                {
                    _showSettings = false;
                }
                else
                {
                    ToggleUI(false);
                }
                Event.current.Use();
            }
        }

        private void DrawHeaderWithTexturedButtons()
        {
            // Header background
            Rect headerRect = new Rect(0, 0, _windowRect.width, 40);
            GUI.Box(headerRect, "", _headerStyle ?? GUI.skin.box);

            // Title
            Rect titleRect = new Rect(headerRect.x + 10, headerRect.y, headerRect.width - 80, headerRect.height);
            GUI.Label(titleRect, ModInfo.Name, _titleStyle ?? GUI.skin.label);

            // Settings button - using custom texture
            Rect settingsRect = new Rect(headerRect.width - 70, headerRect.y + 5, 30, 30);
            if (_settingsButtonTexture != null && GUI.Button(settingsRect, _settingsButtonTexture, GUIStyle.none))
            {
                _showSettings = true;
            }

            // Close button - using custom texture
            Rect closeRect = new Rect(headerRect.width - 35, headerRect.y + 5, 30, 30);
            if (_closeButtonTexture != null && GUI.Button(closeRect, _closeButtonTexture, GUIStyle.none))
            {
                ToggleUI(false);
            }
        }

        private void DrawWindow(int windowId)
        {
            try
            {
                // Draw our custom header with textured buttons
                DrawHeaderWithTexturedButtons();

                // Main window vertical group - start below header
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.Space(40); // Space for header

                // Category Tabs
                GUILayout.BeginHorizontal();
                try
                {
                    for (int i = 0; i < _categories.Count; i++)
                    {
                        var style = i == _selectedCategoryIndex ?
                            _categoryButtonActiveStyle : _categoryButtonStyle;

                        if (GUILayout.Button(_categories[i].Name, style, GUILayout.ExpandWidth(true)))
                        {
                            _selectedCategoryIndex = i;
                            _scrollPosition = Vector2.zero;
                        }
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

                // Content Area
                GUILayout.BeginVertical(_panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                try
                {
                    if (_showSettings)
                    {
                        DrawSettingsPanel();
                    }
                    else
                    {
                        // Draw based on selected category
                        DrawSelectedCategory();
                    }
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                // Handle window dragging
                HandleWindowDragging();

                GUILayout.EndVertical(); // End main window vertical group
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Window draw error: {ex}");
                GUILayout.EndVertical();
            }
        }

        private void DrawSelectedCategory()
        {
            if (_selectedCategoryIndex < 0 || _selectedCategoryIndex >= _categories.Count)
                return;

            var category = _categories[_selectedCategoryIndex];

            // Different handling based on category name
            switch (category.Name)
            {
                case "Item Manager":
                    DrawItemManager();
                    break;
                case "Online":
                    DrawOnlinePlayers();
                    break;
                case "Teleport Manager":
                    DrawTeleportManager();
                    break;
                default:
                    DrawCommandCategory(category);
                    break;
            }
        }

        private bool _showPlayerMap = false;
        private Texture2D _playerMapTexture;
        private bool _playerMapInitialized = false;
        private float _playerMapZoom = 1.0f;
        private Vector2 _playerMapPanOffset = Vector2.zero;
        private bool _isDraggingPlayerMap = false;
        private Vector2 _playerMapDragStart;
        private Vector2 _playerMapDragStartOffset;
        private Dictionary<string, Color> _playerColors = new Dictionary<string, Color>();

        private void DrawOnlinePlayers()
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // Header with refresh button and map toggle
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh Players", _buttonStyle, GUILayout.Width(150), GUILayout.Height(30)))
                {
                    RefreshOnlinePlayers();
                    _lastPlayerRefreshTime = Time.time;
                    ShowNotification("Online", "Player list refreshed", NotificationType.Info);
                }

                // Map toggle button
                bool showMap = GUILayout.Toggle(_showPlayerMap, "Show Map", _toggleButtonStyle ?? GUI.skin.toggle, GUILayout.Width(100));
                if (showMap != _showPlayerMap)
                {
                    _showPlayerMap = showMap;
                    if (_showPlayerMap)
                        RefreshPlayerMapPositions();
                }

                // Total players count display
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Total Players: {_onlinePlayers.Count}", _labelStyle, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.Space(5); // Reduced spacing

                // Player Grid Layout
                // Check for refresh time
                if (Time.time - _lastPlayerRefreshTime > PLAYER_REFRESH_INTERVAL)
                {
                    RefreshOnlinePlayers();
                    _lastPlayerRefreshTime = Time.time;
                }

                // Grid layout settings
                float playerCardWidth = 210f;
                float playerCardHeight = 140f; // Smaller height for each player card
                int playersPerRow = Mathf.FloorToInt((windowWidth - 10) / (playerCardWidth + 10));
                playersPerRow = Mathf.Max(playersPerRow, 1); // Ensure at least 1 player per row

                // Calculate maximum height for player list area based on whether map is shown
                float playerListMaxHeight = _showPlayerMap ?
                    windowHeight * 0.4f : // Smaller when map is shown
                    windowHeight - 40f;   // Larger when map is hidden

                // Calculate how many rows we need
                int totalRows = Mathf.CeilToInt((float)_onlinePlayers.Count / playersPerRow);
                float contentHeight = totalRows * (playerCardHeight + 10);

                // Create player list scroll view
                _playerScrollPosition = GUILayout.BeginScrollView(
                    _playerScrollPosition,
                    GUILayout.Height(Mathf.Min(contentHeight + 10, playerListMaxHeight))
                );

                if (_onlinePlayers.Count == 0)
                {
                    GUILayout.Label("No players found. Try refreshing the list.", _labelStyle);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    int playerIndex = 0;

                    foreach (var playerInfo in _onlinePlayers)
                    {
                        if (playerInfo == null || playerInfo.Player == null)
                            continue;

                        // Start new row if needed
                        if (playerIndex > 0 && playerIndex % playersPerRow == 0)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                        }

                        // Player card
                        GUILayout.BeginVertical(GUILayout.Width(playerCardWidth), GUILayout.Height(playerCardHeight));

                        // Player card background
                        Rect cardRect = GUILayoutUtility.GetRect(playerCardWidth - 10, playerCardHeight - 10);
                        GUI.Box(cardRect, "", _panelStyle);

                        // Player name header with status indicator
                        Rect headerRect = new Rect(cardRect.x + 5, cardRect.y + 5, cardRect.width - 10, 25);
                        GUI.color = playerInfo.IsLocal ? new Color(0.2f, 0.7f, 1f) : Color.white;

                        GUIStyle nameStyle = new GUIStyle(_commandLabelStyle ?? _labelStyle);
                        nameStyle.fontStyle = FontStyle.Bold;
                        nameStyle.fontSize = 12;
                        nameStyle.alignment = TextAnchor.MiddleCenter;

                        string localTag = playerInfo.IsLocal ? " (YOU)" : "";
                        GUI.Label(headerRect, $"{playerInfo.Name}{localTag}", nameStyle);
                        GUI.color = Color.white;

                        // Status indicator
                        bool isAlive = playerInfo.Health != null && playerInfo.Health.IsAlive;
                        Color statusColor = isAlive ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
                        string statusText = isAlive ? "ALIVE" : "DEAD";

                        Rect statusRect = new Rect(cardRect.x + 5, cardRect.y + 30, cardRect.width - 10, 20);
                        GUIStyle statusStyle = new GUIStyle(_labelStyle);
                        statusStyle.normal.textColor = statusColor;
                        statusStyle.fontStyle = FontStyle.Bold;
                        statusStyle.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(statusRect, statusText, statusStyle);

                        // Health bar if available
                        if (playerInfo.Health != null)
                        {
                            Rect healthLabelRect = new Rect(cardRect.x + 5, cardRect.y + 50, 50, 20);
                            GUI.Label(healthLabelRect, "Health:", _labelStyle);

                            // Slimmer health bar container (reduced from 10px to 6px height)
                            Rect healthBarRect = new Rect(cardRect.x + 60, cardRect.y + 57, cardRect.width - 70, 6); // Height reduced by 40%

                            // 1. Draw subtle 1px border
                            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark gray border
                            GUI.DrawTexture(new Rect(
                                healthBarRect.x - 1,
                                healthBarRect.y - 1,
                                healthBarRect.width + 2,
                                healthBarRect.height + 2
                            ), Texture2D.whiteTexture);

                            // 2. Draw background
                            GUI.color = new Color(0.25f, 0.25f, 0.25f, 1f); // Medium gray background
                            GUI.DrawTexture(healthBarRect, Texture2D.whiteTexture);

                            // 3. Draw slim health fill (4px height = 66% of container)
                            float healthPercent = playerInfo.Health.CurrentHealth / (float)PlayerHealth.MAX_HEALTH;
                            Rect fillRect = new Rect(
                                healthBarRect.x,
                                healthBarRect.y + 1, // Center vertically
                                healthBarRect.width * healthPercent,
                                healthBarRect.height - 2 // Leaves 1px margin top/bottom
                            );

                            Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
                            GUI.color = healthColor;
                            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                            GUI.color = Color.white;
                        }

                        // Player ID 
                        GUIStyle smallTextStyle = new GUIStyle(_labelStyle);
                        smallTextStyle.fontSize = 12;
                        smallTextStyle.alignment = TextAnchor.MiddleCenter;

                        string steamId = playerInfo.SteamID;

                        Rect idRect = new Rect(cardRect.x + 5, cardRect.y + 75, cardRect.width - 10, 15);
                        GUI.Label(idRect, $"Steam ID: {steamId}", smallTextStyle);

                        // Action buttons - only if not local player
                        if (!playerInfo.IsLocal)
                        {
                            float buttonWidth = (cardRect.width - 20) / 2;
                            float buttonY = cardRect.y + 90;

                            // First row of buttons
                            if (GUI.Button(new Rect(cardRect.x + 5, buttonY, buttonWidth, 20), "Kill", _buttonStyle))
                            {
                                ServerExecuteKillPlayer(playerInfo.Player);
                                ShowNotification("Player", $"Killed {playerInfo.Name}", NotificationType.Success);
                            }

                            if (GUI.Button(new Rect(cardRect.x + 10 + buttonWidth, buttonY, buttonWidth, 20), "Explode", _buttonStyle))
                            {
                                CreateServerSideExplosion(playerInfo.Player.transform.position, 100f, 5f);
                                ShowNotification("Player", $"Exploded {playerInfo.Name}", NotificationType.Success);
                            }

                            // Second row - Teleport and Explosion Loop
                            float button2Y = buttonY + 25;
                            if (GUI.Button(new Rect(cardRect.x + 5, button2Y, buttonWidth, 20), "Teleport To", _buttonStyle))
                            {
                                TeleportPlayer(playerInfo.Player.transform.position);
                                ShowNotification("Player", $"Teleported to {playerInfo.Name}", NotificationType.Success);
                            }

                            bool newLoopState = GUI.Toggle(
                                new Rect(cardRect.x + 10 + buttonWidth, button2Y, buttonWidth, 20),
                                playerInfo.ExplodeLoop,
                                "Loop",
                                _toggleButtonStyle ?? GUI.skin.toggle
                            );

                            if (newLoopState != playerInfo.ExplodeLoop)
                            {
                                playerInfo.ExplodeLoop = newLoopState;
                                if (newLoopState)
                                {
                                    StartExplodeLoop(playerInfo);
                                    ShowNotification("Player", $"Started explosion loop on {playerInfo.Name}", NotificationType.Warning);
                                }
                            }
                        }

                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                        playerIndex++;
                    }

                    // Fill empty slots in the last row for better alignment
                    for (int i = 0; i < playersPerRow - (_onlinePlayers.Count % playersPerRow); i++)
                    {
                        if (_onlinePlayers.Count % playersPerRow == 0) break;
                        GUILayout.BeginVertical(GUILayout.Width(playerCardWidth), GUILayout.Height(playerCardHeight));
                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                // Draw player map as a separate element if enabled
                if (_showPlayerMap)
                {
                    GUILayout.Space(5); // Minimal spacing between player list and map

                    // Add a titled header for the map section
                    GUILayout.BeginVertical();
                    GUIStyle mapTitleStyle = new GUIStyle(_titleStyle ?? _labelStyle);
                    mapTitleStyle.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("Player Positions", mapTitleStyle);

                    // Fixed height for map
                    float mapHeight = 200f;
                    Rect mapContainerRect = GUILayoutUtility.GetRect(_windowRect.width - 40, mapHeight);
                    GUI.Box(mapContainerRect, "", _panelStyle);

                    // Draw the map with proper integration
                    DrawPlayerPositionsMap(mapContainerRect);

                    GUILayout.EndVertical();
                }

                GUILayout.Space(5); // Reduced spacing

                // Global actions row at the bottom - always visible
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Kill All", _buttonStyle, GUILayout.Height(30)))
                {
                    KillAllPlayersCommand(null);
                }

                if (GUILayout.Button("Explode All", _buttonStyle, GUILayout.Height(30)))
                {
                    CreateExplosion(new string[] { "all", "99999999999999", "2" });
                }

                if (GUILayout.Button("Teleport All To Me", _buttonStyle, GUILayout.Height(30)))
                {
                    TeleportAllPlayersToMe();
                }

                if (GUILayout.Button("Increase Lobby Size", _buttonStyle, GUILayout.Height(30)))
                {
                    ApplyLobbyPatch();
                }

                GUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawOnlinePlayers: {ex.Message}");
            }
        }

        // Generate a unique color based on player ID
        private Color GetUniqueColor(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return Color.gray;

            // If the player is local, use green
            var localPlayer = FindLocalPlayer();
            if (localPlayer != null && localPlayer.name.Contains(identifier))
                return new Color(0.2f, 0.8f, 0.2f); // Green

            // Hash the identifier to create a consistent color
            int hash = 0;
            foreach (char c in identifier)
            {
                hash = (hash * 31) + c;
            }

            // Use the hash to create a HSV color with consistent saturation and value
            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.8f, 0.8f);
        }

        private void SaveWindowPosition()
        {
            try
            {
                if (_menuPosXEntry == null || _menuPosYEntry == null)
                {
                    LoggerInstance.Error("Menu position entries are null! Cannot save position.");
                    return;
                }

                // Set the values
                _menuPosXEntry.Value = _windowRect.x;
                _menuPosYEntry.Value = _windowRect.y;

                // Save to file
                _settingsCategory.SaveToFile(false); // Pass false to prevent triggering the OnSaved event

                LoggerInstance.Msg($"Window position saved: X={_windowRect.x}, Y={_windowRect.y}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to save window position: {ex.Message}");
                LoggerInstance.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        private void HandleWindowDragging()
        {
            try
            {
                // Make the drag area just the header section
                Rect dragRect = new Rect(0, 0, _windowRect.width, 40);

                // Current event
                Event current = Event.current;

                // Start dragging
                if (current.type == EventType.MouseDown &&
                    current.button == 0 &&
                    dragRect.Contains(current.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = current.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                    current.Use();
                }
                // End dragging
                else if (_isDragging && current.type == EventType.MouseUp)
                {
                    _isDragging = false;

                    // Save position
                    SaveWindowPosition();

                    current.Use();
                }
                // Handle dragging - simplified approach
                else if (_isDragging)
                {
                    // Update position regardless of event type while dragging is active
                    // This provides smoother dragging by updating on every frame
                    _windowRect.x = current.mousePosition.x - _dragOffset.x;
                    _windowRect.y = current.mousePosition.y - _dragOffset.y;

                    // Keep window on screen
                    _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
                    _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

                    // Force repaint
                    GUI.changed = true;

                    // Only consume the event for mouse movement
                    if (current.type == EventType.MouseDrag)
                    {
                        current.Use();
                    }

                    // Check if mouse button is released outside normal events
                    if (!Input.GetMouseButton(0))
                    {
                        _isDragging = false;

                        // Save position when drag ends
                        _menuPosXEntry.Value = _windowRect.x;
                        _menuPosYEntry.Value = _windowRect.y;
                        _settingsCategory.SaveToFile();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Window dragging error: {ex.Message}");
                _isDragging = false;
            }
        }

        // Enhanced socket finding method
        private Il2CppFishySteamworks.Server.ServerSocket FindBestServerSocket()
        {
            try
            {
                var transports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();

                if (transports != null && transports.Length > 0)
                {
                    LoggerInstance.Msg($"Found {transports.Length} FishySteamworks transports");
                    return transports[0]._server;
                }
                else
                {
                    LoggerInstance.Error("Could not find FishySteamworks transport");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error finding server socket: {ex.Message}");
                return null;
            }
        }

        private void RefreshOnlinePlayers()
        {
            try
            {
                _onlinePlayers.Clear();
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;

                if (playerList == null || playerList.Count == 0)
                    return;

                //LoggerInstance.Msg($"Total players in list: {playerList.Count}");

                // Get FishySteamworks transport instance
                var fishyTransport = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>().FirstOrDefault();
                var serverSocket = fishyTransport?._server;

                // Create a mapping of connection IDs to Steam IDs
                Dictionary<int, string> connIdToSteamId = new Dictionary<int, string>();

                // Extract Steam ID mappings from the server socket
                if (serverSocket != null && serverSocket._steamIds != null)
                {
                    try
                    {
                        var steamIds = serverSocket._steamIds.First;
                        if (steamIds != null)
                        {
                            var getEnumerator = steamIds.GetType().GetMethod("GetEnumerator");
                            if (getEnumerator != null)
                            {
                                var enumerator = getEnumerator.Invoke(steamIds, null);
                                if (enumerator != null)
                                {
                                    var moveNext = enumerator.GetType().GetMethod("MoveNext");
                                    var current = enumerator.GetType().GetProperty("Current");

                                    if (moveNext != null && current != null)
                                    {
                                        while ((bool)moveNext.Invoke(enumerator, null))
                                        {
                                            var kvp = current.GetValue(enumerator);
                                            if (kvp != null)
                                            {
                                                var key = kvp.GetType().GetProperty("Key")?.GetValue(kvp);
                                                var value = kvp.GetType().GetProperty("Value")?.GetValue(kvp);

                                                if (key != null && value != null)
                                                {
                                                    string steamId = key.ToString();
                                                    int connId = Convert.ToInt32(value);
                                                    connIdToSteamId[connId] = steamId;
                                                    //LoggerInstance.Msg($"Mapped Connection ID {connId} to Steam ID {steamId}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error mapping connections to Steam IDs: {ex.Message}");
                    }
                }

                foreach (var player in playerList)
                {
                    if (player == null) continue;

                    bool isLocal = IsLocalPlayer(player);
                    var health = GetPlayerHealth(player);

                    // Extract Steam ID from name
                    string steamId = "Unknown";
                    string playerName = player.name;
                    int steamIdStart = playerName.IndexOf('(');
                    int steamIdEnd = playerName.IndexOf(')');

                    if (steamIdStart != -1 && steamIdEnd != -1)
                    {
                        steamId = playerName.Substring(steamIdStart + 1, steamIdEnd - steamIdStart - 1);
                    }

                    // Get network object for additional information
                    var netObj = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();

                    string networkInfo = "Unknown";
                    string ipAddress = "Unknown";

                    if (netObj != null)
                    {
                        networkInfo = $"Owner ID: {netObj.OwnerId}, Spawned: {netObj.IsSpawned}, Is Owner: {netObj.IsOwner}";

                        // For local player, set address to "Local Player"
                        if (isLocal)
                        {
                            ipAddress = "Local Player";
                        }
                        else
                        {
                            // For remote players, set the address to the Steam ID from the mapping
                            int ownerId = (int)netObj.OwnerId;
                            if (connIdToSteamId.ContainsKey(ownerId))
                            {
                                ipAddress = $"Steam ID: {connIdToSteamId[ownerId]}";
                            }
                            else
                            {
                                // If no mapping, use the Steam ID from the player name
                                ipAddress = $"Steam ID: {steamId}";
                            }
                        }
                    }

                    var playerInfo = new OnlinePlayerInfo
                    {
                        Player = player,
                        Name = playerName.Split('(')[0].Trim(),
                        SteamID = steamId,
                        ClientAddress = ipAddress,
                        Health = health,
                        IsLocal = isLocal,
                        ExplodeLoop = false
                    };

                    _onlinePlayers.Add(playerInfo);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error refreshing online players: {ex.Message}");
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

        // Add this method to your menu initialization
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

        private void DrawCommandCategory(CommandCategory category)
        {
            try
            {
                // Special handling for Player Exploits category
                if (category.Name == "Player Exploits")
                {
                    DrawPlayerExploitsUI(category);
                    return;
                }

                // Original implementation for other categories
                float windowWidth = _windowRect.width - 40f;

                // Manual scroll view setup
                _scrollPosition = GUI.BeginScrollView(
                    new Rect(20, 100, windowWidth, _windowRect.height - 150),
                    _scrollPosition,
                    new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f)
                );

                float yOffset = 0f;
                foreach (var command in category.Commands)
                {
                    // Command container rectangle
                    Rect commandRect = new Rect(0, yOffset, windowWidth - 40f, 90f);
                    GUI.Box(commandRect, "", _panelStyle);

                    // Command Name
                    GUI.Label(
                        new Rect(commandRect.x + 10f, commandRect.y + 5f, 200f, 25f),
                        command.Name,
                        _commandLabelStyle ?? _labelStyle
                    );

                    // Parameters handling
                    float paramX = commandRect.x + 220f;
                    if (command.Parameters.Count > 0)
                    {
                        foreach (var param in command.Parameters)
                        {
                            Rect paramRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                            if (param.Type == ParameterType.Input)
                            {
                                // Unique key for each parameter
                                string paramKey = $"param_{command.Name}_{param.Name}";

                                // Custom text field
                                if (!_textFields.TryGetValue(paramKey, out var textField))
                                {
                                    textField = new CustomTextField(param.Value ?? "", _inputFieldStyle ?? GUI.skin.textField);
                                    _textFields[paramKey] = textField;
                                }

                                param.Value = textField.Draw(paramRect);
                            }
                            else if (param.Type == ParameterType.Dropdown)
                            {
                                // Dropdown-like button
                                if (GUI.Button(paramRect, param.Value ?? "Select", _buttonStyle))
                                {
                                    ShowDropdownMenu(param);
                                }
                            }

                            paramX += 130f;
                        }
                    }

                    // Execute Button
                    Rect executeRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);
                    if (GUI.Button(executeRect, "Execute", _buttonStyle))
                    {
                        ExecuteCommand(command);
                    }

                    // Optional description
                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        GUI.Label(
                            new Rect(commandRect.x + 10f, commandRect.y + 35f, windowWidth - 60f, 50f),
                            command.Description,
                            _tooltipStyle ?? GUI.skin.label
                        );
                    }

                    // Increment Y offset for next command
                    yOffset += 100f;
                }

                GUI.EndScrollView();
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawCommandCategory: {ex}");
            }
        }

        private void DrawItemManager()
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // Vertical offset to move everything down
                float verticalOffset = 90f;

                // Add a header panel for the options
                Rect headerRect = new Rect(20f, 20f + verticalOffset, windowWidth - 40f, 50f);
                GUI.Box(headerRect, "", _panelStyle);

                // Center and space out the buttons in the header
                float buttonWidth = 120f;
                float buttonHeight = 30f;
                float buttonSpacing = 20f;
                float startX = headerRect.x + (headerRect.width - (buttonWidth * 3 + buttonSpacing * 2)) / 2;

                // Set Quality Button (first in the row)
                Rect setQualityRect = new Rect(
                    startX,
                    headerRect.y + (headerRect.height - buttonHeight) / 2,
                    buttonWidth,
                    buttonHeight
                );

                if (!_itemCache.TryGetValue("qualities", out var qualities)) return;
                if (GUI.Button(setQualityRect, "Set Quality", _buttonStyle))
                {
                    // Convert selected quality index to enum value
                    var quality = (Il2CppScheduleOne.ItemFramework.EQuality)_selectedQualityIndex;
                    SetItemQuality(quality);
                }

                // Package Item Button (second in the row)
                Rect packageItemRect = new Rect(
                    setQualityRect.x + buttonWidth + buttonSpacing,
                    setQualityRect.y,
                    buttonWidth,
                    buttonHeight
                );

                if (GUI.Button(packageItemRect, "Package Item", _buttonStyle))
                {
                    PackageProductCommand(_packageType);   
                }

                // Package Type Button (third in the row)
                Rect packageTypeRect = new Rect(
                    packageItemRect.x + buttonWidth + buttonSpacing,
                    packageItemRect.y,
                    buttonWidth,
                    buttonHeight
                );

                // Toggle between baggie/jar on click
                string packageTypeText = _packageType == "baggie" ? "Type: Baggie" : "Type: Jar";
                if (GUI.Button(packageTypeRect, packageTypeText, _buttonStyle))
                {
                    _packageType = _packageType == "baggie" ? "jar" : "baggie";
                    ShowNotification("Package Type", $"Set to {_packageType}", NotificationType.Info);
                }

                // Search Bar - moved down for better spacing
                float searchBarY = headerRect.y + headerRect.height + 20f;
                Rect searchRect = new Rect(20f, searchBarY, windowWidth - 40f, 30f);
                GUI.Label(new Rect(searchRect.x, searchRect.y - 25f, 100f, 25f), "Search:", _labelStyle);

                // Use custom text field for search
                if (!_textFields.TryGetValue("itemSearch", out var searchField))
                {
                    searchField = new CustomTextField(_itemSearchText, _searchBoxStyle ?? GUI.skin.textField);
                    _textFields["itemSearch"] = searchField;
                }
                _itemSearchText = searchField.Draw(searchRect);

                // Item Grid Scroll View - adjusted position to account for new header
                float scrollViewY = searchRect.y + searchRect.height + 20f;
                Rect scrollViewRect = new Rect(
                    20f,
                    scrollViewY,
                    windowWidth - 40f,
                    windowHeight - (scrollViewY - verticalOffset) - 120f // Adjust height to fit
                );

                // Calculate dynamic content height based on filtered items
                if (!_itemCache.TryGetValue("items", out var allItems)) return;

                var filteredItems = allItems
                    .Where(item => string.IsNullOrEmpty(_itemSearchText) ||
                                   item.IndexOf(_itemSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                const float itemSize = 100f;
                const float spacing = 25;
                int columns = Mathf.FloorToInt((scrollViewRect.width - spacing) / (itemSize + spacing));
                columns = Mathf.Max(columns, 5); // Force 5 columns

                // Calculate total rows and content height
                int totalRows = Mathf.CeilToInt((float)filteredItems.Count / columns);
                float contentHeight = totalRows * (itemSize + spacing) + spacing;

                // Create content rect with calculated height
                Rect contentRect = new Rect(0f, 0f, scrollViewRect.width - 20f, contentHeight);

                // Begin scroll view with dynamic content height
                _itemScrollPosition = GUI.BeginScrollView(
                    scrollViewRect,
                    _itemScrollPosition,
                    contentRect,
                    false,  // Horizontal scrolling
                    true    // Vertical scrolling
                );

                float yPos = 0f;
                for (int i = 0; i < filteredItems.Count; i += columns)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        int index = i + j;
                        if (index >= filteredItems.Count) break;

                        Rect itemRect = new Rect(
                            j * (itemSize + spacing),
                            yPos,
                            itemSize,
                            itemSize
                        );

                        string itemName = filteredItems[index];
                        DrawItemButton(itemName, itemRect);
                    }

                    yPos += itemSize + spacing;
                }

                GUI.EndScrollView();

                // Item Details Panel
                if (!string.IsNullOrEmpty(_selectedItemId))
                {
                    Rect detailsRect = new Rect(
                        20f,
                        windowHeight - 100f + verticalOffset,
                        windowWidth - 40f,
                        90f
                    );

                    GUI.Box(detailsRect, "", _panelStyle);

                    // Selected Item Label
                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 10f, 200f, 25f),
                        "Selected Item: " + GetDisplayNameFromId(_selectedItemId),
                        _labelStyle
                    );

                    // Quantity Input
                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 40f, 100f, 25f),
                        "Quantity:",
                        _labelStyle
                    );

                    Rect quantityRect = new Rect(detailsRect.x + 120f, detailsRect.y + 40f, 100f, 25f);
                    if (!_textFields.TryGetValue("quantityInput", out var quantityField))
                    {
                        quantityField = new CustomTextField(_quantityInput, _inputFieldStyle ?? GUI.skin.textField);
                        _textFields["quantityInput"] = quantityField;
                    }
                    _quantityInput = quantityField.Draw(quantityRect);

                    int quantity = 1;
                    if (!int.TryParse(_quantityInput, out quantity))
                    {
                        // Invalid input - reset to default
                        quantity = 1;
                        _quantityInput = "1";
                    }

                    // Ensure minimum quantity of 1
                    quantity = Math.Max(quantity, 1);

                    // Slot Input
                    GUI.Label(
                        new Rect(detailsRect.x + 250f, detailsRect.y + 40f, 100f, 25f),
                        "Slot:",
                        _labelStyle
                    );

                    Rect slotRect = new Rect(detailsRect.x + 350f, detailsRect.y + 40f, 100f, 25f);
                    if (!_textFields.TryGetValue("slotInput", out var slotField))
                    {
                        slotField = new CustomTextField(_slotInput, _inputFieldStyle ?? GUI.skin.textField);
                        _textFields["slotInput"] = slotField;
                    }
                    _slotInput = slotField.Draw(slotRect);

                    // Quality Selection
                    if (qualities != null)
                    {
                        float qualityX = detailsRect.x + 10f;
                        for (int i = 0; i < qualities.Count; i++)
                        {
                            Rect qualityRect = new Rect(qualityX, detailsRect.y + 70f, 80f, 25f);

                            var style = i == _selectedQualityIndex ? _itemSelectedStyle : _itemButtonStyle;
                            if (GUI.Button(qualityRect, qualities[i], style ?? _buttonStyle))
                            {
                                _selectedQualityIndex = i;
                            }

                            qualityX += 90f;
                        }
                    }

                    // Spawn Button - Now we have plenty of space for it
                    Rect spawnRect = new Rect(
                        detailsRect.x + detailsRect.width - 130f,
                        detailsRect.y + detailsRect.height - 40f,
                        120f,
                        30f
                    );

                    if (GUI.Button(spawnRect, "Spawn Item", _buttonStyle))
                    {
                        // Get the quantity
                        if (!int.TryParse(_quantityInput, out quantity) || quantity < 1)
                        {
                            quantity = 1;
                        }

                        // Convert selected quality index to enum value
                        var quality = (Il2CppScheduleOne.ItemFramework.EQuality)_selectedQualityIndex;

                        // Use the console command approach
                        SpawnItemViaConsole(_selectedItemId, quantity, quality);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawItemManager: {ex}");
            }
        }
        
        private string GetDisplayNameFromId(string itemId)
        {
            // Find the display name that corresponds to the item ID
            foreach (var item in _itemDictionary)
            {
                if (item.Value == itemId)
                {
                    return item.Key;
                }
            }
            return itemId; // Fallback to ID if no display name found
        }

        private void DrawItemButton(string itemName, Rect buttonRect)
        {
            if (string.IsNullOrEmpty(itemName) || !_itemDictionary.ContainsKey(itemName))
                return;

            // Get the item ID for this display name
            string itemId = _itemDictionary[itemName];

            bool isSelected = _selectedItemId == itemId;
            // Ensure consistent item size with some padding
            var style = isSelected ? _itemSelectedStyle : _itemButtonStyle;
            style = style ?? _buttonStyle;

            // Adjust style to ensure text is centered and doesn't overflow
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;

            if (GUI.Button(buttonRect, itemName, style))
            {
                // Set the selected item ID when clicked
                _selectedItemId = itemId;

                // Reset quantity and slot to defaults
                _quantityInput = "1";
                _slotInput = "1";

                // Reset quality to default (Heavenly)
                _selectedQualityIndex = 4; // Assuming Heavenly is the 5th option
            }

            if (buttonRect.Contains(Event.current.mousePosition))
            {
                ShowItemTooltip(itemName, buttonRect);
            }
        }

        private void ShowItemTooltip(string itemName, Rect hoverRect)
        {
            string itemId = _itemDictionary[itemName];
            _currentTooltip = $"Item: {itemName}\nID: {itemId}";
            _tooltipPosition = new Vector2(hoverRect.xMax + 10, hoverRect.y);
            _showTooltip = true;
            _tooltipTimer = 0f;
        }

        private void DrawSettingsPanel()
        {
            try
            {
                // Header with title and close button
                GUILayout.BeginHorizontal(_headerStyle);
                GUILayout.Label("Settings", _titleStyle, GUILayout.ExpandWidth(true));

                // Close button
                if (GUILayout.Button("X", _iconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    _showSettings = false;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Create scrollview for settings
                _settingsScrollPosition = GUILayout.BeginScrollView(_settingsScrollPosition);

                // Settings content - now organized in sections
                // Visual Settings Section
                GUILayout.BeginVertical(_panelStyle);
                GUILayout.Label("Visual Settings", _subHeaderStyle ?? _labelStyle);

                // UI Scale slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("UI Scale:", GUILayout.Width(120));
                float newScale = GUILayout.HorizontalSlider(_uiScale, 0.7f, 1.5f, _sliderStyle ?? GUI.skin.horizontalSlider,
                                                       _sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
                if (newScale != _uiScale)
                {
                    _uiScale = newScale;
                }
                GUILayout.Label($"{_uiScale:F2}x", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                // UI Opacity slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("UI Opacity:", GUILayout.Width(120));
                float newOpacity = GUILayout.HorizontalSlider(_uiOpacity, 0.5f, 1.0f, _sliderStyle ?? GUI.skin.horizontalSlider,
                                                         _sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
                if (newOpacity != _uiOpacity)
                {
                    _uiOpacity = newOpacity;
                }
                GUILayout.Label($"{(int)(_uiOpacity * 100)}%", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                GUILayout.Space(15);




                // Toggle settings
                GUILayout.BeginHorizontal();
                bool newAnimations = GUILayout.Toggle(_enableAnimations, "Enable Animations", GUILayout.Width(200));
                if (newAnimations != _enableAnimations)
                {
                    _enableAnimations = newAnimations;
                    if (!_enableAnimations)
                    {
                        // Reset all animations
                        _commandAnimations.Clear();
                        _buttonHoverAnimations.Clear();
                        _itemGridAnimations.Clear();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _enableGlow = GUILayout.Toggle(_enableGlow, "Enable Glow Effects", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _enableBlur = GUILayout.Toggle(_enableBlur, "Enable Background Blur", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _darkTheme = GUILayout.Toggle(_darkTheme, "Dark Theme", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.Space(15);

                // Keybind Settings Section
                GUILayout.BeginVertical(_panelStyle);
                GUILayout.Label("Keyboard Shortcuts", _subHeaderStyle ?? _labelStyle);

                // Menu Toggle Key
                GUILayout.BeginHorizontal();
                GUILayout.Label("Menu Toggle Key:", GUILayout.Width(150));
                string menuKeyText = _isCapturingKey && _currentKeyCaptureEntry == _menuToggleKeyEntry ?
                    "Press any key..." : _menuToggleKeyEntry.Value;
                GUILayout.Label(menuKeyText, GUILayout.Width(100));

                if (GUILayout.Button("Change", _buttonStyle, GUILayout.Width(80)))
                {
                    StartCaptureKeybind(_menuToggleKeyEntry);
                }
                GUILayout.EndHorizontal();

                // Explosion Key
                GUILayout.BeginHorizontal();
                GUILayout.Label("Explosion Key:", GUILayout.Width(150));
                string explosionKeyText = _isCapturingKey && _currentKeyCaptureEntry == _explosionKeyEntry ?
                    "Press any key..." : _explosionKeyEntry.Value;
                GUILayout.Label(explosionKeyText, GUILayout.Width(100));

                if (GUILayout.Button("Change", _buttonStyle, GUILayout.Width(80)))
                {
                    StartCaptureKeybind(_explosionKeyEntry);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.Space(15);

                // About section
                GUILayout.BeginVertical(_panelStyle);
                GUILayout.Label("About", _subHeaderStyle ?? _labelStyle);
                GUILayout.Label($"{ModInfo.Name} - {ModInfo.Version}");
                GUILayout.Label($"by {ModInfo.Author}");

                // HWID Information
                GUILayout.Space(10);
                GUILayout.Label("HWID Spoofer", _subHeaderStyle ?? _labelStyle);
                GUILayout.Label($"Current HWID: {_generatedHwid}");

                if (GUILayout.Button("Generate New HWID", _buttonStyle, GUILayout.Width(150)))
                {
                    GenerateNewHWID(null);
                }
                GUILayout.EndVertical();

                GUILayout.Space(15);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Debug Info", _buttonStyle, GUILayout.Width(150)))
                {
                    GenerateDebugInfoCommand(null);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                
                // Action buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Settings", _buttonStyle, GUILayout.Width(150)))
                {
                    SaveSettings();
                }

                if (GUILayout.Button("Reset Settings", _buttonStyle, GUILayout.Width(150)))
                {
                    // Reset to defaults
                    _uiScale = 1.0f;
                    _uiOpacity = 0.95f;
                    _enableAnimations = true;
                    _enableGlow = true;
                    _enableBlur = true;
                    _darkTheme = true;

                    ShowNotification("Settings Reset", "All settings have been reset to defaults", NotificationType.Info);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing settings panel: {ex.Message}");
            }
        }

        private void ShowDropdownMenu(CommandParameter param)
        {
            // Explicit logging

            if (param == null)
            {
                Debug.LogError("CommandParameter is NULL!");
                return;
            }

            if (!_itemCache.ContainsKey(param.ItemCacheKey))
            {
                Debug.LogError($"NO ITEM CACHE FOR KEY: {param.ItemCacheKey}");
                return;
            }

            var items = _itemCache[param.ItemCacheKey];

            // Default to first item if none selected
            if (string.IsNullOrEmpty(param.Value) && items.Count > 0)
                param.Value = items[0];

            // Get current index
            int currentIndex = items.IndexOf(param.Value);

            // Cycle to the next value, wrapping around
            int nextIndex = (currentIndex + 1) % items.Count;
            param.Value = items[nextIndex];

        }

        #region HWID Spoofer
        private static string _generatedHwid;

        private void ViewCurrentHWID(string[] args)
        {
            try
            {
                ShowNotification("HWID", $"Current HWID: {_generatedHwid}", NotificationType.Info);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error viewing HWID: {ex.Message}");
                ShowNotification("Error", "Failed to view HWID", NotificationType.Error);
            }
        }

        private void GenerateNewHWID(string[] args)
        {
            try
            {
                // Use the existing HWID generation logic from InitializeHwidPatch
                var random = new System.Random(Environment.TickCount);
                var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                random.NextBytes(bytes);
                var newId = string.Join("", bytes.Select(it => it.ToString("x2")));

                // Update the preferences entry
                var hwidEntry = MelonPreferences.CreateEntry("CheatMenu", "HWID", "", is_hidden: true);
                hwidEntry.Value = newId;

                // Update the static _generatedHwid
                _generatedHwid = newId;

                ShowNotification("HWID", $"Generated new HWID", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error generating new HWID: {ex.Message}");
                ShowNotification("Error", "Failed to generate new HWID", NotificationType.Error);
            }
        }

        private void InitializeHwidPatch()
        {
            try
            {
                LoggerInstance.Msg("Initializing HWID Patch...");

                // Always generate a new HWID on each game load
                var random = new System.Random(Environment.TickCount);
                var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                random.NextBytes(bytes);
                var newId = string.Join("", bytes.Select(it => it.ToString("x2")));

                // Save the new HWID to MelonPreferences
                var hwidEntry = MelonPreferences.CreateEntry("CheatMenu", "HWID", newId, is_hidden: true);

                // Store the generated HWID
                _generatedHwid = newId;

                // Create a harmony patch for the deviceUniqueIdentifier property
                var originalMethod = typeof(SystemInfo).GetProperty("deviceUniqueIdentifier").GetMethod;
                var patchMethod = typeof(Core).GetMethod("GetDeviceIdPatch",
                    BindingFlags.Static | BindingFlags.NonPublic);

                _harmony.Patch(originalMethod, new HarmonyLib.HarmonyMethod(patchMethod));

                LoggerInstance.Msg("HWID Patch integrated successfully");
                LoggerInstance.Msg($"New HWID: {newId}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize HWID patch: {ex.Message}");
            }
        }

        // Harmony patch for SystemInfo.deviceUniqueIdentifier
        private static bool GetDeviceIdPatch(ref string __result)
        {
            __result = _generatedHwid;
            return false; // Skip the original method
        }
        #endregion

        #region Command Implementations

        // Method to get a player's health component
        private PlayerHealth GetPlayerHealth(Il2CppScheduleOne.PlayerScripts.Player player)
        {
            try
            {
                if (player != null)
                {
                    // Try to get the health component directly
                    return player.GetComponent<PlayerHealth>();
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error getting player health: {ex.Message}");
            }
            return null;
        }

        private Il2CppScheduleOne.PlayerScripts.Player FindLocalPlayer()
        {
            try 
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList != null)
                {
                    // First check: Try to find player with IsLocalPlayer flag

                    foreach (var player in playerList)
                    {
                        if (player != null && player.IsLocalPlayer)
                        {
                            return player;
                        }
                    }

                    // Second check: Try to find player with name matching device name
                    foreach (var player in playerList)
                    {
                        if (player != null && player.name.Contains(SystemInfo.deviceName))
                        {
                            return player;
                        }
                    }

                    // Third check: Try to find player with IsOwner flag or similar ownership flag
                    foreach (var player in playerList)
                    {
                        if (player != null)
                        {
                            // Check for NetworkBehaviour and IsOwner
                            var netBehavior = player.GetComponent<Il2CppFishNet.Object.NetworkBehaviour>();
                            if (netBehavior != null && netBehavior.IsOwner)
                            {
                                return player;
                            }

                            // Check for NetworkObject and IsOwner
                            var netObject = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                            if (netObject != null && netObject.IsOwner)
                            {
                                return player;
                            }

                            // Use reflection to check for any property that might indicate ownership
                            var properties = player.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            foreach (var prop in properties)
                            {
                                string propName = prop.Name.ToLower();
                                if ((propName.Contains("islocal") || propName.Contains("isowner") || propName.Contains("ismine")) &&
                                    prop.PropertyType == typeof(bool))
                                {
                                    try
                                    {
                                        bool value = (bool)prop.GetValue(player);
                                        if (value)
                                        {
                                            return player;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }

                    // As a last resort, if we only have one player, assume it's the local player
                    if (playerList.Count == 1)
                    {
                        return playerList[0];
                    }

                    LoggerInstance.Error("Could not identify local player!");
                }
                else
                {
                    LoggerInstance.Error("PlayerList is null!");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error finding local player: {ex.Message}");
            }
            return null;
        }

        // Method to determine if a player is the local player
        private bool IsLocalPlayer(Il2CppScheduleOne.PlayerScripts.Player player)
        {
            try
            {
                if (player != null)
                {
                    // Check if this player is the local player
                    return player.IsLocalPlayer ||
                           player.name.Contains(SystemInfo.deviceName);
                }
            }
            catch { }
            return false;
        }

        private IEnumerator SpawnItemViaConsoleCoroutine(string itemId, int quantity, Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            bool success = false;
            Exception caughtException = null;

            // Initial add item command
            var addList = new Il2CppSystem.Collections.Generic.List<string>();
            addList.Add(itemId);
            addList.Add(quantity.ToString());

            try
            {
                new Il2CppScheduleOne.Console.AddItemToInventoryCommand().Execute(addList);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            yield return null; // Wait one frame

            if (caughtException != null)
            {
                LoggerInstance.Error($"Add item failed: {caughtException}");
                yield break;
            }

            // Final cleanup
            try
            {
                CursorManager.Instance.SetCursorAppearance(CursorManager.ECursorType.Default);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Failed to spawn the item: {ex.Message}");
            }
        }

        // Updated spawn caller
        private void SpawnItemViaConsole(string itemId, int quantity, Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            MelonCoroutines.Start(SpawnItemViaConsoleCoroutine(itemId, quantity, quality));
        }

        // For UI buttons (called from buttons in the UI)
        private void PackageProductCommand(string packageType)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedItemId))
                {
                    ShowNotification("Package Product", "No item selected", NotificationType.Warning);
                    return;
                }

                // Validate package type
                if (packageType != "baggie" && packageType != "jar")
                {
                    packageType = "baggie"; // Default to baggie
                }

                // Create a parameter list for the command
                var args = new Il2CppSystem.Collections.Generic.List<string>();
                args.Add(packageType);

                // Execute the PackageProduct command
                var cmd = new Il2CppScheduleOne.Console.PackageProduct();
                cmd.Execute(args);

                ShowNotification("Package Product", $"Item packaged in {packageType}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error packaging product: {ex.Message}");
                ShowNotification("Error", "Failed to package product", NotificationType.Error);
            }
        }

        private void SetItemQuality(Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            try
            {
                // Create parameter list with just the quality value
                var qualityList = new Il2CppSystem.Collections.Generic.List<string>();
                qualityList.Add(((int)quality).ToString());

                // Execute with the parameter list
                var cmd = new Il2CppScheduleOne.Console.SetQuality();
                cmd.Execute(qualityList);

                ShowNotification("Item Quality", $"Set to {quality} quality", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting item quality: {ex.Message}");
                ShowNotification("Error", "Failed to set item quality", NotificationType.Error);
            }
        }

        private void DrawFreecamOverlay()
        {
            try
            {
                // Create a style for the freecam text
                GUIStyle freecamStyle = new GUIStyle();
                freecamStyle.normal.textColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange with some transparency
                freecamStyle.fontSize = 22;
                freecamStyle.fontStyle = FontStyle.Bold;
                freecamStyle.alignment = TextAnchor.UpperCenter;
                freecamStyle.wordWrap = false;

                // Calculate position - centered at top of screen
                Rect textRect = new Rect(
                    Screen.width / 2 - 150,
                    20,
                    300,
                    30
                );

                // Draw text with a shadow effect for better visibility
                // First draw shadow
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.Label(new Rect(textRect.x + 2, textRect.y + 2, textRect.width, textRect.height),
                    "FREECAM MODE (ESC to exit)", freecamStyle);

                // Then draw main text
                GUI.color = new Color(1f, 0.5f, 0f, 0.8f);
                GUI.Label(textRect, "FREECAM MODE (ESC to exit)", freecamStyle);

                // Reset color
                GUI.color = Color.white;

                // Controls help text with the same shadow effect
                GUIStyle helpStyle = new GUIStyle();
                helpStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f); // White with some transparency
                helpStyle.fontSize = 18;
                helpStyle.alignment = TextAnchor.UpperCenter;

                // Positioned further down
                Rect helpRect = new Rect(
                    Screen.width / 2 - 200,
                    66,
                    400,
                    60
                );

                // Draw shadow for control text
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.Label(new Rect(helpRect.x + 2, helpRect.y + 2, helpRect.width, helpRect.height),
                    "WASD to move · Space/Ctrl to move up/down · Shift to move faster",
                    helpStyle);

                // Draw main control text
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.Label(helpRect,
                    "WASD to move · Space/Ctrl to move up/down · Shift to move faster",
                    helpStyle);

                // Reset color
                GUI.color = Color.white;
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error drawing freecam overlay: {ex.Message}");
            }
        }

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

        private void EndTutorial(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.EndTutorial();
                command.Execute(null);
                ShowNotification("Tutorial", "Tutorial ended", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error ending tutorial: {ex.Message}");
                ShowNotification("Error", "Failed to end tutorial", NotificationType.Error);
            }
        }


        private void ForceGameSave(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.Save();
                command.Execute(null);
                ShowNotification("Game", "Save completed", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error forcing game save: {ex.Message}");
                ShowNotification("Error", "Failed to save game", NotificationType.Error);
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

        #endregion

        #region UI Functions
        private void DrawPlayerExploitsUI(CommandCategory category)
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // Player list panel at the top
                float playerPanelHeight = 120f; // Fixed height for player panel

                // Draw player list at the top
                Rect playerListRect = new Rect(20, 100, windowWidth, playerPanelHeight);
                GUI.Box(playerListRect, "", _panelStyle);

                // Player list header
                GUI.Label(
                    new Rect(playerListRect.x + 10, playerListRect.y + 10, 150, 20),
                    "Players Online",
                    _commandLabelStyle ?? _labelStyle
                );

                // Draw player entries in a horizontal layout
                DrawPlayerListHorizontal(playerListRect);

                // Commands section below player list
                _scrollPosition = GUI.BeginScrollView(
                    new Rect(20, 100 + playerPanelHeight + 10, windowWidth, windowHeight - playerPanelHeight - 10),
                    _scrollPosition,
                    new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f)
                );

                float yOffset = 0f;

                // Draw the standard commands
                foreach (var command in category.Commands)
                {
                    // Command container rectangle
                    Rect commandRect = new Rect(0, yOffset, windowWidth - 40f, 90f);
                    GUI.Box(commandRect, "", _panelStyle);

                    // Command Name
                    GUI.Label(
                        new Rect(commandRect.x + 10f, commandRect.y + 5f, 200f, 25f),
                        command.Name,
                        _commandLabelStyle ?? _labelStyle
                    );

                    // Parameters handling
                    float paramX = commandRect.x + 220f;
                    if (command.Parameters.Count > 0)
                    {
                        foreach (var param in command.Parameters)
                        {
                            Rect paramRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                            if (param.Type == ParameterType.Input)
                            {
                                // Unique key for each parameter
                                string paramKey = $"param_{command.Name}_{param.Name}";

                                // Custom text field
                                if (!_textFields.TryGetValue(paramKey, out var textField))
                                {
                                    textField = new CustomTextField(param.Value ?? "", _inputFieldStyle ?? GUI.skin.textField);
                                    _textFields[paramKey] = textField;
                                }

                                param.Value = textField.Draw(paramRect);
                            }
                            else if (param.Type == ParameterType.Dropdown)
                            {
                                // Dropdown-like button
                                if (GUI.Button(paramRect, param.Value ?? "Select", _buttonStyle))
                                {
                                    ShowDropdownMenu(param);
                                }
                            }

                            paramX += 130f;
                        }
                    }

                    // Execute Button
                    Rect executeRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);
                    if (GUI.Button(executeRect, "Execute", _buttonStyle))
                    {
                        ExecuteCommand(command);
                    }

                    // Optional description
                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        GUI.Label(
                            new Rect(commandRect.x + 10f, commandRect.y + 35f, windowWidth - 60f, 50f),
                            command.Description,
                            _tooltipStyle ?? GUI.skin.label
                        );
                    }

                    // Increment Y offset for next command
                    yOffset += 100f;
                }

                GUI.EndScrollView();
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawPlayerExploitsUI: {ex}");
            }
        }

        private void DrawPlayerListHorizontal(Rect containerRect)
        {
            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList == null || playerList.Count == 0)
                {
                    GUI.Label(
                        new Rect(containerRect.x + 10, containerRect.y + 30, 200, 25),
                        "No players found",
                        _labelStyle
                    );
                    return;
                }

                float contentY = containerRect.y + 40;

                foreach (var player in playerList)
                {
                    if (player == null) continue;

                    bool isLocal = IsLocalPlayer(player);
                    var playerHealth = GetPlayerHealth(player);

                    // Basic player card
                    Rect playerRect = new Rect(
                        containerRect.x + 10,
                        contentY,
                        containerRect.width - 20,
                        85
                    );

                    // Player card background - solid black for better contrast
                    GUI.color = new Color(0.1f, 0.1f, 0.15f, 1.0f);
                    GUI.DrawTexture(playerRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    // Player name
                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.fontSize = 18;
                    nameStyle.fontStyle = FontStyle.Bold;
                    nameStyle.normal.textColor = isLocal ? Color.cyan : Color.white;

                    GUI.Label(
                        new Rect(playerRect.x + 10, playerRect.y + 5, playerRect.width - 20, 25),
                        $"{player.name}{(isLocal ? " (YOU)" : "")}",
                        nameStyle
                    );

                    // ALIVE status on right side of name
                    if (playerHealth != null)
                    {
                        bool isAlive = playerHealth.IsAlive;

                        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                        statusStyle.fontSize = 16;
                        statusStyle.fontStyle = FontStyle.Bold;
                        statusStyle.normal.textColor = isAlive ? Color.green : Color.red;
                        statusStyle.alignment = TextAnchor.MiddleRight;

                        GUI.Label(
                            new Rect(playerRect.x + playerRect.width - 90, playerRect.y + 5, 80, 25),
                            isAlive ? "ALIVE" : "DEAD",
                            statusStyle
                        );
                    }

                    // Health display
                    if (playerHealth != null)
                    {
                        GUIStyle healthStyle = new GUIStyle(_labelStyle);
                        healthStyle.fontSize = 16;

                        // Only show the "Health:" label
                        GUI.Label(
                            new Rect(playerRect.x + 10, playerRect.y + 35, 50, 20),
                            "Health:",
                            healthStyle
                        );

                        // Make the health bar bigger and more prominent
                        Rect healthBarRect = new Rect(playerRect.x + 70, playerRect.y + 35, playerRect.width - 90, 20);
                        GUI.Box(healthBarRect, "", GUI.skin.box);

                        // Calculate percentage based on integer values to avoid any decimal issues
                        float healthPercent = (int)playerHealth.CurrentHealth / (float)(int)PlayerHealth.MAX_HEALTH;
                        Rect fillRect = new Rect(
                            healthBarRect.x + 2,
                            healthBarRect.y + 2,
                            (healthBarRect.width - 4) * healthPercent,
                            healthBarRect.height - 4
                        );

                        // Better color gradient from red to green
                        Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
                        GUI.color = healthColor;
                        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }

                    // SteamID at bottom
                    var playerInfo = _onlinePlayers.FirstOrDefault(p => p.Player == player);
                    if (playerInfo != null)
                    {
                        GUIStyle idStyle = new GUIStyle(GUI.skin.label);
                        idStyle.fontSize = 16;
                        idStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

                        GUI.Label(
                            new Rect(playerRect.x + 10, playerRect.y + 60, 300, 20),
                            playerInfo.SteamID,
                            idStyle
                        );

                        // Explode Loop Toggle
                        if (!isLocal)
                        {
                            Rect toggleRect = new Rect(playerRect.x + playerRect.width - 110, playerRect.y + 60, 100, 20);

                            // Custom toggle style with white text for better visibility
                            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
                            toggleStyle.normal.textColor = Color.white;
                            toggleStyle.onNormal.textColor = Color.white;

                            bool newState = GUI.Toggle(
                                toggleRect,
                                playerInfo.ExplodeLoop,
                                "Explode Loop",
                                toggleStyle
                            );

                            if (newState != playerInfo.ExplodeLoop)
                            {
                                playerInfo.ExplodeLoop = newState;
                                if (newState)
                                    StartExplodeLoop(playerInfo);
                                else
                                    StopExplodeLoop(playerInfo);
                            }
                        }
                    }

                    contentY += 95; // Space between cards
                }

                // Global Explode Loop All button
                Rect explodeLoopAllRect = new Rect(
                    containerRect.x + 10,
                    contentY + 10,
                    containerRect.width - 20,
                    30
                );

                if (GUI.Button(explodeLoopAllRect, "Explode Loop All", _buttonStyle))
                {
                    foreach (var playerInfo in _onlinePlayers)
                    {
                        if (!playerInfo.IsLocal)
                        {
                            playerInfo.ExplodeLoop = true;
                            StartExplodeLoop(playerInfo);
                        }
                    }
                    ShowNotification("Players", "Started explosion loop on all players", NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing horizontal player list: {ex.Message}");
            }
        }
        #endregion


        #region Teleporter
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


        private EMapRegion GetRegionAtPosition(Vector2 normalizedPos)
        {
            foreach (var regionPair in _regionRects)
            {
                if (regionPair.Value.Contains(normalizedPos))
                {
                    return regionPair.Key;
                }
            }

            // No matching region found
            return (EMapRegion)(-1);
        }


        private void DrawTeleportManager()
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // ADDED: Current player coordinates - placed below the tabs, centered
                var localPlayer = FindLocalPlayer();
                Vector3 currentPos = Vector3.zero;

                if (localPlayer != null)
                {
                    currentPos = localPlayer.transform.position;

                    // Create a style for coordinates
                    GUIStyle coordStyle = new GUIStyle(_labelStyle);
                    coordStyle.alignment = TextAnchor.MiddleCenter;
                    coordStyle.fontSize = 14;
                    coordStyle.fontStyle = FontStyle.Bold;

                    // Draw position text directly below tabs
                    Rect coordRect = new Rect(0, 90, _windowRect.width, 25);

                    // Format the coordinate text
                    string coordText = $"Current Position: X: {currentPos.x:F1}, Y: {currentPos.y:F1}, Z: {currentPos.z:F1}";

                    // Draw the text centered
                    GUI.Label(coordRect, coordText, coordStyle);
                }

                // Make the map smaller to accommodate coordinate controls in middle
                float mapHeight = windowHeight * 0.45f; // Reduced from 0.6f
                _mapRect = new Rect(20, 115, windowWidth, mapHeight);
                GUI.Box(_mapRect, "", _panelStyle);

                // Draw the map
                DrawInteractiveMap(_mapRect);

                // Check if we need to initialize the map - ADD AUTO LOADING
                if (!_mapInitialized && !_isCapturingMap)
                {
                    // Start map capture automatically
                    MelonCoroutines.Start(CaptureMapCoroutine());
                    _isCapturingMap = true;
                    LoggerInstance.Msg("Auto-starting map capture");
                }

                // MIDDLE SECTION - XYZ Controls
                float middleSectionHeight = 80;
                Rect middleSectionRect = new Rect(20, 115 + mapHeight + 10, windowWidth, middleSectionHeight);
                GUI.Box(middleSectionRect, "", _panelStyle);

                // X, Y, Z input fields - now moved to the middle and made more compact
                float inputWidth = (windowWidth - 100) / 3;
                float inputY = middleSectionRect.y + 10;

                // X field
                GUI.Label(
                    new Rect(middleSectionRect.x + 10, inputY + 5, 20, 25),
                    "X:",
                    _labelStyle
                );

                if (!_textFields.TryGetValue("teleport_x", out var xField))
                {
                    xField = new CustomTextField(localPlayer != null ? currentPos.x.ToString("F1") : "0",
                                                 _inputFieldStyle ?? GUI.skin.textField);
                    _textFields["teleport_x"] = xField;
                }
                xField.Draw(new Rect(middleSectionRect.x + 30, inputY, inputWidth, 25));

                // Y field
                GUI.Label(
                    new Rect(middleSectionRect.x + inputWidth + 40, inputY + 5, 20, 25),
                    "Y:",
                    _labelStyle
                );

                if (!_textFields.TryGetValue("teleport_y", out var yField))
                {
                    yField = new CustomTextField(localPlayer != null ? currentPos.y.ToString("F1") : "0",
                                                 _inputFieldStyle ?? GUI.skin.textField);
                    _textFields["teleport_y"] = yField;
                }
                yField.Draw(new Rect(middleSectionRect.x + inputWidth + 60, inputY, inputWidth, 25));

                // Z field
                GUI.Label(
                    new Rect(middleSectionRect.x + inputWidth * 2 + 70, inputY + 5, 20, 25),
                    "Z:",
                    _labelStyle
                );

                if (!_textFields.TryGetValue("teleport_z", out var zField))
                {
                    zField = new CustomTextField(localPlayer != null ? currentPos.z.ToString("F1") : "0",
                                                 _inputFieldStyle ?? GUI.skin.textField);
                    _textFields["teleport_z"] = zField;
                }
                zField.Draw(new Rect(middleSectionRect.x + inputWidth * 2 + 90, inputY, inputWidth, 25));

                // Button row
                float buttonY = inputY + 35;
                float buttonWidth = 150;
                float buttonSpacing = 30;
                float startX = middleSectionRect.x + (windowWidth - (buttonWidth * 2 + buttonSpacing)) / 2;

                // Teleport button
                if (GUI.Button(
                    new Rect(startX, buttonY, buttonWidth, 25),
                    "Teleport to Coordinates",
                    _buttonStyle))
                {
                    try
                    {
                        float x = float.Parse(xField.Value);
                        float y = float.Parse(yField.Value);
                        float z = float.Parse(zField.Value);

                        TeleportPlayer(new Vector3(x, y, z));
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error parsing coordinates: {ex.Message}");
                        ShowNotification("Error", "Invalid coordinates", NotificationType.Error);
                    }
                }

                // Update position button - simplified to use current coordinates directly
                if (GUI.Button(
                    new Rect(startX + buttonWidth + buttonSpacing, buttonY, buttonWidth, 25),
                    "Use Current Position",
                    _buttonStyle) && localPlayer != null)
                {
                    // Copy to input fields
                    if (_textFields.TryGetValue("teleport_x", out var xF))
                        xF.Value = currentPos.x.ToString("F1");

                    if (_textFields.TryGetValue("teleport_y", out var yF))
                        yF.Value = currentPos.y.ToString("F1");

                    if (_textFields.TryGetValue("teleport_z", out var zF))
                        zF.Value = currentPos.z.ToString("F1");

                    ShowNotification("Position", "Current position updated in input fields", NotificationType.Info);
                }

                // Predefined teleports - takes bottom portion, now smaller with more space for the middle section
                float teleportListHeight = windowHeight - mapHeight - middleSectionHeight - 40;
                Rect teleportListRect = new Rect(20, middleSectionRect.y + middleSectionHeight + 10, windowWidth, teleportListHeight);
                GUI.Box(teleportListRect, "", _panelStyle);

                // Title and refresh button in single row
                GUI.Label(
                    new Rect(teleportListRect.x + 10, teleportListRect.y + 5, 200, 25),
                    "Predefined Teleports",
                    _commandLabelStyle ?? _labelStyle
                );

                // Refresh button
                if (GUI.Button(
                    new Rect(teleportListRect.x + teleportListRect.width - 100, teleportListRect.y + 5, 80, 25),
                    "Refresh",
                    _buttonStyle))
                {
                    InitializePredefinedTeleports();
                    ShowNotification("Teleport", "Teleport locations refreshed", NotificationType.Info);
                }

                // Draw the predefined teleport buttons in a scrollable area - now smaller
                float teleportButtonsY = teleportListRect.y + 35;
                float teleportButtonsHeight = teleportListHeight - 45; // Leave space for header
                Rect teleportButtonsRect = new Rect(
                    teleportListRect.x + 10,
                    teleportButtonsY,
                    teleportListRect.width - 20,
                    teleportButtonsHeight
                );

                // Add a scroll view for the teleport locations
                _scrollPosition = GUI.BeginScrollView(
                    teleportButtonsRect,
                    _scrollPosition,
                    new Rect(0, 0, teleportButtonsRect.width - 20, _predefinedTeleports.Count * 30)
                );

                int i = 0;
                foreach (var teleport in _predefinedTeleports)
                {
                    Rect buttonRect = new Rect(5, i * 30, teleportButtonsRect.width - 30, 25);
                    if (GUI.Button(buttonRect, teleport.Key, _buttonStyle))
                    {
                        TeleportPlayer(teleport.Value);
                        ShowNotification("Teleport", $"Teleported to {teleport.Key}", NotificationType.Success);
                    }
                    i++;
                }

                GUI.EndScrollView();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawTeleportManager: {ex.Message}");
            }
        }

        private void TeleportTargetPlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer, Vector3 position)
        {
            try
            {
                if (targetPlayer == null)
                {
                    LoggerInstance.Error("Target player is null!");
                    ShowNotification("Error", "Target player not found", NotificationType.Error);
                    return;
                }

                // Ensure position is valid
                if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
                    float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
                {
                    LoggerInstance.Error("Invalid teleport position!");
                    ShowNotification("Error", "Invalid teleport position", NotificationType.Error);
                    return;
                }

                // Method 1: Try to get PlayerMovement from target player
                var playerMovement = targetPlayer.GetComponent<Il2CppScheduleOne.PlayerScripts.PlayerMovement>();
                if (playerMovement != null)
                {
                    // Direct call to the player's Teleport method
                    LoggerInstance.Msg($"Teleporting {targetPlayer.name} using PlayerMovement.Teleport");
                    playerMovement.Teleport(position);
                    ShowNotification("Teleport", $"Teleported {targetPlayer.name} using movement teleport", NotificationType.Success);
                    return;
                }

                // Method 2: Try to use teleport fields if available
                try
                {
                    // Set player's teleport flag and position
                    var teleportField = targetPlayer.GetType().GetField("teleport", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var teleportPosField = targetPlayer.GetType().GetField("teleportPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (teleportField != null && teleportPosField != null)
                    {
                        // Set teleport flag to true
                        teleportField.SetValue(targetPlayer, true);
                        // Set teleport position
                        teleportPosField.SetValue(targetPlayer, position);

                        LoggerInstance.Msg($"Teleporting {targetPlayer.name} using teleport fields");
                        ShowNotification("Teleport", $"Set teleport flag and position for {targetPlayer.name}", NotificationType.Success);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Error accessing teleport fields: {ex.Message}");
                }

                // Method 3: As a last resort, try to directly set transform position
                LoggerInstance.Msg($"Fallback: Directly setting {targetPlayer.name}'s position");
                targetPlayer.transform.position = position;

                // Try to force position sync if possible
                var netObj = targetPlayer.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                if (netObj != null)
                {
                    // Try to call any transform dirty method
                    var dirtyTransformMethod = netObj.GetType().GetMethods()
                        .FirstOrDefault(m => m.Name.Contains("Transform") && m.Name.Contains("Dirty"));

                    if (dirtyTransformMethod != null)
                    {
                        dirtyTransformMethod.Invoke(netObj, null);
                        LoggerInstance.Msg("Called transform dirty method");
                    }
                }

                ShowNotification("Teleport", $"Direct position set for {targetPlayer.name}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error teleporting target player: {ex.Message}");
                ShowNotification("Error", "Teleport failed: " + ex.Message, NotificationType.Error);
            }
        }

        private void GenerateDebugInfoCommand(string[] args)
        {
            try
            {
                StringBuilder debugInfo = new StringBuilder();
                debugInfo.AppendLine("=== MODERN CHEAT MENU DEBUG INFORMATION ===");
                debugInfo.AppendLine($"Generated: {DateTime.Now}");
                debugInfo.AppendLine();

                // System information
                debugInfo.AppendLine("=== SYSTEM INFORMATION ===");
                debugInfo.AppendLine($"OS: {SystemInfo.operatingSystem}");
                debugInfo.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
                debugInfo.AppendLine($"RAM: {SystemInfo.systemMemorySize} MB");
                debugInfo.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
                debugInfo.AppendLine($"GPU API: {SystemInfo.graphicsDeviceType}");
                debugInfo.AppendLine($"GPU Memory: {SystemInfo.graphicsMemorySize} MB");
                debugInfo.AppendLine($"GPU Driver: {SystemInfo.graphicsDeviceVersion}");
                debugInfo.AppendLine($"Screen Resolution: {Screen.currentResolution.width}x{Screen.currentResolution.height} @{Screen.currentResolution.refreshRate}Hz");
                debugInfo.AppendLine($"Current DPI: {Screen.dpi}");
                debugInfo.AppendLine($"Device Unique ID: {SystemInfo.deviceUniqueIdentifier}");
                debugInfo.AppendLine($"Device Model: {SystemInfo.deviceModel}");
                debugInfo.AppendLine($"HWID (Spoofed): {_generatedHwid}");
                debugInfo.AppendLine();

                // MelonLoader information
                debugInfo.AppendLine("=== MELONLOADER INFORMATION ===");
                try
                {
                    //debugInfo.AppendLine($"MelonLoader Version: {MelonLoader.BuildInfo.Version}");
                    //debugInfo.AppendLine($"MelonLoader Hash: {MelonLoader.BuildInfo.Hash}");
                    //debugInfo.AppendLine($"Game Assembly: {MelonLoader.BuildInfo.GameAssembly}");
                    //debugInfo.AppendLine($"Is Game IL2CPP: {MelonLoader.InternalUtils.UnhollowerSupport.IsGameIl2Cpp()}");

                    var melonAssembly = typeof(MelonLoader.MelonMod).Assembly;
                    debugInfo.AppendLine($"MelonLoader Assembly: {melonAssembly.GetName().Name} v{melonAssembly.GetName().Version}");

                    // List loaded mods
                    var loadedMods = MelonLoader.MelonBase.RegisteredMelons;
                    if (loadedMods != null && loadedMods.Count > 0)
                    {
                        debugInfo.AppendLine($"Loaded Mods ({loadedMods.Count}):");
                        foreach (var mod in loadedMods)
                        {
                            debugInfo.AppendLine($"  - {mod.Info.Name} v{mod.Info.Version} by {mod.Info.Author}");
                        }
                    }
                    else
                    {
                        debugInfo.AppendLine("No other mods loaded");
                    }
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Error retrieving MelonLoader info: {ex.Message}");
                }
                debugInfo.AppendLine();

                // Game information
                debugInfo.AppendLine("=== GAME INFORMATION ===");
                debugInfo.AppendLine($"Game Name: {Modern_Cheat_Menu.ModInfo.NameOfGame}");
                debugInfo.AppendLine($"Game Developers: {Modern_Cheat_Menu.ModInfo.GameDevelopers}");
                debugInfo.AppendLine($"Unity Version: {Application.unityVersion}");
                debugInfo.AppendLine($"Game Version: {Application.version}");
                debugInfo.AppendLine($"Game Data Path: {Application.dataPath}");
                debugInfo.AppendLine($"Product Name: {Application.productName}");
                debugInfo.AppendLine($"Company Name: {Application.companyName}");
                debugInfo.AppendLine($"Target Frame Rate: {Application.targetFrameRate}");
                debugInfo.AppendLine($"Is Focused: {Application.isFocused}");
                debugInfo.AppendLine($"Is Playing: {Application.isPlaying}");
                debugInfo.AppendLine($"Is Background: {Application.runInBackground}");
                debugInfo.AppendLine($"Quality Level: {QualitySettings.GetQualityLevel()}");
                debugInfo.AppendLine();

                // Mod information
                debugInfo.AppendLine("=== MOD INFORMATION ===");
                debugInfo.AppendLine($"Menu Name: {Modern_Cheat_Menu.ModInfo.Name}");
                debugInfo.AppendLine($"Menu Version: {Modern_Cheat_Menu.ModInfo.Version}");
                debugInfo.AppendLine($"Author: {Modern_Cheat_Menu.ModInfo.Author}");
                debugInfo.AppendLine($"Repository: {Modern_Cheat_Menu.ModInfo.RepositoryUrl}");
                debugInfo.AppendLine($"UI Initialized: {_isInitialized}");
                debugInfo.AppendLine($"UI Scale: {_uiScale}");
                debugInfo.AppendLine($"UI Opacity: {_uiOpacity}");
                debugInfo.AppendLine($"Animations Enabled: {_enableAnimations}");
                debugInfo.AppendLine($"Window Position: X={_windowRect.x}, Y={_windowRect.y}, W={_windowRect.width}, H={_windowRect.height}");
                debugInfo.AppendLine();

                // Active features
                debugInfo.AppendLine("=== FEATURE STATUS ===");
                debugInfo.AppendLine($"Godmode: {_playerGodmodeEnabled}");
                debugInfo.AppendLine($"Never Wanted: {_playerNeverWantedEnabled}");
                debugInfo.AppendLine($"Free Camera: {_freeCamEnabled}");
                debugInfo.AppendLine($"Unlimited Ammo: {_unlimitedAmmoEnabled}");
                debugInfo.AppendLine($"Aimbot: {_aimbotEnabled}");
                debugInfo.AppendLine($"Perfect Accuracy: {_perfectAccuracyEnabled}");
                debugInfo.AppendLine($"No Recoil: {_noRecoilEnabled}");
                debugInfo.AppendLine($"One Hit Kill: {_oneHitKillEnabled}");
                debugInfo.AppendLine($"NPCs Pacified: {_npcsPacifiedEnabled}");
                debugInfo.AppendLine($"Crosshair Always Visible: {_forceCrosshairAlwaysVisible}");
                debugInfo.AppendLine();

                // Current player information
                var localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    debugInfo.AppendLine("=== PLAYER INFORMATION ===");
                    debugInfo.AppendLine($"Player Name: {localPlayer.name}");
                    debugInfo.AppendLine($"Position: X={localPlayer.transform.position.x:F2}, Y={localPlayer.transform.position.y:F2}, Z={localPlayer.transform.position.z:F2}");
                    debugInfo.AppendLine($"Rotation: X={localPlayer.transform.rotation.eulerAngles.x:F2}, Y={localPlayer.transform.rotation.eulerAngles.y:F2}, Z={localPlayer.transform.rotation.eulerAngles.z:F2}");

                    var playerHealth = GetPlayerHealth(localPlayer);
                    if (playerHealth != null)
                    {
                        debugInfo.AppendLine($"Health: {playerHealth.CurrentHealth}/{PlayerHealth.MAX_HEALTH}");
                        debugInfo.AppendLine($"Is Alive: {playerHealth.IsAlive}");
                    }

                    var playerMovement = localPlayer.GetComponent<Il2CppScheduleOne.PlayerScripts.PlayerMovement>();
                    if (playerMovement != null)
                    {
                        debugInfo.AppendLine($"Movement Speed: {Il2CppScheduleOne.PlayerScripts.PlayerMovement.WalkSpeed}");
                        debugInfo.AppendLine($"Sprint Multiplier: {Il2CppScheduleOne.PlayerScripts.PlayerMovement.SprintMultiplier}");
                        debugInfo.AppendLine($"Gravity Multiplier: {Il2CppScheduleOne.PlayerScripts.PlayerMovement.GravityMultiplier}");
                        debugInfo.AppendLine($"Jump Force: {Il2CppScheduleOne.PlayerScripts.PlayerMovement.JumpForce}");
                        debugInfo.AppendLine($"Is Grounded: {playerMovement.IsGrounded}");
                        debugInfo.AppendLine($"Is Crouched: {playerMovement.IsCrouched}");
                        debugInfo.AppendLine($"Is Sprinting: {playerMovement.IsSprinting}");
                        debugInfo.AppendLine($"Current Stamina: {playerMovement.CurrentStaminaReserve}");
                    }

                    var netObj = localPlayer.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                    if (netObj != null)
                    {
                        debugInfo.AppendLine($"Network ID: {netObj.ObjectId}");
                        debugInfo.AppendLine($"Owner ID: {netObj.OwnerId}");
                        debugInfo.AppendLine($"Is Owner: {netObj.IsOwner}");
                        debugInfo.AppendLine($"Is Server: {netObj.IsServer}");
                        debugInfo.AppendLine($"Is Spawned: {netObj.IsSpawned}");
                    }

                    debugInfo.AppendLine();
                }

                // Online players
                debugInfo.AppendLine("=== ONLINE INFORMATION ===");
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                debugInfo.AppendLine($"Total Players: {(playerList != null ? playerList.Count : 0)}");
                debugInfo.AppendLine($"Server Socket Found: {(_discoveredServerSocket != null)}");

                // List all players
                if (playerList != null && playerList.Count > 0)
                {
                    debugInfo.AppendLine("\nPlayer List:");
                    foreach (var player in playerList)
                    {
                        if (player == null) continue;
                        debugInfo.AppendLine($"- {player.name} (Local: {IsLocalPlayer(player)})");

                        var netObj = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                        if (netObj != null)
                        {
                            debugInfo.AppendLine($"  Network ID: {netObj.ObjectId}, Owner ID: {netObj.OwnerId}");
                        }
                    }
                }

                // Server transport information
                var transports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();
                if (transports != null && transports.Length > 0)
                {
                    var transport = transports[0];
                    debugInfo.AppendLine("\nTransport Information:");
                    debugInfo.AppendLine($"Transport Type: {transport.GetType().Name}");
                    debugInfo.AppendLine($"Max Clients: {transport._maximumClients}");

                    if (transport._server != null)
                    {
                        debugInfo.AppendLine($"Server Socket: {transport._server.GetType().Name}");
                        debugInfo.AppendLine($"Server Max Clients: {transport._server._maximumClients}");

                        try
                        {
                            if (transport._server._steamIds != null)
                            {
                                debugInfo.AppendLine($"Connected Steam IDs: {transport._server._steamIds.Count}");
                            }
                        }
                        catch (Exception ex)
                        {
                            debugInfo.AppendLine($"Error accessing steam IDs: {ex.Message}");
                        }
                    }
                }

                // Cached items information
                debugInfo.AppendLine("\n=== CACHE INFORMATION ===");
                debugInfo.AppendLine($"Total Items: {_itemDictionary.Count}");
                debugInfo.AppendLine($"Quality Items: {_qualitySupportCache.Count(kv => kv.Value)}");
                debugInfo.AppendLine($"Vehicle Types: {_vehicleCache.Count}");

                // Exception handling info
                debugInfo.AppendLine("\n=== RUNTIME INFO ===");
                debugInfo.AppendLine($"Current Culture: {System.Globalization.CultureInfo.CurrentCulture.Name}");
                debugInfo.AppendLine($"Current UI Culture: {System.Globalization.CultureInfo.CurrentUICulture.Name}");
                //debugInfo.AppendLine($"Thread Count: {System.Threading.Process.GetCurrentProcess().Threads.Count}");
                debugInfo.AppendLine($"CLR Version: {System.Environment.Version}");
                debugInfo.AppendLine($"Process Start Time: {System.Diagnostics.Process.GetCurrentProcess().StartTime}");
                debugInfo.AppendLine($"Process Working Set: {System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB");
                debugInfo.AppendLine($"FPS: {1.0f / Time.deltaTime:F1}");
                debugInfo.AppendLine($"Time.time: {Time.time:F1}");
                debugInfo.AppendLine($"Time.unscaledTime: {Time.unscaledTime:F1}");
                debugInfo.AppendLine($"Time.timeScale: {Time.timeScale:F2}");

                // Save the debug information to a file in the game directory
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(Application.dataPath, $"MCM_Debug_{timestamp}.txt");
                File.WriteAllText(filePath, debugInfo.ToString());

                // Also copy to clipboard for easy sharing
                GUIUtility.systemCopyBuffer = debugInfo.ToString();

                ShowNotification("Debug Info", $"Debug information generated and saved to:\n{filePath}\nAlso copied to clipboard!", NotificationType.Success);
                LoggerInstance.Msg($"Debug information saved to: {filePath}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error generating debug information: {ex.Message}");
                ShowNotification("Error", "Failed to generate debug information", NotificationType.Error);
            }
        }

        private Texture2D CreateReadableTexture(Texture sourceTexture)
        {
            if (sourceTexture == null) return null;

            try
            {
                // Get dimensions
                int width = sourceTexture.width;
                int height = sourceTexture.height;

                // Create render texture
                RenderTexture renderTex = RenderTexture.GetTemporary(
                    width, height, 0, RenderTextureFormat.ARGB32);

                // Copy source texture to render texture
                Graphics.Blit(sourceTexture, renderTex);

                // Create result texture
                Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

                // Save active render texture
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTex;

                // Read pixels
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply();

                // Restore active render texture
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTex);

                return result;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error creating readable texture: {ex.Message}");
                return null;
            }
        }
        #endregion

    }
    #endregion
}
