/*
 * Modern Cheat Menu
 * Core.SelfCommands.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private void PatchImpactNetworkMethods()
        {
            try
            {
                LoggerInstance.Msg("Setting up godmode network method patches...");

                // Save the local player name for comparison
                var localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    _localPlayerName = localPlayer.name;
                    LoggerInstance.Msg($"Local player identified as: {_localPlayerName}");
                }
                else
                {
                    LoggerInstance.Error("Failed to identify local player for godmode!");
                    return;
                }

                // Use System.Type instead of Il2CppSystem.Type
                var playerHealthType = typeof(PlayerHealth);

                var blockMethod = typeof(Core).GetMethod("BlockNetworkDamageMethod",
                                                         BindingFlags.Static | BindingFlags.NonPublic);

                if (blockMethod == null)
                {
                    LoggerInstance.Error("BlockNetworkDamageMethod not found!");
                    return;
                }

                var prefix = new HarmonyMethod(blockMethod);

                // Patch TakeDamage methods
                PatchMethod(playerHealthType, "RpcWriter___Observers_TakeDamage_3505310624", prefix);
                PatchMethod(playerHealthType, "RpcLogic___TakeDamage_3505310624", prefix);
                PatchMethod(playerHealthType, "RpcReader___Observers_TakeDamage_3505310624", prefix);

                // Patch Die methods
                PatchMethod(playerHealthType, "RpcWriter___Observers_Die_2166136261", prefix);
                PatchMethod(playerHealthType, "RpcLogic___Die_2166136261", prefix);
                PatchMethod(playerHealthType, "RpcReader___Observers_Die_2166136261", prefix);

                LoggerInstance.Msg("Successfully patched PlayerHealth network methods");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error patching network damage methods: {ex}");
            }
        }

        private static bool BlockNetworkDamageMethod()
        {
            return !_staticPlayerGodmodeEnabled;
        }

        private void ToggleGodmode(string[] args)
        {
            try
            {
                // Toggle godmode state
                _playerGodmodeEnabled = !_playerGodmodeEnabled;
                _staticPlayerGodmodeEnabled = _playerGodmodeEnabled;

                if (_playerGodmodeEnabled)
                {
                    // Update local player name for checking
                    var localPlayer = FindLocalPlayer();
                    if (localPlayer != null)
                    {
                        _localPlayerName = localPlayer.name;
                    }
                    else
                    {
                        LoggerInstance.Error("Failed to identify local player for godmode!");
                    }

                    // Patch network methods to block damage for local player only
                    PatchImpactNetworkMethods();

                    // Start the godmode coroutine
                    if (_godModeCoroutine == null)
                    {
                        _godModeCoroutine = MelonCoroutines.Start(GodModeRoutine());
                    }

                    ShowNotification("Godmode", "Enabled network patches.", NotificationType.Success);
                }
                else
                {
                    // Stop the godmode coroutine if it's running
                    if (_godModeCoroutine != null)
                    {
                        MelonCoroutines.Stop(_godModeCoroutine);
                        _godModeCoroutine = null;
                    }

                    // Clear local player name
                    _localPlayerName = "";

                    // Attempt to unpatch methods
                    try
                    {
                        HarmonyLib.Harmony.UnpatchAll();
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error unpatching methods: {ex.Message}");
                    }

                    ShowNotification("Godmode", "Disabled network patches.", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error toggling godmode: {ex.Message}");
                ShowNotification("Error", "Failed to toggle godmode", NotificationType.Error);
            }
        }

        private IEnumerator GodModeRoutine()
        {
            while (_staticPlayerGodmodeEnabled)
            {
                try
                {
                    // Find the local player
                    var localPlayer = FindLocalPlayer();
                    if (localPlayer != null)
                    {
                        // Get the health component
                        var playerHealth = GetPlayerHealth(localPlayer);
                        if (playerHealth != null)
                        {
                            // Set health to maximum using native method
                            playerHealth.SetHealth(PlayerHealth.MAX_HEALTH);

                            // Revive player if not alive using native method
                            if (!playerHealth.IsAlive)
                            {
                                // Use the native revive method with current position and rotation
                                playerHealth.Revive(
                                    playerHealth.transform.position,
                                    playerHealth.transform.rotation
                                );
                            }

                            // Remove any lethal effects
                            playerHealth.SetAfflictedWithLethalEffect(false);

                            // Optional: Recover health
                            playerHealth.RecoverHealth(PlayerHealth.HEALTH_RECOVERY_PER_MINUTE);

                            // Prevent death events
                            if (playerHealth.onDie != null)
                            {
                                playerHealth.onDie.RemoveAllListeners();
                            }
                        }
                        else
                        {
                            LoggerInstance.Error("Local player health component not found!");
                        }
                    }
                    else
                    {
                        LoggerInstance.Error("Local player not found in godmode routine!");
                    }
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.5f);
            }
        }

        private Equippable_RangedWeapon GetEquippedWeapon()
        {
            try
            {
                // Use cached weapon if recent
                if (_cachedWeapon != null &&
                    Time.time - _lastWeaponCheckTime < WEAPON_CACHE_INTERVAL)
                {
                    return _cachedWeapon;
                }

                // Reset cache time
                _lastWeaponCheckTime = Time.time;

                // Find player object
                var playerObject = FindPlayerNetworkObject();
                if (playerObject == null)
                {
                    LoggerInstance.Error("Cannot find player object for weapon detection.");
                    return null;
                }

                // Attempt to find weapon through different methods
                Equippable_RangedWeapon foundWeapon = null;

                // Method 1: Direct component search on player object
                foundWeapon = playerObject.GetComponent<Equippable_RangedWeapon>();

                // Method 2: Search in player's children
                if (foundWeapon == null)
                {
                    foundWeapon = playerObject.GetComponentInChildren<Equippable_RangedWeapon>();
                }

                // Method 3: Reflection-based search in player components
                if (foundWeapon == null)
                {
                    var components = playerObject.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        try
                        {
                            // Look for properties that might contain the weapon
                            var properties = component.GetType().GetProperties(
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            foreach (var prop in properties)
                            {
                                if (prop.Name.Contains("Weapon") || prop.Name.Contains("Equipped") || prop.Name.Contains("CurrentItem"))
                                {
                                    var value = prop.GetValue(component);
                                    if (value is Equippable_RangedWeapon rangedWeapon)
                                    {
                                        foundWeapon = rangedWeapon;
                                        break;
                                    }
                                }
                            }

                            if (foundWeapon != null) break;
                        }
                        catch { }
                    }
                }

                // Fallback: Direct type search
                if (foundWeapon == null)
                {
                    var weapons = Resources.FindObjectsOfTypeAll<Equippable_RangedWeapon>();
                    foundWeapon = weapons.FirstOrDefault(w =>
                    w != null &&
                    w.gameObject != null &&
                    w.gameObject.name.Contains("(Clone)"));
                }

                // Cache the weapon
                if (foundWeapon != null)
                {
                    _cachedWeapon = foundWeapon;
                    return foundWeapon;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in weapon detection: {ex.Message}");
                return null;
            }
        }

        public void ToggleUnlimitedAmmo(string[] args)
        {
            try
            {
                _unlimitedAmmoEnabled = !_unlimitedAmmoEnabled;

                if (_unlimitedAmmoEnabled)
                {
                    if (_unlimitedAmmoCoroutine == null)
                    {
                        _unlimitedAmmoCoroutine = MelonCoroutines.Start(UnlimitedAmmoRoutine());
                    }

                    ShowNotification("Unlimited Ammo", "Enabled", NotificationType.Success);
                }
                else
                {
                    if (_unlimitedAmmoCoroutine != null)
                    {
                        MelonCoroutines.Stop(_unlimitedAmmoCoroutine);
                        _unlimitedAmmoCoroutine = null;
                    }

                    ShowNotification("Unlimited Ammo", "Disabled", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error toggling unlimited ammo: {ex.Message}");
                ShowNotification("Error", "Failed to toggle unlimited ammo", NotificationType.Error);
            }
        }

        private IEnumerator UnlimitedAmmoRoutine()
        {
            while (_unlimitedAmmoEnabled)
            {
                try
                {
                    // Get the weapon more efficiently
                    var weapon = GetEquippedWeapon();

                    // Only process if weapon is actually equipped
                    if (weapon != null)
                    {
                        // Ensure the weapon is usable
                        if (weapon.weaponItem != null)
                        {
                            // Directly set magazine to full
                            if (weapon.weaponItem.Value < weapon.MagazineSize)
                            {
                                weapon.weaponItem.Value = weapon.MagazineSize;
                            }
                        }

                        // Specific handling for different weapon types
                        if (weapon is Equippable_Revolver revolver)
                        {
                            revolver.SetDisplayedBullets(revolver.MagazineSize);
                        }

                        // Prevent unnecessary reloading
                        if (weapon.IsReloading)
                        {
                            // Quick way to stop reload
                            weapon.TimeSinceFire = float.MaxValue;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error in unlimited ammo routine: {ex.Message}");
                }

                // Wait slightly longer to reduce performance impact
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void ToggleNeverWanted(string[] args)
        {
            try
            {
                _playerNeverWantedEnabled = !_playerNeverWantedEnabled;

                if (_playerNeverWantedEnabled)
                {
                    if (_neverWantedCoroutine == null)
                    {
                        _neverWantedCoroutine = MelonCoroutines.Start(NeverWantedRoutine());
                    }
                    ShowNotification("Never Wanted", "Enabled", NotificationType.Success);
                }
                else
                {
                    if (_neverWantedCoroutine != null)
                    {
                        MelonCoroutines.Stop(_neverWantedCoroutine);
                        _neverWantedCoroutine = null;
                    }

                    ShowNotification("Never Wanted", "Disabled", NotificationType.Info);
                }
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error toggling never wanted: {ex.Message}");
                ShowNotification("Error", "Failed to toggle never wanted", NotificationType.Error);
            }
        }

        private IEnumerator NeverWantedRoutine()
        {
            while (true)
            {
                try
                {
                    // Clear Wanted Level
                    ClearWantedLevelEx(null);
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.2f);
            }
        }

        private void ChangeXP(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int amount))
            {
                ShowNotification("Error", "Invalid XP amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.GiveXP();
                cmd.Execute(commandList);

                ShowNotification("XP", $"Changed by ${amount}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error changing XP: {ex.Message}");
                ShowNotification("Error", "Failed to change XP amount", NotificationType.Error);
            }
        }

        private void ChangeCash(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int amount))
            {
                ShowNotification("Error", "Invalid cash amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.ChangeCashCommand();
                cmd.Execute(commandList);

                ShowNotification("Cash", $"Changed by ${amount}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error changing cash: {ex.Message}");
                ShowNotification("Error", "Failed to change cash amount", NotificationType.Error);
            }
        }

        private void ChangeBalance(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int amount))
            {
                ShowNotification("Error", "Invalid balance amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand();
                cmd.Execute(commandList);

                ShowNotification("Online Balance", $"Changed by ${amount}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error changing balance: {ex.Message}");
                ShowNotification("Error", "Failed to change online balance", NotificationType.Error);
            }
        }

        private void SetPlayerMovementSpeed(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Movement speed amount required.");
                ShowNotification("Error", "Movement speed reserve amount required.", NotificationType.Error);
            }

            if (!int.TryParse(args[0], out int speed))
            {
                LoggerInstance.Error("Invalid amount, please enter a valid number.");
                ShowNotification("Error", "Invalid movement speed amount specificed.", NotificationType.Error);
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(speed.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetMoveSpeedCommand();
                cmd.Execute(commandList);

                ShowNotification("Movement Speed", $"Set speed to {speed}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set player movement speed: {ex.Message}");
            }
        }

        private void SetPlayerStaminaReserve(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Stamina amount required.");
                ShowNotification("Error", "Stamina reserve amount required.", NotificationType.Error);
            }

            if (!int.TryParse(args[0], out int reserve))
            {
                LoggerInstance.Error("Invalid amount, please enter a valid number.");
                ShowNotification("Error", "Invalid stamina reserve amount specificed.", NotificationType.Error);
            }
            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(reserve.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetStaminaReserve();
                cmd.Execute(commandList);

                ShowNotification("Stamina Reserve", $"Successfuly set stamina reserve to {reserve}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set player stamina reserve: {ex.Message}");
            }
        }

        private void SetJumpForce(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Force amount required!");
                ShowNotification("Error", "Force amount required", NotificationType.Error);
                return;
            }

            // Try to parse the first argument into an integer
            if (!int.TryParse(args[0], out int force))
            {
                LoggerInstance.Error("Invalid force amount! Please enter a valid number.");
                ShowNotification("Error", "Invalid force value", NotificationType.Error);
                return;
            }

            try
            {
                // Create a list of arguments (using the IL2CPP version of List with string as the type parameter)
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(force.ToString());

                // Create an instance of the SetHealth command
                var cmd = new Il2CppScheduleOne.Console.SetJumpMultiplier();

                // Execute the command with the argument list
                cmd.Execute(commandList);

                ShowNotification("Jump Force", $"Set to {force}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting jump force: {ex.Message}");
                ShowNotification("Error", "Failed to set jump force.", NotificationType.Error);
            }
        }

        private void ClearInventory(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearInventoryCommand();
                command.Execute(null);
                LoggerInstance.Msg("Inventory cleared.");
                ShowNotification("Inventory", "Cleared all items", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error: {ex.Message}");
                ShowNotification("Error", "Failed to clear inventory", NotificationType.Error);
            }
        }

        private void ToggleAlwaysVisibleCrosshair(string[] args)
        {
            try
            {
                _forceCrosshairAlwaysVisible = !_forceCrosshairAlwaysVisible;

                // Apply patch if turning on, remove patch if turning off
                if (_forceCrosshairAlwaysVisible)
                {
                    ApplyCrosshairPatch();
                    ShowNotification("Crosshair", "Always visible crosshair enabled", NotificationType.Success);
                }
                else
                {
                    RemoveCrosshairPatch();
                    ShowNotification("Crosshair", "Always visible crosshair disabled", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error toggling always visible crosshair: {ex.Message}");
                ShowNotification("Error", "Failed to toggle crosshair visibility", NotificationType.Error);
            }
        }

        private void ApplyCrosshairPatch()
        {
            try
            {
                // Find the HUD.SetCrosshairVisible method
                var hudType = typeof(Il2CppScheduleOne.UI.HUD);
                var setCrosshairMethod = hudType.GetMethod("SetCrosshairVisible",
                                                           BindingFlags.Public | BindingFlags.Instance);

                if (setCrosshairMethod == null)
                {
                    LoggerInstance.Error("Could not find SetCrosshairVisible method!");
                    return;
                }

                // Create and apply the prefix patch
                var patchMethod = typeof(Core).GetMethod("CrosshairVisibilityPatch",
                                                         BindingFlags.Static | BindingFlags.NonPublic);

                if (patchMethod == null)
                {
                    LoggerInstance.Error("CrosshairVisibilityPatch method not found!");
                    return;
                }

                _harmony.Patch(setCrosshairMethod, prefix: new HarmonyLib.HarmonyMethod(patchMethod));
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error applying crosshair patch: {ex.Message}");
            }
        }

        private void RemoveCrosshairPatch()
        {
            try
            {
                // Find the HUD.SetCrosshairVisible method
                var hudType = typeof(Il2CppScheduleOne.UI.HUD);
                var setCrosshairMethod = hudType.GetMethod("SetCrosshairVisible",
                                                           BindingFlags.Public | BindingFlags.Instance);

                if (setCrosshairMethod == null)
                {
                    LoggerInstance.Error("Could not find SetCrosshairVisible method!");
                    return;
                }

                // Remove the patch
                _harmony.Unpatch(setCrosshairMethod, HarmonyPatchType.Prefix, _harmony.Id);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error removing crosshair patch: {ex.Message}");
            }
        }

        private static bool CrosshairVisibilityPatch(ref bool vis)
        {
            // If called with false, modify to true to keep crosshair visible
            if (!vis)
            {
                vis = true;
            }
            // Return true to allow original method to run (with our modified parameter)
            return true;
        }

        private void RaiseWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.RaisedWanted();
                command.Execute(null);
                ShowNotification("Wanted Level", "Increased", NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error raising wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to raise wanted level", NotificationType.Error);
            }
        }

        private void LowerWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.LowerWanted();
                command.Execute(null);
                ShowNotification("Wanted Level", "Decreased", NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error lowering wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to lower wanted level", NotificationType.Error);
            }
        }

        private void ClearWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearWanted();
                command.Execute(null);
                ShowNotification("Wanted Level", "Cleared", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error clearing wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to clear wanted level", NotificationType.Error);
            }
        }

        private void ClearWantedLevelEx(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearWanted();
                command.Execute(null);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error clearing wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to clear wanted level", NotificationType.Error);
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
    }
}
