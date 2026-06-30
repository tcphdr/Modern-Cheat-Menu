namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private Dictionary<string, object> _explodeLoopCoroutines = new Dictionary<string, object>();

        private void StartExplodeLoop(OnlinePlayerInfo playerInfo)
        {
            string playerKey = playerInfo.Player.GetInstanceID().ToString();
            if (_explodeLoopCoroutines.ContainsKey(playerKey))
            {
                MelonCoroutines.Stop(_explodeLoopCoroutines[playerKey]);
            }

            _explodeLoopCoroutines[playerKey] = MelonCoroutines.Start(ExplodeLoopRoutine(playerInfo));
        }

        private void StopExplodeLoop(OnlinePlayerInfo playerInfo)
        {
            try
            {
                string playerKey = playerInfo.Player.GetInstanceID().ToString();

                // Stop the coroutine if it exists
                if (_explodeLoopCoroutines.ContainsKey(playerKey))
                {
                    MelonCoroutines.Stop(_explodeLoopCoroutines[playerKey]);
                    _explodeLoopCoroutines.Remove(playerKey);
                }

                // Reset explode loop state
                playerInfo.ExplodeLoop = false;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error stopping explosion loop: {ex.Message}");
            }
        }

        private IEnumerator ExplodeLoopRoutine(OnlinePlayerInfo playerInfo)
        {
            string playerKey = playerInfo.Player.GetInstanceID().ToString();

            while (playerInfo.ExplodeLoop && playerInfo.Player != null)
            {
                try
                {
                    // Create explosion at player position
                    Vector3 explosionPosition = playerInfo.Player.transform.position;
                    CreateServerSideExplosion(explosionPosition, 99999999999999f, 2f);
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Error in explosion loop: {ex.Message}");
                }

                // Wait before next explosion
                yield return new WaitForSeconds(0.09f);
            }
            _explodeLoopCoroutines.Remove(playerKey);
        }

        private void CreateExplosion(string[] args)
        {
            try
            {
                // Parse optional parameters (damage and radius)
                float damage = 99999999999999f;
                float radius = 2f;
                string target = "custom";
                bool serverSide = true;

                // Parse arguments with more flexibility
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLowerInvariant();

                    // Existing target parsing
                    if (arg == "all" || arg == "random" || arg == "nukeall")
                    {
                        target = arg;
                        continue;
                    }

                    // Try parsing as damage or radius
                    if (float.TryParse(arg, out float numericValue))
                    {
                        // First numeric value is damage, second is radius
                        if (damage == 50f)
                            damage = numericValue;
                        else if (radius == 5f)
                            radius = numericValue;
                    }
                }

                // Find all players
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;

                if (playerList == null || playerList.Count == 0)
                {
                    ShowNotification("Explosion", "No players found!", NotificationType.Error);
                    return;
                }

                // Explosion positions
                List<Vector3> explosionPositions = new List<Vector3>();

                switch (target)
                {
                    case "nukeall":
                        damage = 99999999999999f;
                        goto case "all";

                    case "all":
                        foreach (var player in playerList)
                        {
                            if (player != null && player.transform != null)
                            {
                                explosionPositions.Add(player.transform.position);
                            }
                        }
                        break;

                    case "random":
                        var randomPlayer = playerList[UnityEngine.Random.Range(0, playerList.Count)];
                        if (randomPlayer != null && randomPlayer.transform != null)
                        {
                            explosionPositions.Add(randomPlayer.transform.position);
                        }
                        break;

                    default: // custom or default
                        // Try to do a raycast from camera to find target position
                        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                        RaycastHit hit;

                        Vector3 explosionPosition;
                        if (Physics.Raycast(ray, out hit, 100f))
                        {
                            // Use hit position
                            explosionPosition = hit.point;
                        }
                        else
                        {
                            // Use position a few meters in front of camera
                            explosionPosition = Camera.main.transform.position + Camera.main.transform.forward * 5f;
                        }
                        explosionPositions.Add(explosionPosition);
                        break;
                }

                // Create explosions at each target position
                foreach (Vector3 explosionPos in explosionPositions)
                {
                    CreateServerSideExplosion(explosionPos, damage, radius);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error creating explosion: {ex.Message}");
                ShowNotification("Error", "Failed to create explosion", NotificationType.Error);
            }
        }

        private void CreateServerSideExplosion(Vector3 position, float damage = 50f, float radius = 5f)
        {
            try
            {
                // Create explosion data
                //ExplosionData explosionData = new ExplosionData(radius, damage, radius * 2.0f);
                ExplosionData explosionData = new ExplosionData(radius, damage, radius * 2.0f, false, (EExplosionType)0);

                // Get the CombatManager instance
                var combatManager = CombatManager.Instance;
                if (combatManager == null)
                {
                    LoggerInstance.Error("CombatManager instance is NULL!");
                    return;
                }

                // Generate a unique explosion ID
                int explosionId = UnityEngine.Random.Range(0, 10000);

                // Try multiple methods to ensure explosion visibility and damage
                try
                {
                    // Method 1: Direct CreateExplosion
                    combatManager.CreateExplosion(position, explosionData, explosionId);
                }
                catch (Exception createEx)
                {
                    LoggerInstance.Error($"CreateExplosion failed: {createEx.Message}");
                }

                try
                {
                    // Method 2: Explicit Explosion method
                    combatManager.Explosion(position, explosionData, explosionId);
                }
                catch (Exception explodeEx)
                {
                    LoggerInstance.Error($"Explosion method failed: {explodeEx.Message}");
                }

                try
                {
                    // Method 3: Observers RPC method
                    var observersMethod = combatManager.GetType().GetMethod("RpcWriter___Observers_Explosion_2907189355", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (observersMethod != null)
                    {
                        observersMethod.Invoke(combatManager, new object[] { position, explosionData, explosionId });
                    }
                }
                catch (Exception observersEx)
                {
                    LoggerInstance.Error($"Observers RPC method failed: {observersEx.Message}");
                }

                // Additional diagnostic checks
                try
                {
                    // Check for explosion prefab
                    var explosionPrefab = combatManager.ExplosionPrefab;
                    if (explosionPrefab != null)
                    {
                        // Instantiate explosion prefab manually
                        var instantiatedExplosion = UnityEngine.Object.Instantiate(explosionPrefab.gameObject, position, Quaternion.identity);
                        instantiatedExplosion.transform.position = position;
                    }
                    else
                    {
                        LoggerInstance.Error("No explosion prefab found!");
                    }
                }
                catch (Exception prefabEx)
                {
                    LoggerInstance.Error($"Explosion prefab error: {prefabEx.Message}");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Server-side explosion error: {ex.Message}");
                ShowNotification("Error", "Failed to create server-side explosion", NotificationType.Error);
            }
        }

        private void KillPlayerCommand(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int playerIndex) || playerIndex < 1)
            {
                LoggerInstance.Error("Invalid player index! Please enter a valid number.");
                ShowNotification("Error", "Invalid player index", NotificationType.Error);
                return;
            }

            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                playerIndex--; // Convert to 0-based index

                if (playerList != null && playerIndex >= 0 && playerIndex < playerList.Count)
                {
                    var player = playerList[playerIndex];
                    if (player == null)
                    {
                        LoggerInstance.Error("Player is null!");
                        ShowNotification("Error", "Player is null", NotificationType.Error);
                        return;
                    }

                    //ServerExecuteKillPlayer(player);
                    ServerExecuteDamagePlayer(player, 99999999999999f);
                    ShowNotification("Player", $"Sent kill request for {player.name}", NotificationType.Success);

                    var playerHealth = GetPlayerHealth(player);
                    if (playerHealth != null)
                    {
                        playerHealth.Die();
                        LoggerInstance.Msg("Killed local player");
                        ShowNotification("Player", "Killed local player", NotificationType.Success);
                    }
                }
                else
                {
                    LoggerInstance.Error("Player index out of range!");
                    ShowNotification("Error", "Player index out of range", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error killing player: {ex.Message}");
                ShowNotification("Error", "Failed to kill player", NotificationType.Error);
            }
        }

        private void KillAllPlayersCommand(string[] args)
        {
            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList == null || playerList.Count == 0)
                {
                    LoggerInstance.Error("No players found!");
                    ShowNotification("Players", "No players found", NotificationType.Error);
                    return;
                }

                int killedCount = 0;

                for (int i = 0; i < playerList.Count; i++)
                {
                    var player = playerList[i];
                    if (player == null) continue;

                    // Skip the local player
                    if (IsLocalPlayer(player))
                    {
                        LoggerInstance.Msg($"Skipping local player: {player.name}");
                        continue;
                    }

                    // Kill the remote player
                    ServerExecuteDamagePlayer(player, 99999999999999f);
                    killedCount++;
                }
                ShowNotification("Players", $"Kill requests sent for {killedCount} players", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error killing all players: {ex.Message}");
                ShowNotification("Error", "Failed to kill all players", NotificationType.Error);
            }
        }

        private void DamagePlayerCommand(string[] args)
        {
            if (args.Length < 2 ||
                !int.TryParse(args[0], out int playerIndex) || playerIndex < 1 ||
                !float.TryParse(args[1], out float damage))
            {
                LoggerInstance.Error("Invalid parameters! Please enter valid player index and damage amount.");
                ShowNotification("Error", "Invalid parameters", NotificationType.Error);
                return;
            }

            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                playerIndex--; // Convert to 0-based index

                if (playerList != null && playerIndex >= 0 && playerIndex < playerList.Count)
                {
                    var player = playerList[playerIndex];
                    if (player == null)
                    {
                        LoggerInstance.Error("Player is null!");
                        ShowNotification("Error", "Player is null", NotificationType.Error);
                        return;
                    }

                    // Check if it's the local player
                    if (IsLocalPlayer(player))
                    {
                        // For local player, we can use the direct method
                        var playerHealth = GetPlayerHealth(player);
                        if (playerHealth != null)
                        {
                            playerHealth.TakeDamage(damage, true, true);
                            ShowNotification("Player", "Damaged local player", NotificationType.Success);
                        }
                        else
                        {
                            LoggerInstance.Error("Local player health component not found!");
                            ShowNotification("Error", "Health component not found", NotificationType.Error);
                        }
                    }
                    else
                    {
                        // For other players, use the server method
                        ServerExecuteDamagePlayer(player, damage);
                        ShowNotification("Player", $"Sent damage request for {player.name}", NotificationType.Success);
                    }
                }
                else
                {
                    LoggerInstance.Error("Player index out of range!");
                    ShowNotification("Error", "Player index out of range", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error damaging player: {ex.Message}");
                ShowNotification("Error", "Failed to damage player", NotificationType.Error);
            }
        }

        private void ServerExecuteDamagePlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer, float damageAmount)
        {
            try
            {
                var playerHealth = GetPlayerHealth(targetPlayer);
                if (playerHealth == null)
                {
                    LoggerInstance.Error("PlayerHealth not found on player!");
                    return;
                }

                // For damage, we use RpcLogic___TakeDamage, but first need to call RpcWriter to send to server
                try
                {
                    // This is the key - we send to the server, not directly to the client
                    playerHealth.RpcWriter___Observers_TakeDamage_3505310624(damageAmount, true, true);
                }
                catch (Exception e)
                {
                    LoggerInstance.Error($"Failed using RpcWriter: {e.Message}");
                    try
                    {
                        playerHealth.TakeDamage(damageAmount, true, true);
                    }
                    catch (Exception e2)
                    {
                        LoggerInstance.Error($"All damage methods failed: {e2.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in ServerExecuteDamagePlayer: {ex.Message}");
            }
        }

        private void ServerExecuteKillPlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer)
        {
            try
            {
                var playerHealth = GetPlayerHealth(targetPlayer);
                if (playerHealth == null) return;

                // Prefer Server SendDie for remote players
                playerHealth.RpcWriter___Server_SendDie_2166136261();
                playerHealth.RpcWriter___Observers_TakeDamage_3505310624(99999999999999f, true, true);

                LoggerInstance.Msg($"Killed player: {targetPlayer.name}");
                ShowNotification("Player", $"Killed {targetPlayer.name}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error killing player: {ex.Message}");
                ShowNotification("Error", "Failed to kill player", NotificationType.Error);
            }
        }

        private void TeleportAllPlayersToMe()
        {
            try
            {
                var localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    ShowNotification("Teleport", "Local player not found", NotificationType.Error);
                    return;
                }

                Vector3 myPosition = localPlayer.transform.position;
                int teleportCount = 0;

                foreach (var playerInfo in _onlinePlayers)
                {
                    if (playerInfo == null || playerInfo.Player == null || playerInfo.IsLocal)
                        continue;

                    try
                    {

                        // Method 1: Try using SetTransform component
                        var setTransform = playerInfo.Player.GetComponent<Il2CppScheduleOne.DevUtilities.SetTransform>();
                        if (setTransform == null)
                        {
                            // Add the component if it doesn't exist
                            setTransform = playerInfo.Player.gameObject.AddComponent<Il2CppScheduleOne.DevUtilities.SetTransform>();
                        }

                        if (setTransform != null)
                        {
                            // Slight random offset to avoid players stacking on top of each other
                            Vector3 offset = new Vector3(
                                UnityEngine.Random.Range(-1.5f, 1.5f),
                                                         0,
                                                         UnityEngine.Random.Range(-1.5f, 1.5f)
                            );

                            // Configure the SetTransform component
                            setTransform.SetOnUpdate = true;
                            setTransform.SetPosition = true;
                            setTransform.LocalPosition = myPosition + offset;
                            playerInfo.Player.Update();

                            // Call Set to apply the transform immediately
                            setTransform.Set();

                            LoggerInstance.Msg($"Applied SetTransform to {playerInfo.Player.name}");
                            teleportCount++;
                        }
                        else
                        {
                            // Fallback method: Try the PlayerMovement.Teleport method
                            var playerMovement = playerInfo.Player.GetComponent<Il2CppScheduleOne.PlayerScripts.PlayerMovement>();
                            if (playerMovement != null)
                            {
                                Vector3 offset = new Vector3(
                                    UnityEngine.Random.Range(-1.5f, 1.5f),
                                                             0,
                                                             UnityEngine.Random.Range(-1.5f, 1.5f)
                                );
                                playerMovement.Teleport(myPosition + offset);
                                LoggerInstance.Msg($"Used PlayerMovement.Teleport for {playerInfo.Player.name}");
                                teleportCount++;
                            }
                            else
                            {
                                // Last resort: direct transform position modification
                                Vector3 offset = new Vector3(
                                    UnityEngine.Random.Range(-1.5f, 1.5f),
                                                             0,
                                                             UnityEngine.Random.Range(-1.5f, 1.5f)
                                );
                                playerInfo.Player.transform.position = myPosition + offset;
                                LoggerInstance.Msg($"Used direct transform position for {playerInfo.Player.name}");
                                teleportCount++;
                            }
                        }
                    }
                    catch (Exception playerEx)
                    {
                        LoggerInstance.Error($"Failed to teleport {playerInfo.Player.name}: {playerEx.Message}");
                    }
                }

                if (teleportCount > 0)
                {
                    ShowNotification("Teleport", $"Teleported {teleportCount} players to your position", NotificationType.Success);
                }
                else
                {
                    ShowNotification("Teleport", "No other players to teleport", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in TeleportAllPlayersToMe: {ex.Message}");
                ShowNotification("Error", "Failed to teleport players", NotificationType.Error);
            }
        }
    }
}
