/*
 * Modern Cheat Menu
 * Core.Keybings.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
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

        private void StartCaptureKeybind(MelonPreferences_Entry<string> keybindEntry)
        {
            _isCapturingKey = true;
            _currentKeyCaptureEntry = keybindEntry;
            ShowNotification("Keybind", "Press any key to set binding...", NotificationType.Info);
        }
    }
}
