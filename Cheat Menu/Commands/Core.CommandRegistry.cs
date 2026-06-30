namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private void RegisterCommands()
        {
            _categories.Clear();

            // Initialize dropdown option lists
            _itemCache["explosion_targets"] = new List<string> {
                "custom",
                "all",
                "random",
                "nukeall"
            };

            _itemCache["vehicle_targets"] = new List<string> {
                "shitbox",
                "veeper",
                "bruiser",
                "dinkler",
                "hounddog",
                "cheetah",
                "hotbox"
            };

            _itemCache["predefined_tele_targets"] = new List<string>
            {
                "motel",
                "sweatshop",
                "barn",
                "bungalow",
                "warehouse",
                "docks",
                "manor",
                "postoffice",
                "dealership",
                "tacoticklers",
                "laundromat",
                "carwash",
                "pawnshop",
                "hardwarestore"
            };

            _itemCache["weather"] = new List<string>
            {
                "clear",
                "heavyrain",
                "lightrain",
                "overcast"
            };

            LoggerInstance.Msg($"Added {_itemCache["explosion_targets"].Count} explosion targets to item cache.");
            LoggerInstance.Msg($"Added {_itemCache["vehicle_targets"].Count} vehicles to item cache.");
            LoggerInstance.Msg($"Added {_itemCache["predefined_tele_targets"].Count} predefined teleport locations.");

            var onlineCategory = new CommandCategory { Name = "Online" };
            var playerCategory = new CommandCategory { Name = "Self" };
            var exploitsCategory = new CommandCategory { Name = "Exploits" };
            var itemsCategory = new CommandCategory { Name = "Item Manager" };
            var worldCategory = new CommandCategory { Name = "World" };
            var teleportCategory = new CommandCategory { Name = "Teleport Manager" };
            var vehicleCategory = new CommandCategory { Name = "Vehicle Manager" };
            var systemCategory = new CommandCategory { Name = "Game" };


            /* Player category */
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Godmode",
                Description = "Toggles godmode on/off.",
                Handler = ToggleGodmode
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Unlimited Ammo",
                Description = "Toggles unlimited ammo on/off.",
                Handler = ToggleUnlimitedAmmo
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Never Wanted",
                Description = "Toggles never wanted on/off.",
                Handler = ToggleNeverWanted
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Give XP",
                Description = "Gives player XP.",
                Handler = ChangeXP,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "25"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Give Cash",
                Description = "Sends the quantity of cash to the player's cash balance, can take negative numbers.",
                Handler = ChangeCash,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "1000"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Give Online Balance",
                Description = "Sends the quantity of cash to the player's online balance, can take negative numbers.",
                Handler = ChangeBalance,
                Parameters = new List<CommandParameter>
                {
                    new CommandParameter
                    {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "1000"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Always Visible Crosshair",
                Description = "Forces the crosshair to always remain visible, even when using items that would normally hide it.",
                Handler = ToggleAlwaysVisibleCrosshair
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Raise Wanted Level",
                Description = "Raises your wanted level.",
                Handler = RaiseWantedLevel
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Lower Wanted Level",
                Description = "Lowers your wanted level.",
                Handler = LowerWantedLevel
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Clear Wanted Level",
                Description = "Clears your wanted level.",
                Handler = ClearWantedLevel
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Set Movement Speed",
                Description = "Sets the player's movement speed.",
                Handler = SetPlayerMovementSpeed,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Speed",
                        Placeholder = "Speed",
                        Type = ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Set Jump Force",
                Description = "Sets the player's jump force.",
                Handler = SetJumpForce,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Force",
                        Placeholder = "Force",
                        Type = ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Set Stamina Reserve",
                Description = "Sets the player's stamina reserve.",
                Handler = SetPlayerStaminaReserve,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "200"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Clear Inventory",
                Description = "Clears the player's inventory",
                Handler = ClearInventory
            });


            /* World category */
            worldCategory.Commands.Add(new Command
            {
                Name = "Free Camera",
                Description = "Toggles free camera mode",
                Handler = ToggleFreeCam
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Set Weather",
                Description = "Changes weather using the cached weather list.",
                Handler = SetWeather,
                Parameters = new List<CommandParameter> {
                    new CommandParameter
                    {
                        Name = "Weather",
                        Placeholder = "Select weather type",
                        Type = ParameterType.Dropdown,
                        ItemCacheKey = "weather",
                        Value = "clear"
                    }
                }
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Set Time",
                Description = "Sets the time of day (24-hour format)",
                Handler = SetWorldTime,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Time",
                        Placeholder = "HHMM (e.g. 1530)",
                        Type = ParameterType.Input,
                        Value = "1200"
                    }
                }
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Set Time Scale",
                Description = "Sets game time scale (1.0 = normal)",
                Handler = SetTimeScale,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Scale",
                        Placeholder = "Scale",
                        Type = ParameterType.Input,
                        Value = "1.0"
                    }
                }
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Grow Plants",
                Description = "Instantly grows all weed plants in the world.",
                Handler = GrowPlants
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Clear World Trash",
                Description = "Forcefully clears all world trash.",
                Handler = ClearTrash
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Law Intensity",
                Description = "Sets the law intensity (maximum 10)",
                Handler = SetLawIntensity,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Intensity",
                        Placeholder = "6",
                        Type = ParameterType.Input,
                        Value = "6"
                    }
                }
            });

            /* System category */
            systemCategory.Commands.Add(new Command
            {
                Name = "Save Game",
                Description = "Forces a game save",
                Handler = ForceGameSave
            });
            systemCategory.Commands.Add(new Command
            {
                Name = "End Tutorial",
                Description = "Forcefully ends the tutorial.",
                Handler = EndTutorial
            });


            /* Exploits Category */
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Create Explosion",
                Description = "Create explosions. Options: 'all' (target all players), 'random' (target random player), or custom location.",
                Handler = CreateExplosion,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Target",
                        Placeholder = "all/random",
                        Type = ParameterType.Dropdown,
                        ItemCacheKey = "explosion_targets",
                        Value = "custom"
                    },
                    new CommandParameter {
                        Name = "Damage",
                        Placeholder = "Damage",
                        Type = ParameterType.Input,
                        Value = "100"
                    },
                    new CommandParameter {
                        Name = "Radius",
                        Placeholder = "Radius",
                        Type = ParameterType.Input,
                        Value = "10"
                    }
                }
            });
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Kill Player",
                Description = "Kills the specified player by index.",
                Handler = KillPlayerCommand,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "PlayerIndex",
                        Placeholder = "Player Index",
                        Type = ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Damage Player",
                Description = "Damages the specified player by index.",
                Handler = DamagePlayerCommand,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "PlayerIndex",
                        Placeholder = "Player Index",
                        Type = ParameterType.Input,
                        Value = "1"
                    },
                    new CommandParameter {
                        Name = "Damage",
                        Placeholder = "Damage Amount",
                        Type = ParameterType.Input,
                        Value = "10"
                    }
                }
            });
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Kill All Players",
                Description = "Kills all players except yourself.",
                Handler = KillAllPlayersCommand
            });

            vehicleCategory.Commands.Add(new Command
            {
                Name = "Spawn Vehicle",
                Description = "Spawns a vehicle of your choosing.",
                Handler = SpawnVehicle,
                Parameters = new List<CommandParameter> {
                    new CommandParameter
                    {
                        Name = "Vehicle",
                        Placeholder = "Select vehicle",
                        Type = ParameterType.Dropdown,
                        ItemCacheKey = "vehicle_targets",
                        Value = "Cheetah"
                    }
                }
            });

            // Add categories to list
            _categories.Add(onlineCategory);
            _categories.Add(playerCategory);
            _categories.Add(exploitsCategory);
            _categories.Add(itemsCategory);
            _categories.Add(worldCategory);
            _categories.Add(teleportCategory);
            _categories.Add(vehicleCategory);
            _categories.Add(systemCategory);
        }

        private void ExecuteCommand(Command command)
        {
            try
            {
                if (command.Handler != null)
                {
                    string[] args = command.Parameters
                    .Select(p => p.Value?.Trim() ?? "")
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToArray();

                    command.Handler.Invoke(args);
                    ShowNotification("Command Executed", $"{command.Name} completed", NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Command failed: {ex}");
                ShowNotification("Command Error", ex.Message, NotificationType.Error);
            }
        }
    }
}
