/*
 * Modern Cheat Menu
 * Core.HwidPatch.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
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
    }
}
