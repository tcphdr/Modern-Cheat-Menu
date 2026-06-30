namespace Modern_Cheat_Menu
{
    public partial class Core
    {
        private void InitializePredefinedTeleports()
        {
            _predefinedTeleports.Clear();

            try
            {
                var mapInstance = Map.Instance;
                if (mapInstance == null) return;

                // Add special locations
                if (mapInstance.PoliceStation != null)
                {
                    _predefinedTeleports["Police Station"] = mapInstance.PoliceStation.transform.position;
                }

                if (mapInstance.MedicalCentre != null)
                {
                    _predefinedTeleports["Medical Centre"] = mapInstance.MedicalCentre.transform.position;
                }

                // Add region-based teleports
                var regions = mapInstance.Regions;
                if (regions != null)
                {
                    foreach (var region in regions)
                    {
                        if (region != null && !string.IsNullOrEmpty(region.Name))
                        {
                            // For regions with delivery locations, add those as teleport points
                            var deliveryLocations = region.RegionDeliveryLocations;
                            if (deliveryLocations != null && deliveryLocations.Length > 0)
                            {
                                int count = 0;
                                foreach (var location in deliveryLocations)
                                {
                                    if (location != null && location.transform != null)
                                    {
                                        count++;
                                        string locationName = !string.IsNullOrEmpty(location.name) ?
                                        location.name : $"Location {count}";

                                        _predefinedTeleports[$"{region.Name} - {locationName}"] = location.transform.position;

                                        // Limit to 5 locations per region to avoid cluttering the list
                                        if (count >= 5) break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Add other players' positions
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList != null && playerList.Count > 0)
                {
                    foreach (var player in playerList)
                    {
                        if (player != null && !IsLocalPlayer(player))
                        {
                            _predefinedTeleports[$"Player: {player.name}"] = player.transform.position;
                        }
                    }
                }

                LoggerInstance.Msg($"Initialized {_predefinedTeleports.Count} predefined teleport locations");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing predefined teleports: {ex.Message}");
            }
        }

        private void DrawInteractiveMap(Rect mapRect)
        {
            try
            {
                // First, define the container area
                Rect strictMapContainer = new Rect(
                    mapRect.x + 5,
                    mapRect.y + 5,
                    mapRect.width - 10,
                    mapRect.height - 10
                );

                // Draw background
                GUI.color = new Color(0.1f, 0.1f, 0.12f, 1f);
                GUI.DrawTexture(strictMapContainer, Texture2D.whiteTexture);
                GUI.color = Color.white;

                if (_mapTexture != null)
                {
                    // Calculate aspect ratio
                    float texRatio = (float)_mapTexture.width / _mapTexture.height;
                    float rectRatio = strictMapContainer.width / strictMapContainer.height;

                    Rect baseDisplayRect;
                    if (texRatio > rectRatio)
                    {
                        // Fit width
                        float adjustedHeight = strictMapContainer.width / texRatio;
                        baseDisplayRect = new Rect(
                            strictMapContainer.x,
                            strictMapContainer.y + (strictMapContainer.height - adjustedHeight) / 2,
                                                   strictMapContainer.width,
                                                   adjustedHeight
                        );
                    }
                    else
                    {
                        // Fit height
                        float adjustedWidth = strictMapContainer.height * texRatio;
                        baseDisplayRect = new Rect(
                            strictMapContainer.x + (strictMapContainer.width - adjustedWidth) / 2,
                                                   strictMapContainer.y,
                                                   adjustedWidth,
                                                   strictMapContainer.height
                        );
                    }

                    // Apply zoom and pan
                    Rect displayRect = ApplyZoomAndPan(baseDisplayRect, strictMapContainer);

                    // Begin clip area to prevent drawing outside container
                    GUI.BeginClip(strictMapContainer);

                    // Adjust coordinates for clipping
                    Rect adjustedDisplayRect = new Rect(
                        displayRect.x - strictMapContainer.x,
                        displayRect.y - strictMapContainer.y,
                        displayRect.width,
                        displayRect.height
                    );

                    // Draw the map texture
                    GUI.DrawTexture(adjustedDisplayRect, _mapTexture, ScaleMode.StretchToFill);

                    // Draw regions and other map elements - adjusted for clipping
                    DrawMapElementsClipped(adjustedDisplayRect);

                    // End clip area
                    GUI.EndClip();

                    // Draw zoom controls (outside the clipped area)
                    DrawZoomControls(mapRect);

                    // Handle mouse interactions (outside the clipped area)
                    HandleMapInteraction(strictMapContainer, displayRect, baseDisplayRect);
                }
                else
                {
                    // No map texture available yet
                    GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
                    msgStyle.alignment = TextAnchor.MiddleCenter;
                    msgStyle.fontSize = 16;
                    GUI.Label(strictMapContainer, _isCapturingMap ? "Loading map data...." : "Map not available", msgStyle);

                    if (!_isCapturingMap && GUI.Button(
                        new Rect(strictMapContainer.x + strictMapContainer.width / 2 - 60,
                                 strictMapContainer.y + strictMapContainer.height / 2 + 30,
                                 120, 30),
                                 "Capture Map", _buttonStyle))
                    {
                        MelonCoroutines.Start(CaptureMapCoroutine());
                        _isCapturingMap = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawInteractiveMap: {ex.Message}");
            }
        }

        private void DrawMapElementsClipped(Rect displayRect)
        {
            // Draw region overlays
            foreach (var regionRect in _regionRects)
            {
                // Scale region rect to display
                Rect scaledRect = new Rect(
                    displayRect.x + regionRect.Value.x * displayRect.width,
                    displayRect.y + regionRect.Value.y * displayRect.height,
                    regionRect.Value.width * displayRect.width,
                    regionRect.Value.height * displayRect.height
                );

                // Determine color and opacity
                Color regionColor = _regionColors.ContainsKey(regionRect.Key) ?
                _regionColors[regionRect.Key] :
                new Color(0.5f, 0.5f, 0.5f, 0.7f);

                float opacity = (regionRect.Key == _hoveredRegion) ? 0.7f : 0.4f;
                GUI.color = new Color(regionColor.r, regionColor.g, regionColor.b, opacity);

                // Draw region
                GUI.Box(scaledRect, "", _panelStyle ?? GUI.skin.box);
                GUI.color = Color.white;

                // Draw region name
                string regionName = "";
                var mapInstance = Map.Instance;
                if (mapInstance != null)
                {
                    var regionData = mapInstance.GetRegionData(regionRect.Key);
                    if (regionData != null)
                    {
                        regionName = regionData.Name;
                    }
                }

                if (!string.IsNullOrEmpty(regionName))
                {
                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.alignment = TextAnchor.MiddleCenter;
                    nameStyle.fontStyle = FontStyle.Bold;
                    nameStyle.normal.textColor = Color.white;
                    nameStyle.fontSize = Mathf.Max(10, Mathf.FloorToInt(14 * _mapZoom));

                    GUI.Label(scaledRect, regionName, nameStyle);
                }
            }

            // Draw marker for last clicked position
            if (_lastClickPosition != Vector2.zero)
            {
                // Convert normalized position to display coordinates
                Vector2 markerPos = new Vector2(
                    displayRect.x + _lastClickPosition.x * displayRect.width,
                    displayRect.y + _lastClickPosition.y * displayRect.height
                );

                // Size of the marker
                //float markerSize = 8f * _mapZoom;
                //Rect markerRect = new Rect(
                //    markerPos.x - markerSize / 2,
                //    markerPos.y - markerSize / 2,
                //    markerSize,
                //    markerSize
                //);

                //// Draw a red dot
                //GUI.color = Color.red;
                //GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
                //GUI.color = Color.white;
            }
        }

        private void DrawZoomControls(Rect mapRect)
        {
            // Zoom level indicator
            GUIStyle zoomStyle = new GUIStyle(GUI.skin.label);
            zoomStyle.normal.textColor = Color.white;
            zoomStyle.alignment = TextAnchor.MiddleCenter;
            zoomStyle.fontSize = 12;

            Rect zoomLabelRect = new Rect(
                mapRect.x + 10,
                mapRect.y + 10,
                100,
                20
            );

            GUI.Label(zoomLabelRect, $"Zoom: {_mapZoom:F1}x", zoomStyle);

            // Zoom buttons
            Rect zoomInRect = new Rect(
                mapRect.x + mapRect.width - 70,
                mapRect.y + 10,
                30,
                20
            );

            Rect zoomOutRect = new Rect(
                mapRect.x + mapRect.width - 35,
                mapRect.y + 10,
                30,
                20
            );

            if (GUI.Button(zoomInRect, "+", _buttonStyle ?? GUI.skin.button))
            {
                _mapZoom = Mathf.Clamp(_mapZoom + 0.1f, 1.0f, _maxZoom);
            }

            // Disable zoom out button if already at default zoom
            GUI.enabled = _mapZoom > 1.0f;
            if (GUI.Button(zoomOutRect, "-", _buttonStyle ?? GUI.skin.button))
            {
                _mapZoom = Mathf.Clamp(_mapZoom - 0.1f, 1.0f, _maxZoom);
            }
            GUI.enabled = true;

            // Reset zoom/pan button
            Rect resetRect = new Rect(
                mapRect.x + mapRect.width - 90,
                mapRect.y + 35,
                80,
                20
            );

            if (GUI.Button(resetRect, "Reset View", _buttonStyle ?? GUI.skin.button))
            {
                _mapZoom = 1.0f;
                _mapPanOffset = Vector2.zero;
            }

            // Help text for controls
            GUIStyle helpStyle = new GUIStyle(GUI.skin.label);
            helpStyle.fontSize = 10;
            helpStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

            Rect helpRect = new Rect(
                mapRect.x + 10,
                mapRect.y + 35,
                200,
                20
            );

            GUI.Label(helpRect, "Middle-click & drag to pan. Scroll to zoom.", helpStyle);
        }

        private void HandleMapInteraction(Rect mapContainer, Rect displayRect, Rect baseRect)
        {
            // Check if mouse is over the map
            if (mapContainer.Contains(Event.current.mousePosition))
            {
                // Calculate relative mouse position
                Vector2 normalizedPos = new Vector2(
                    Mathf.Clamp01((Event.current.mousePosition.x - displayRect.x) / displayRect.width),
                                                    Mathf.Clamp01((Event.current.mousePosition.y - displayRect.y) / displayRect.height)
                );

                // Determine hovered region
                _hoveredRegion = GetRegionAtPosition(normalizedPos);

                // Handle middle-click drag (panning)
                if (Event.current.type == EventType.MouseDown && Event.current.button == 2)
                {
                    _isDraggingMap = true;
                    _dragStartPos = Event.current.mousePosition;
                    _dragStartOffset = _mapPanOffset;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && Event.current.button == 2)
                {
                    _isDraggingMap = false;
                    Event.current.Use();
                }

                // Handle drag movement
                if (_isDraggingMap && Event.current.type == EventType.MouseDrag)
                {
                    Vector2 delta = Event.current.mousePosition - _dragStartPos;
                    _mapPanOffset = _dragStartOffset + delta / _mapZoom;
                    Event.current.Use();
                }

                // Handle scroll wheel zooming
                if (Event.current.type == EventType.ScrollWheel)
                {
                    // Only allow zooming in if already at or below default zoom
                    if (Event.current.delta.y > 0 && _mapZoom <= 1.0f)
                    {
                        // Don't allow zooming out more
                    }
                    else
                    {
                        float zoomDelta = -Event.current.delta.y * 0.05f;
                        float newZoom = Mathf.Clamp(_mapZoom + zoomDelta, 1.0f, _maxZoom);

                        // Adjust pan to zoom toward mouse position
                        Vector2 mousePos = Event.current.mousePosition;
                        Vector2 mapCenter = new Vector2(
                            baseRect.x + baseRect.width / 2,
                            baseRect.y + baseRect.height / 2
                        );

                        _mapZoom = newZoom;
                    }

                    Event.current.Use();
                }

                // Handle clicking for teleportation
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    _lastClickPosition = normalizedPos;

                    // Use a more direct world coordinate mapping method
                    Vector3 worldPos = MapToWorldPosition(normalizedPos, mapContainer, displayRect);
                    TeleportPlayer(worldPos);

                    Event.current.Use();
                }
            }
            else
            {
                _hoveredRegion = (EMapRegion)(-1);

                // Cancel dragging if mouse leaves map
                if (_isDraggingMap && Event.current.type == EventType.MouseUp)
                {
                    _isDraggingMap = false;
                }
            }
        }

        private Vector3 MapToWorldPosition(Vector2 normalizedPos, Rect mapContainer, Rect displayRect)
        {
            try
            {
                // Get current player position for height reference
                var localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    LoggerInstance.Error("Could not find local player!");
                    return Vector3.zero;
                }

                /*
                 * I think I figured out the game's map coordinates relative to world position
                 * After spending countless hours analyzing game functions and the various data containers associated with them.
                 * I've managed to discern that the map dimensions are 2048x2048, with a scale factor of 5.006356f, the formula is as follows:
                 * Take X position and subtract it by 0.5f, times it by the X map dimension and then divide it by the scale factor.
                 * The same applies to the Z coordinate, awfully strange how the game developer chose to use the Z axis for lateral movement instead of the Y position.
                 * I guess this approach is best for a 3D space, I wouldn't know.
                 * Example: (positonX - 0.5f) * 2048 / 5.006356, (0.5f - positionZ) * 2048 / 5.006356.
                 * This seems to produce the best results, but it's still not quite accurate, but this could be due to the scaling factor of the UI relative to the mouse click input.
                 * Who knows, who cares, this is good enough for me, if you have a better method, please share it with me.
                 */
                Vector2 _mapDimensions = new Vector2(2048f, 2048f);
                float _scaleFactor = 5.006356f;

                float worldX = (normalizedPos.x - 0.5f) * _mapDimensions.x / _scaleFactor;
                float worldZ = (0.5f - normalizedPos.y) * _mapDimensions.y / _scaleFactor;

                Vector3 raycastStart = new Vector3(worldX, 100f, worldZ);
                float height = GetGroundHeight(raycastStart);

                return new Vector3(worldX, height, worldZ);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error converting map coordinates: {ex.Message}");
                return Vector3.zero;
            }
        }

        private Rect ApplyZoomAndPan(Rect baseRect, Rect containerRect)
        {
            // Calculate the zoom-adjusted size
            float zoomedWidth = baseRect.width * _mapZoom;
            float zoomedHeight = baseRect.height * _mapZoom;

            // Calculate center position
            float centerX = baseRect.x + baseRect.width / 2;
            float centerY = baseRect.y + baseRect.height / 2;

            // Apply pan offset with improved constraints
            float maxPanX = Math.Max(0, (zoomedWidth - containerRect.width) / 2);
            float maxPanY = Math.Max(0, (zoomedHeight - containerRect.height) / 2);

            // Clamp pan offset to prevent going outside container bounds
            _mapPanOffset.x = Mathf.Clamp(_mapPanOffset.x, -maxPanX, maxPanX);
            _mapPanOffset.y = Mathf.Clamp(_mapPanOffset.y, -maxPanY, maxPanY);

            centerX += _mapPanOffset.x;
            centerY += _mapPanOffset.y;

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

        private float GetGroundHeight(Vector3 xzPosition)
        {
            // Start raycast from high above to ensure we're above any terrain
            float raycastHeight = 1000f;
            Vector3 rayStart = new Vector3(xzPosition.x, raycastHeight, xzPosition.z);
            RaycastHit hit;

            // Use a proper layer mask for ground detection
            // Adjust these layer names based on your actual project's layer setup
            int groundLayer = LayerMask.GetMask("Ground", "Terrain", "Default");

            // Perform the raycast
            if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, groundLayer))
            {
                // Add a small offset to prevent players from sinking into the ground
                float heightOffset = 2.0f;
                return hit.point.y + heightOffset;
            }

            if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity))
            {
                // Add offset for non-terrain objects too
                return hit.point.y + 2.0f;
            }

            LoggerInstance.Error($"No ground detected at position {xzPosition}, using fallback height");
            return 3f; // Or some other safe default height for your game world
        }

        private IEnumerator CaptureMapCoroutine()
        {
            yield return new WaitForSeconds(0.5f);

            try
            {
                // Get the map instance
                var mapInstance = Map.Instance;
                if (mapInstance == null)
                {
                    LoggerInstance.Error("Map instance not found!");
                    _isCapturingMap = false;
                    yield break;
                }

                // Try to get map texture from the phone's MapApp
                Texture2D mapTexture = null;

                // Find MapApp instance
                var mapApp = Resources.FindObjectsOfTypeAll<Il2CppScheduleOne.UI.Phone.Map.MapApp>()
                .FirstOrDefault();

                if (mapApp != null)
                {
                    // Check if any of the map sprites are available
                    if (mapApp.MainMapSprite != null && mapApp.MainMapSprite.texture != null)
                    {

                        mapTexture = CreateReadableTexture(mapApp.MainMapSprite.texture);
                    }
                }
                // If we found a map texture, use it
                if (mapTexture != null)
                {
                    _mapTexture = mapTexture;
                }

                _mapTexture.Apply();
                InitializePredefinedTeleports();
                _mapInitialized = true;

            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error capturing map: {ex.Message}");
                LoggerInstance.Error($"Stack trace: {ex.StackTrace}");
            }

            _isCapturingMap = false;
        }
    }
}
