/*
 * Modern Cheat Menu
 * Core.ItemCommands.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
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
    }
}
