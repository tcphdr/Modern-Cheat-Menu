/*
 * Modern Cheat Menu
 * Core.Helpers.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
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
    }
}
