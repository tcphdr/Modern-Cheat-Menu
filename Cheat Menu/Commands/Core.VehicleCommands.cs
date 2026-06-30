/*
 * Modern Cheat Menu
 * Core.VehicleCommands.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private void SpawnVehicle(string[] args)
        {
            try
            {
                string vehicle = "cheetah"; // Default to shitbox if no valid argument is provided

                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    // Take the first argument as the vehicle type
                    vehicle = args[0].ToLowerInvariant(); // Convert to lowercase
                }

                // Create command parameter list
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(vehicle);

                // Execute the command
                var cmd = new Il2CppScheduleOne.Console.SpawnVehicleCommand();
                cmd.Execute(commandList);

                ShowNotification("Vehicle", $"Spawned {vehicle}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error spawning vehicle: {ex.Message}");
                ShowNotification("Error", "Failed to spawn vehicle", NotificationType.Error);
            }
        }
    }
}
