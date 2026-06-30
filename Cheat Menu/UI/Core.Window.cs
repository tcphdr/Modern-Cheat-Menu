/*
 * Modern Cheat Menu
 * Core.Window.cs
 */

/*
 * Modern Cheat Menu
 * Core.Window.cs
 */

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

    public partial class Core
    {
        private MelonPreferences_Category _settingsCategory;
        private MelonPreferences_Entry<float> _uiScaleEntry;
        private MelonPreferences_Entry<float> _uiOpacityEntry;
        private MelonPreferences_Entry<bool> _enableAnimationsEntry;
        private MelonPreferences_Entry<bool> _enableGlowEntry;
        private MelonPreferences_Entry<bool> _enableBlurEntry;
        private MelonPreferences_Entry<bool> _darkThemeEntry;
        private Texture2D _settingsButtonTexture;
        private Texture2D _closeButtonTexture;
        private bool _showPlayerMap = false;
        private Texture2D _playerMapTexture;
        private bool _playerMapInitialized = false;
        private float _playerMapZoom = 1.0f;
        private Vector2 _playerMapPanOffset = Vector2.zero;
        private bool _isDraggingPlayerMap = false;
        private Vector2 _playerMapDragStart;
        private Vector2 _playerMapDragStartOffset;
        private Dictionary<string, Color> _playerColors = new Dictionary<string, Color>();


        private void ToggleUI(bool visible)
        {
            LoggerInstance.Msg($"Toggling UI visibility: {visible}");
            _uiVisible = visible;

            if (visible)
            {
                _fadeInProgress = 0f;
                _menuAnimationTime = 0f;

                if (_windowRect.x <= -_windowRect.width)
                {
                    _windowRect.x = -_windowRect.width;
                }
                togglePlayerControllable(false);
            }

            else
            {
                togglePlayerControllable(true);
            }
        }

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

        private Texture2D CreateReadableTexture(Texture sourceTexture)
        {
            if (sourceTexture == null) return null;

            try
            {
                // Get dimensions
                int width = sourceTexture.width;
                int height = sourceTexture.height;

                // Create render texture
                RenderTexture renderTex = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

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

        private void DrawWindow(int windowId)
        {
            try
            {
                DrawHeaderWithTexturedButtons();

                GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.Space(40);
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

                GUILayout.BeginVertical(_panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                try
                {
                    if (_showSettings)
                    {
                        DrawSettingsPanel();
                    }

                    else
                    {
                        DrawSelectedCategory();
                    }
                }

                finally
                {
                    GUILayout.EndVertical();
                }

                HandleWindowDragging();

                GUILayout.EndVertical();
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

        private void DrawCommandCategory(CommandCategory category)
        {
            try
            {
                if (category.Name == "Player Exploits")
                {
                    DrawPlayerExploitsUI(category);
                    return;
                }

                float windowWidth = _windowRect.width - 40f;

                _scrollPosition = GUI.BeginScrollView(new Rect(20, 100, windowWidth, _windowRect.height - 150), _scrollPosition, new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f));

                float yOffset = 0f;

                foreach (var command in category.Commands)
                {
                    Rect commandRect = new Rect(0, yOffset, windowWidth - 40f, 90f);
                    GUI.Box(commandRect, "", _panelStyle);

                    GUI.Label(
                        new Rect(commandRect.x + 10f, commandRect.y + 5f, 200f, 25f),
                            command.Name,
                            _commandLabelStyle ?? _labelStyle
                    );

                    float paramX = commandRect.x + 220f;

                    if (command.Parameters.Count > 0)
                    {
                        foreach (var param in command.Parameters)
                        {
                            Rect paramRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                            if (param.Type == ParameterType.Input)
                            {
                                string paramKey = $"param_{command.Name}_{param.Name}";

                                if (!_textFields.TryGetValue(paramKey, out var textField))
                                {
                                    textField = new CustomTextField(param.Value ?? "", _inputFieldStyle ?? GUI.skin.textField);
                                    _textFields[paramKey] = textField;
                                }
                                param.Value = textField.Draw(paramRect);
                            }

                            else if (param.Type == ParameterType.Dropdown)
                            {
                                if (GUI.Button(paramRect, param.Value ?? "Select", _buttonStyle))
                                {
                                    ShowDropdownMenu(param);
                                }
                            }
                            paramX += 130f;
                        }
                    }

                    Rect executeRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                    if (GUI.Button(executeRect, "Execute", _buttonStyle))
                    {
                        ExecuteCommand(command);
                    }


                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        GUI.Label(
                            new Rect(commandRect.x + 10f, commandRect.y + 35f, windowWidth - 60f, 50f),
                                command.Description,
                                _tooltipStyle ?? GUI.skin.label
                        );
                    }
                    yOffset += 100f;
                }

                GUI.EndScrollView();
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawCommandCategory: {ex}");
            }
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

        private void HandleWindowDragging()
        {
            try
            {
                Rect dragRect = new Rect(0, 0, _windowRect.width, 40);
                Event current = Event.current;

                if (current.type == EventType.MouseDown && current.button == 0 && dragRect.Contains(current.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = current.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                    current.Use();
                }

                else if (_isDragging && current.type == EventType.MouseUp)
                {
                    _isDragging = false;
                    SaveWindowPosition();
                    current.Use();
                }

                else if (_isDragging)
                {
                    _windowRect.x = current.mousePosition.x - _dragOffset.x;
                    _windowRect.y = current.mousePosition.y - _dragOffset.y;
                    _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
                    _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

                    GUI.changed = true;

                    if (current.type == EventType.MouseDrag)
                    {
                        current.Use();
                    }

                    if (!Input.GetMouseButton(0))
                    {
                        _isDragging = false;
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

        private void DrawPlayerExploitsUI(CommandCategory category)
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;
                float playerPanelHeight = 120f;

                Rect playerListRect = new Rect(20, 100, windowWidth, playerPanelHeight);
                GUI.Box(playerListRect, "", _panelStyle);

                GUI.Label(
                    new Rect(playerListRect.x + 10, playerListRect.y + 10, 150, 20),
                          "Players Online",
                          _commandLabelStyle ?? _labelStyle
                );

                DrawPlayerListHorizontal(playerListRect);

                _scrollPosition = GUI.BeginScrollView(
                    new Rect(20, 100 + playerPanelHeight + 10,
                             windowWidth, windowHeight - playerPanelHeight - 10),
                             _scrollPosition,
                             new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f));

                float yOffset = 0f;

                foreach (var command in category.Commands)
                {
                    Rect commandRect = new Rect(0, yOffset, windowWidth - 40f, 90f);
                    GUI.Box(commandRect, "", _panelStyle);

                    GUI.Label(
                        new Rect(commandRect.x + 10f, commandRect.y + 5f, 200f, 25f),
                              command.Name,
                              _commandLabelStyle ?? _labelStyle
                    );

                    float paramX = commandRect.x + 220f;

                    if (command.Parameters.Count > 0)
                    {
                        foreach (var param in command.Parameters)
                        {
                            Rect paramRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                            if (param.Type == ParameterType.Input)
                            {
                                string paramKey = $"param_{command.Name}_{param.Name}";

                                if (!_textFields.TryGetValue(paramKey, out var textField))
                                {
                                    textField = new CustomTextField(param.Value ?? "", _inputFieldStyle ?? GUI.skin.textField);
                                    _textFields[paramKey] = textField;
                                }
                                param.Value = textField.Draw(paramRect);
                            }

                            else if (param.Type == ParameterType.Dropdown)
                            {
                                if (GUI.Button(paramRect, param.Value ?? "Select", _buttonStyle))
                                {
                                    ShowDropdownMenu(param);
                                }
                            }
                            paramX += 130f;
                        }
                    }

                    Rect executeRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);
                    if (GUI.Button(executeRect, "Execute", _buttonStyle))
                    {
                        ExecuteCommand(command);
                    }

                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        GUI.Label(
                            new Rect(commandRect.x + 10f, commandRect.y + 35f, windowWidth - 60f, 50f),
                                  command.Description,
                                  _tooltipStyle ?? GUI.skin.label
                        );
                    }
                    yOffset += 100f;
                }

                GUI.EndScrollView();
            }

            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawPlayerExploitsUI: {ex}");
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
    }
}
