/*
 * Modern Cheat Menu
 * Core.PlayerMap.cs
 */

namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private void InitializePlayerMap()
        {
            try
            {
                // Try to use existing map texture if available
                if (_mapTexture != null)
                {
                    _playerMapTexture = _mapTexture;
                    _playerMapInitialized = true;
                    return;
                }

                // Otherwise try to capture from the game
                var mapApp = Resources.FindObjectsOfTypeAll<Il2CppScheduleOne.UI.Phone.Map.MapApp>()
                .FirstOrDefault();

                if (mapApp != null && mapApp.MainMapSprite != null && mapApp.MainMapSprite.texture != null)
                {
                    _playerMapTexture = CreateReadableTexture(mapApp.MainMapSprite.texture);
                    _playerMapInitialized = true;
                }
                else
                {
                    // Create fallback texture if map cannot be obtained
                    _playerMapTexture = new Texture2D(1, 1);
                    _playerMapTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.3f));
                    _playerMapTexture.Apply();
                    _playerMapInitialized = true;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing player map: {ex.Message}");
            }
        }

        private void RefreshPlayerMapPositions()
        {
            // Just refresh player list
            RefreshOnlinePlayers();

            // Ensure map is initialized
            if (!_playerMapInitialized)
            {
                InitializePlayerMap();
            }

            // Assign unique colors to new players
            foreach (var player in _onlinePlayers)
            {
                if (player != null && !_playerColors.ContainsKey(player.SteamID))
                {
                    _playerColors[player.SteamID] = GetUniqueColor(player.SteamID);
                }
            }
        }

        private void DrawPlayerPositionsMap(Rect mapRect)
        {
            try
            {
                // Inner map container with padding
                Rect innerMapRect = new Rect(
                    mapRect.x + 5,
                    mapRect.y + 5,
                    mapRect.width - 10,
                    mapRect.height - 10
                );

                // Draw background
                GUI.color = new Color(0.1f, 0.1f, 0.15f, 1f);
                GUI.DrawTexture(innerMapRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Initialize map if needed
                if (_playerMapTexture == null || !_playerMapInitialized)
                {
                    InitializePlayerMap();
                }

                if (_playerMapTexture != null)
                {
                    // Calculate display rectangle with proper aspect ratio
                    float texRatio = (float)_playerMapTexture.width / _playerMapTexture.height;
                    float rectRatio = innerMapRect.width / innerMapRect.height;

                    Rect baseDisplayRect;
                    if (texRatio > rectRatio)
                    {
                        // Fit to width
                        float height = innerMapRect.width / texRatio;
                        baseDisplayRect = new Rect(
                            innerMapRect.x,
                            innerMapRect.y + (innerMapRect.height - height) / 2,
                                                   innerMapRect.width,
                                                   height
                        );
                    }
                    else
                    {
                        // Fit to height
                        float width = innerMapRect.height * texRatio;
                        baseDisplayRect = new Rect(
                            innerMapRect.x + (innerMapRect.width - width) / 2,
                                                   innerMapRect.y,
                                                   width,
                                                   innerMapRect.height
                        );
                    }

                    // Apply zoom and pan using your existing method
                    Rect displayRect = ApplyPlayerMapZoomAndPan(baseDisplayRect, innerMapRect);

                    // Create a strict clipping area to prevent drawing outside the map container
                    GUI.BeginClip(innerMapRect);

                    // Adjust coordinates for clipping
                    Rect adjustedDisplayRect = new Rect(
                        displayRect.x - innerMapRect.x,
                        displayRect.y - innerMapRect.y,
                        displayRect.width,
                        displayRect.height
                    );

                    // Draw map texture
                    GUI.DrawTexture(adjustedDisplayRect, _playerMapTexture, ScaleMode.StretchToFill);

                    // Draw player markers - adjusted for clipping
                    foreach (var playerInfo in _onlinePlayers)
                    {
                        if (playerInfo == null || playerInfo.Player == null)
                            continue;

                        // Get player position and convert to map position
                        Vector3 worldPos = playerInfo.Player.transform.position;
                        Vector2 normalizedPos = WorldToMapPosition(worldPos);

                        // Convert normalized position to display coordinates
                        Vector2 markerPos = new Vector2(
                            adjustedDisplayRect.x + normalizedPos.x * adjustedDisplayRect.width,
                            adjustedDisplayRect.y + normalizedPos.y * adjustedDisplayRect.height
                        );

                        // Draw marker
                        DrawPlayerMapMarker(markerPos, playerInfo.IsLocal ? Color.white : Color.red, playerInfo.IsLocal, playerInfo.Name);
                    }

                    // End clip area
                    GUI.EndClip();

                    // *** FIXED: Draw legend and controls outside clipped area with proper visibility ***
                    DrawPlayerMapLegend(innerMapRect);
                    DrawPlayerMapZoomControls(innerMapRect);

                    // Handle map interaction
                    HandlePlayerMapInteraction(innerMapRect, displayRect, baseDisplayRect);
                }
                else
                {
                    // No map texture available
                    GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
                    msgStyle.alignment = TextAnchor.MiddleCenter;
                    msgStyle.fontSize = 16;
                    msgStyle.normal.textColor = Color.white;

                    GUI.Label(innerMapRect, "Map not available", msgStyle);

                    if (GUI.Button(
                        new Rect(innerMapRect.x + innerMapRect.width / 2 - 60,
                                 innerMapRect.y + innerMapRect.height / 2 + 30,
                                 120, 30),
                                 "Initialize Map", _buttonStyle))
                    {
                        InitializePlayerMap();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing player map: {ex.Message}");
            }
        }

        private void DrawPlayerMapLegend(Rect mapRect)
        {
            try
            {
                // Create a black background panel for the legend
                Rect legendRect = new Rect(
                    mapRect.x + 10,
                    mapRect.y + 10,
                    130,
                    70
                );

                // Solid black background
                GUI.color = new Color(0, 0, 0, 1f);
                GUI.DrawTexture(legendRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Add "Legend" header
                GUIStyle headerStyle = new GUIStyle();
                headerStyle.normal.textColor = Color.white;
                headerStyle.alignment = TextAnchor.MiddleCenter;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.fontSize = 14;

                GUI.Label(
                    new Rect(legendRect.x, legendRect.y + 5, legendRect.width, 20),
                          "Legend",
                          headerStyle
                );

                // Label style
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                labelStyle.fontSize = 12;

                // You marker
                Rect youLabelRect = new Rect(legendRect.x + 40, legendRect.y + 30, 60, 20);
                GUI.Label(youLabelRect, "You", labelStyle);

                Rect youMarkerRect = new Rect(legendRect.x + 15, legendRect.y + 30, 15, 15);
                GUI.color = Color.white;
                GUI.DrawTexture(youMarkerRect, Texture2D.whiteTexture);

                // Others marker
                Rect othersLabelRect = new Rect(legendRect.x + 40, legendRect.y + 50, 60, 20);
                GUI.Label(othersLabelRect, "Others", labelStyle);

                Rect othersMarkerRect = new Rect(legendRect.x + 15, legendRect.y + 50, 15, 15);
                GUI.color = Color.red;
                GUI.DrawTexture(othersMarkerRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing map legend: {ex.Message}");
            }
        }

        private void DrawPlayerMapZoomControls(Rect mapRect)
        {
            try
            {
                // Controls container - solid black background
                Rect controlsRect = new Rect(
                    mapRect.x + mapRect.width - 110,
                    mapRect.y + 10,
                    100,
                    90
                );

                // Solid black background
                GUI.color = new Color(0, 0, 0, 1f);
                GUI.DrawTexture(controlsRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Zoom indicator
                GUIStyle zoomStyle = new GUIStyle();
                zoomStyle.normal.textColor = Color.white;
                zoomStyle.alignment = TextAnchor.MiddleCenter;
                zoomStyle.fontSize = 16;
                zoomStyle.fontStyle = FontStyle.Bold;

                GUI.Label(
                    new Rect(controlsRect.x, controlsRect.y + 5, controlsRect.width, 25),
                          $"Zoom: {_playerMapZoom:F1}x",
                          zoomStyle
                );

                // Reset button
                if (GUI.Button(
                    new Rect(controlsRect.x + 10, controlsRect.y + 55, 80, 25),
                               "Reset",
                               _buttonStyle))
                {
                    _playerMapZoom = 1.0f;
                    _mapPanOffset = Vector2.zero;
                }

                // Zoom buttons
                if (GUI.Button(
                    new Rect(controlsRect.x + 55, controlsRect.y + 30, 35, 25),
                               "+",
                               _buttonStyle))
                {
                    _playerMapZoom = Mathf.Clamp(_playerMapZoom + 0.2f, 1.0f, 3.0f);
                }

                GUI.enabled = _playerMapZoom > 1.0f;
                if (GUI.Button(
                    new Rect(controlsRect.x + 10, controlsRect.y + 30, 35, 25),
                               "-",
                               _buttonStyle))
                {
                    _playerMapZoom = Mathf.Clamp(_playerMapZoom - 0.2f, 1.0f, 3.0f);
                }
                GUI.enabled = true;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing zoom controls: {ex.Message}");
            }
        }

        private void DrawPlayerMapMarker(Vector2 position, Color color, bool isLocal, string playerName)
        {
            try
            {
                // Marker size (larger for local player)
                float markerSize = isLocal ? 10f : 8f;

                // Create the marker rect
                Rect markerRect = new Rect(
                    position.x - markerSize / 2,
                    position.y - markerSize / 2,
                    markerSize,
                    markerSize
                );

                // Draw shadow for better visibility
                GUI.color = new Color(0, 0, 0, 0.5f);
                GUI.DrawTexture(new Rect(markerRect.x + 1, markerRect.y + 1, markerRect.width, markerRect.height),
                                Texture2D.whiteTexture);

                // Draw marker in player color
                GUI.color = color;
                GUI.DrawTexture(markerRect, Texture2D.whiteTexture);

                // For local player, add white border
                if (isLocal)
                {
                    GUI.color = Color.white;
                    // Top
                    GUI.DrawTexture(new Rect(markerRect.x - 1, markerRect.y - 1, markerRect.width + 2, 1), Texture2D.whiteTexture);
                    // Bottom
                    GUI.DrawTexture(new Rect(markerRect.x - 1, markerRect.y + markerRect.height, markerRect.width + 2, 1), Texture2D.whiteTexture);
                    // Left
                    GUI.DrawTexture(new Rect(markerRect.x - 1, markerRect.y - 1, 1, markerRect.height + 2), Texture2D.whiteTexture);
                    // Right
                    GUI.DrawTexture(new Rect(markerRect.x + markerRect.width, markerRect.y - 1, 1, markerRect.height + 2), Texture2D.whiteTexture);
                }

                // Reset color
                GUI.color = Color.white;

                // Show tooltip on hover
                if (markerRect.Contains(Event.current.mousePosition))
                {
                    // Tooltip positioning
                    float tooltipWidth = 120;
                    float tooltipHeight = 25;

                    Rect tooltipRect = new Rect(
                        position.x + markerSize,
                        position.y - tooltipHeight / 2,
                        tooltipWidth,
                        tooltipHeight
                    );

                    // Background
                    GUI.color = new Color(0, 0, 0.15f, 0.9f);
                    GUI.DrawTexture(tooltipRect, Texture2D.whiteTexture);

                    // Border
                    GUI.color = color;
                    // Top
                    GUI.DrawTexture(new Rect(tooltipRect.x, tooltipRect.y, tooltipRect.width, 1), Texture2D.whiteTexture);
                    // Bottom
                    GUI.DrawTexture(new Rect(tooltipRect.x, tooltipRect.y + tooltipRect.height - 1, tooltipRect.width, 1), Texture2D.whiteTexture);
                    // Left
                    GUI.DrawTexture(new Rect(tooltipRect.x, tooltipRect.y, 1, tooltipRect.height), Texture2D.whiteTexture);
                    // Right
                    GUI.DrawTexture(new Rect(tooltipRect.x + tooltipRect.width - 1, tooltipRect.y, 1, tooltipRect.height), Texture2D.whiteTexture);

                    GUI.color = Color.white;

                    // Player name text
                    GUIStyle tooltipStyle = new GUIStyle(_labelStyle);
                    tooltipStyle.normal.textColor = Color.white;
                    tooltipStyle.alignment = TextAnchor.MiddleCenter;
                    tooltipStyle.fontSize = 12;

                    GUI.Label(tooltipRect, playerName, tooltipStyle);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing player marker: {ex.Message}");
            }
        }

        private void HandlePlayerMapInteraction(Rect mapContainer, Rect displayRect, Rect baseRect)
        {
            // Check if mouse is over the map
            if (mapContainer.Contains(Event.current.mousePosition))
            {
                // Calculate relative mouse position
                Vector2 normalizedPos = new Vector2(
                    Mathf.Clamp01((Event.current.mousePosition.x - displayRect.x) / displayRect.width),
                                                    Mathf.Clamp01((Event.current.mousePosition.y - displayRect.y) / displayRect.height)
                );

                // Handle middle-click drag (panning)
                if (Event.current.type == EventType.MouseDown && Event.current.button == 2)
                {
                    _isDraggingPlayerMap = true;
                    _playerMapDragStart = Event.current.mousePosition;
                    _playerMapDragStartOffset = _playerMapPanOffset;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && Event.current.button == 2)
                {
                    _isDraggingPlayerMap = false;
                    Event.current.Use();
                }

                // Handle drag movement
                if (_isDraggingPlayerMap && Event.current.type == EventType.MouseDrag)
                {
                    Vector2 delta = Event.current.mousePosition - _playerMapDragStart;
                    _playerMapPanOffset = _playerMapDragStartOffset + delta / _playerMapZoom;
                    Event.current.Use();
                }

                // Handle scroll wheel zooming
                if (Event.current.type == EventType.ScrollWheel)
                {
                    // Only allow zooming in if already at or below default zoom
                    if (Event.current.delta.y > 0 && _playerMapZoom <= 1.0f)
                    {
                        // Don't allow zooming out more
                    }
                    else
                    {
                        float zoomDelta = -Event.current.delta.y * 0.05f;
                        float newZoom = Mathf.Clamp(_playerMapZoom + zoomDelta, 1.0f, 3.0f);

                        // Adjust pan to zoom toward mouse position
                        Vector2 mousePos = Event.current.mousePosition;
                        Vector2 mapCenter = new Vector2(
                            baseRect.x + baseRect.width / 2,
                            baseRect.y + baseRect.height / 2
                        );

                        _playerMapZoom = newZoom;
                    }

                    Event.current.Use();
                }
            }
            else
            {
                // Cancel dragging if mouse leaves map
                if (_isDraggingPlayerMap && Event.current.type == EventType.MouseUp)
                {
                    _isDraggingPlayerMap = false;
                }
            }
        }

        private Rect ApplyPlayerMapZoomAndPan(Rect baseRect, Rect containerRect)
        {
            // Calculate the zoom-adjusted size
            float zoomedWidth = baseRect.width * _playerMapZoom;
            float zoomedHeight = baseRect.height * _playerMapZoom;

            // Calculate center position
            float centerX = baseRect.x + baseRect.width / 2;
            float centerY = baseRect.y + baseRect.height / 2;

            // Apply pan offset with improved constraints
            float maxPanX = Math.Max(0, (zoomedWidth - containerRect.width) / 2);
            float maxPanY = Math.Max(0, (zoomedHeight - containerRect.height) / 2);

            // Clamp pan offset to prevent going outside container bounds
            _playerMapPanOffset.x = Mathf.Clamp(_playerMapPanOffset.x, -maxPanX, maxPanX);
            _playerMapPanOffset.y = Mathf.Clamp(_playerMapPanOffset.y, -maxPanY, maxPanY);

            centerX += _playerMapPanOffset.x;
            centerY += _playerMapPanOffset.y;

            // Calculate the zoomed rect with the adjusted center
            Rect zoomedRect = new Rect(
                centerX - zoomedWidth / 2,
                centerY - zoomedHeight / 2,
                zoomedWidth,
                zoomedHeight
            );

            // Improved container bounds checking
            if (zoomedWidth <= containerRect.width)
            {
                // Center horizontally if smaller than container
                zoomedRect.x = containerRect.x + (containerRect.width - zoomedWidth) / 2;
            }
            else
            {
                // Constrain to container edges
                if (zoomedRect.x > containerRect.x)
                    zoomedRect.x = containerRect.x;
                if (zoomedRect.x + zoomedWidth < containerRect.x + containerRect.width)
                    zoomedRect.x = containerRect.x + containerRect.width - zoomedWidth;
            }

            if (zoomedHeight <= containerRect.height)
            {
                // Center vertically if smaller than container
                zoomedRect.y = containerRect.y + (containerRect.height - zoomedHeight) / 2;
            }
            else
            {
                // Constrain to container edges
                if (zoomedRect.y > containerRect.y)
                    zoomedRect.y = containerRect.y;
                if (zoomedRect.y + zoomedHeight < containerRect.y + containerRect.height)
                    zoomedRect.y = containerRect.y + containerRect.height - zoomedHeight;
            }

            return zoomedRect;
        }

        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            try
            {
                // Using the same conversion method from your teleport code
                Vector2 _mapDimensions = new Vector2(2048f, 2048f);
                float _scaleFactor = 5.006356f;

                // Calculate normalized map coordinates (0-1 range)
                float normalizedX = (worldPos.x * _scaleFactor / _mapDimensions.x) + 0.5f;
                float normalizedZ = 0.5f - (worldPos.z * _scaleFactor / _mapDimensions.y);

                // Clamp to valid range
                normalizedX = Mathf.Clamp01(normalizedX);
                normalizedZ = Mathf.Clamp01(normalizedZ);

                return new Vector2(normalizedX, normalizedZ);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error converting world to map position: {ex.Message}");
                return new Vector2(0.5f, 0.5f); // Default to center of map
            }
        }
    }
}
