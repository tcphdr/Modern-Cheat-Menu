/*
 * Modern Cheat Menu
 * Core.WorldCommands.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private void ToggleFreeCam(string[] args)
        {
            try
            {
                _freeCamEnabled = !_freeCamEnabled;

                // Close the menu.
                ToggleUI(false);

                // Toggle player camera
                if (Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance.SetCanLook(true);
                }

                // Toggle player movement
                if (Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance.CanMove = false;
                }

                // Toggle input system
                if (Il2CppScheduleOne.GameInput.Instance != null &&
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput != null)
                {
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput.m_InputActive = true;
                }

                Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance.SetFreeCam(_freeCamEnabled);

                ShowNotification("Free Camera", _freeCamEnabled ? "Enabled" : "Disabled", _freeCamEnabled ? NotificationType.Success : NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error toggling free camera: {ex.Message}");
                ShowNotification("Error", "Failed to toggle free camera", NotificationType.Error);
            }
        }

        private void SetWeather(string[] args)
        {
            // args[0] will now hold the string selected in the dropdown
            string selectedWeather = args[0];

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(selectedWeather);

                var cmd = new Il2CppScheduleOne.Console.SetWeather();
                cmd.Execute(commandList);

                ShowNotification("Weather", $"Changed to {selectedWeather}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting weather: {ex.Message}");
            }
        }

        private void SetWorldTime(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int time))
            {
                LoggerInstance.Error("Invalid scale! Please enter a number.");
                ShowNotification("Error", "Invalid time scale value", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(time.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetTimeCommand();
                cmd.Execute(commandList);

                ShowNotification("World Time", $"Set world time to {time}!", NotificationType.Success);
            }

            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set time: {ex.Message}");
            }
        }

        private void SetTimeScale(string[] args)
        {
            if (args.Length < 1 || !float.TryParse(args[0], out float scale))
            {
                LoggerInstance.Error("Invalid scale! Please enter a number.");
                ShowNotification("Error", "Invalid time scale value", NotificationType.Error);
                return;
            }

            try
            {
                // Clamp to reasonable range
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(scale.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetTimeScale();
                cmd.Execute(commandList);

                ShowNotification("Time Scale", $"Set to {scale}.", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set time scale: {ex.Message}");
            }
        }

        private void GrowPlants(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.GrowPlants();
                command.Execute(null);

                ShowNotification("World", "All weed plants have been instantly grown!", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error calling GrowPlants: {ex.Message}");
            }
        }

        private void ClearTrash(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearTrash();
                command.Execute(null);
                ShowNotification("World", "Cleared all trash", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error clearing world trash: {ex.Message}");
                ShowNotification("Error", "Failed to clear trash", NotificationType.Error);
            }
        }

        private void SetLawIntensity(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("intensity amount required!");
                ShowNotification("Error", "intensity amount required", NotificationType.Error);
                return;
            }

            // Try to parse the first argument into an integer
            if (!int.TryParse(args[0], out int intensity))
            {
                LoggerInstance.Error("Invalid intensity amount! Please enter a valid number.");
                ShowNotification("Error", "Invalid intensity value", NotificationType.Error);
                return;
            }

            try
            {
                // Create a list of arguments (using the IL2CPP version of List with string as the type parameter)
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(intensity.ToString());

                // Create an instance of the SetHealth command
                var cmd = new Il2CppScheduleOne.Console.SetLawIntensity();

                // Execute the command with the argument list
                cmd.Execute(commandList);

                ShowNotification("Law Intensity", $"Set to {intensity}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting law intensity: {ex.Message}");
                ShowNotification("Error", "Failed to set law intensity.", NotificationType.Error);
            }
        }

        private void TeleportPlayer(Vector3 position)
        {
            try
            {
                var localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    LoggerInstance.Error("Failed to find local player for teleportation!");
                    ShowNotification("Teleport", "Player not found", NotificationType.Error);
                    return;
                }

                // Directly set player position
                localPlayer.transform.position = position;

                // Also update text fields with new position for reference
                if (_textFields.TryGetValue("teleport_x", out var xField))
                    xField.Value = position.x.ToString("F1");
                if (_textFields.TryGetValue("teleport_y", out var yField))
                    yField.Value = position.y.ToString("F1");
                if (_textFields.TryGetValue("teleport_z", out var zField))
                    zField.Value = position.z.ToString("F1");

                ShowNotification("Teleport", "Teleported successfully", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error teleporting player: {ex.Message}");
                ShowNotification("Error", "Failed to teleport player", NotificationType.Error);
            }
        }
    }
}
