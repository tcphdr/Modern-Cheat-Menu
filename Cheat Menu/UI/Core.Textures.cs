namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private void CreateTextures()
        {
            try
            {
                _backgroundTexture = new Texture2D(1, 1);
                _backgroundTexture.SetPixel(0, 0, _backgroundColor);
                _backgroundTexture.Apply();
                _panelTexture = new Texture2D(1, 1);
                _panelTexture.SetPixel(0, 0, _panelColor);
                _panelTexture.Apply();
                _buttonNormalTexture = new Texture2D(1, 1);
                _buttonNormalTexture.SetPixel(0, 0, new Color(0.18f, 0.18f, 0.24f, 0.8f));
                _buttonNormalTexture.Apply();
                _buttonHoverTexture = new Texture2D(1, 1);
                _buttonHoverTexture.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.35f, 0.9f));
                _buttonHoverTexture.Apply();
                _buttonActiveTexture = new Texture2D(1, 1);
                _buttonActiveTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.4f, 1f));
                _buttonActiveTexture.Apply();
                _categoryTabTexture = _buttonNormalTexture;
                _categoryTabActiveTexture = _buttonActiveTexture;
                _categoryTabActiveTexture = _buttonActiveTexture;
                _settingsIconTexture = new Texture2D(16, 16);
                Color[] pixels = new Color[16 * 16];

                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(0, 0, 0, 0);

                Color iconColor = new Color(0.9f, 0.9f, 0.9f, 1f);

                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        float dx = x - 8;
                        float dy = y - 8;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        if (dist > 5 && dist < 7)
                            pixels[y * 16 + x] = iconColor;

                        if ((x == 1 || x == 14) && y >= 6 && y <= 9)
                            pixels[y * 16 + x] = iconColor;
                        if ((y == 1 || y == 14) && x >= 6 && x <= 9)
                            pixels[y * 16 + x] = iconColor;
                        if ((x == 3 || x == 12) && (y == 3 || y == 12))
                            pixels[y * 16 + x] = iconColor;
                    }
                }

                for (int y = 6; y <= 9; y++)
                {
                    for (int x = 6; x <= 9; x++)
                    {
                        float dx = x - 8;
                        float dy = y - 8;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        if (dist < 2)
                            pixels[y * 16 + x] = iconColor;
                    }
                }

                _settingsIconTexture.SetPixels(pixels);
                _settingsIconTexture.Apply();

            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error creating textures: {ex.Message}");
            }
        }

        private void ProtectUIResources()
        {
            Texture2D[] textures =
            {
                _backgroundTexture, _panelTexture, _buttonNormalTexture, _buttonHoverTexture,
                _buttonActiveTexture, _toggleOnTexture, _toggleOffTexture, _sliderThumbTexture,
                _sliderTrackTexture, _inputFieldTexture, _headerTexture, _categoryTabTexture,
                _categoryTabActiveTexture, _checkmarkTexture, _settingsIconTexture, _closeIconTexture,
                _glowTexture, _warningTexture, _settingsButtonTexture, _closeButtonTexture
            };
            foreach (Texture2D tex in textures)
                if (tex != null)
                    tex.hideFlags = HideFlags.HideAndDontSave;

            if (_customSkin != null)
                _customSkin.hideFlags = HideFlags.HideAndDontSave;
        }

        private void InitializeStyles()
        {
            try
            {
                _customSkin = ScriptableObject.CreateInstance<GUISkin>();
                _customSkin.box = new GUIStyle(GUI.skin.box);
                _customSkin.button = new GUIStyle(GUI.skin.button);
                _customSkin.label = new GUIStyle(GUI.skin.label);
                _customSkin.textField = new GUIStyle(GUI.skin.textField);
                _customSkin.toggle = new GUIStyle(GUI.skin.toggle);
                _customSkin.window = new GUIStyle(GUI.skin.window);
                _customSkin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
                _customSkin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);

                // Window style
                _windowStyle = new GUIStyle();
                _windowStyle.normal.background = _backgroundTexture;
                _windowStyle.border = new RectOffset(10, 10, 10, 10);
                _windowStyle.padding = new RectOffset(0, 0, 0, 0);
                _windowStyle.margin = new RectOffset(0, 0, 0, 0);

                // Panel style
                _panelStyle = new GUIStyle(GUI.skin.box);
                _panelStyle.normal.background = _panelTexture;
                _panelStyle.border = new RectOffset(8, 8, 8, 8);
                _panelStyle.margin = new RectOffset(10, 10, 10, 10);
                _panelStyle.padding = new RectOffset(10, 10, 10, 10);

                // Title style
                _titleStyle = new GUIStyle(GUI.skin.label);
                _titleStyle.fontSize = 20;
                _titleStyle.fontStyle = FontStyle.Bold;
                _titleStyle.normal.textColor = _textColor;
                _titleStyle.alignment = TextAnchor.MiddleCenter;
                _titleStyle.margin = new RectOffset(0, 0, 10, 15);

                // Header style
                _headerStyle = new GUIStyle(GUI.skin.box);
                _headerStyle.normal.background = _backgroundTexture;
                _headerStyle.border = new RectOffset(2, 2, 2, 2);
                _headerStyle.margin = new RectOffset(0, 0, 0, 10);
                _headerStyle.padding = new RectOffset(10, 10, 8, 8);
                _headerStyle.fontSize = 14;
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = _textColor;

                // Button style
                _buttonStyle = new GUIStyle(GUI.skin.button);
                _buttonStyle.normal.background = _buttonNormalTexture;
                _buttonStyle.hover.background = _buttonHoverTexture;
                _buttonStyle.active.background = _buttonActiveTexture;
                _buttonStyle.focused.background = _buttonNormalTexture;
                _buttonStyle.normal.textColor = _textColor;
                _buttonStyle.hover.textColor = Color.white;
                _buttonStyle.fontSize = 12;
                _buttonStyle.alignment = TextAnchor.MiddleCenter;
                _buttonStyle.margin = new RectOffset(5, 5, 2, 2);
                _buttonStyle.padding = new RectOffset(10, 10, 6, 6);
                _buttonStyle.border = new RectOffset(6, 6, 6, 6);

                // Icon button style
                _iconButtonStyle = new GUIStyle(_buttonStyle);
                _iconButtonStyle.padding = new RectOffset(6, 6, 6, 6);

                // Search box style
                _searchBoxStyle = new GUIStyle(GUI.skin.textField);
                _searchBoxStyle.fontSize = 14;
                _searchBoxStyle.margin = new RectOffset(0, 0, 5, 10);

                // Label style
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 12;
                _labelStyle.normal.textColor = _textColor;
                _labelStyle.alignment = TextAnchor.MiddleLeft;
                _labelStyle.padding = new RectOffset(5, 5, 2, 2);

                // Category styles
                _categoryButtonStyle = new GUIStyle(_buttonStyle);
                _categoryButtonActiveStyle = new GUIStyle(_buttonStyle);
                _categoryButtonActiveStyle.normal.background = _buttonActiveTexture;

                // Item button style
                _itemButtonStyle = new GUIStyle(_buttonStyle);
                _itemSelectedStyle = new GUIStyle(_itemButtonStyle);
                _itemSelectedStyle.normal.background = _buttonActiveTexture;

                _stylesInitialized = true;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing styles: {ex}");
            }
        }

        private void CreateButtonTextures()
        {
            // Create settings button texture (gear icon) - SMALLER (18x18)
            _settingsButtonTexture = new Texture2D(18, 18, TextureFormat.RGBA32, false);
            Color[] settingsPixels = new Color[18 * 18];
            for (int i = 0; i < settingsPixels.Length; i++)
                settingsPixels[i] = Color.clear;
            // Draw gear with light gray color instead of white
            Color iconColor = new Color(0.8f, 0.8f, 0.8f, 1.0f); // Light gray
            int center = 9; // Center of 18x18 texture
            int outerRadius = 7; // Smaller radius
            int innerRadius = 3; // Smaller inner radius
            for (int y = 0; y < 18; y++)
            {
                for (int x = 0; x < 18; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > innerRadius && dist < outerRadius)
                        settingsPixels[y * 18 + x] = iconColor;
                }
            }
            // Create spokes
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                float sx = Mathf.Sin(angle);
                float cx = Mathf.Cos(angle);
                for (int r = 6; r < 9; r++) // Adjusted for smaller size
                {
                    int x = (int)(center + cx * r);
                    int y = (int)(center + sx * r);
                    if (x >= 0 && x < 18 && y >= 0 && y < 18)
                        settingsPixels[y * 18 + x] = iconColor;
                }
            }
            _settingsButtonTexture.SetPixels(settingsPixels);
            _settingsButtonTexture.Apply();
            // Create close button texture (X icon) - SMALLER (18x18)
            _closeButtonTexture = new Texture2D(15, 15, TextureFormat.RGBA32, false);
            Color[] closePixels = new Color[15 * 15];
            for (int i = 0; i < closePixels.Length; i++)
                closePixels[i] = Color.clear;
            // Draw X with the same light gray color
            for (int i = 0; i < 15; i++)
            {
                int x1 = i;
                int y1 = i;
                int x2 = 14 - i;
                int y2 = i;
                // Draw diagonal lines with some thickness (but less for smaller size)
                for (int t = -1; t <= 1; t++) // Reduced thickness from -2,2 to -1,1
                {
                    int xt1 = x1 + t;
                    int yt1 = y1;
                    int xt2 = x2 + t;
                    int yt2 = y2;
                    if (xt1 >= 0 && xt1 < 15)
                        closePixels[yt1 * 15 + xt1] = iconColor;
                    if (xt2 >= 0 && xt2 < 15)
                        closePixels[yt2 * 15 + xt2] = iconColor;
                }
            }
            _closeButtonTexture.SetPixels(closePixels);
            _closeButtonTexture.Apply();
        }
    }
}
