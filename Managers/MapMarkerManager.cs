using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using OmenTools;
using EliteHunt.Data;
using EliteHunt.Utils;

namespace EliteHunt.Managers
{
    public class MapMarker
    {
        public uint TerritoryType { get; set; }
        public uint MapId { get; set; }
        public uint MobHuntId { get; set; }
        public string Name { get; set; } = "";
        public Vector2 Coordinate { get; set; }
        public bool IsEliteMark { get; set; }
        public bool ShowArea { get; set; }
    }

    public unsafe class MapMarkerManager
    {
        private readonly List<MapMarker> _markers = new();
        private MapMarker? _highlightedMarker;

        public event Action<MapMarker>? OnMarkerClicked;
        public event Action<MapMarker>? OnMarkerHovered;

        public void Initialize()
        {
        }

        public void CreateMarker(MobHuntEntry entry, bool showArea = false)
        {
            var marker = new MapMarker
            {
                TerritoryType = entry.TerritoryType,
                MapId = entry.MapId,
                MobHuntId = entry.MobHuntId,
                Name = entry.Name ?? "",
                IsEliteMark = entry.IsEliteMark,
                ShowArea = showArea
            };

            marker.Coordinate = GetMarkerCoordinate(entry);

            _markers.Add(marker);

            DService.Instance().Log.Information($"创建地图标记: {marker.Name} 在 ({marker.Coordinate.X:F1}, {marker.Coordinate.Y:F1})");

            if (showArea)
            {
                OpenMapWithMarker(marker);
            }
        }

        private Vector2 GetMarkerCoordinate(MobHuntEntry entry)
        {
            return Vector2.Zero;
        }

        private void OpenMapWithMarker(MapMarker marker)
        {
            try
            {
                DService.Instance().Log.Information($"在地图上打开标记: {marker.Name}");
            }
            catch (Exception ex)
            {
                DService.Instance().Log.Warning($"打开地图失败: {ex.Message}");
            }
        }

        public void ClearMarkers()
        {
            _markers.Clear();
            _highlightedMarker = null;
        }

        public void RemoveMarker(uint mobHuntId)
        {
            _markers.RemoveAll(m => m.MobHuntId == mobHuntId);
        }

        public List<MapMarker> GetMarkers()
        {
            return _markers.ToList();
        }

        public List<MapMarker> GetMarkersForCurrentTerritory()
        {
            var clientState = DService.Instance().ClientState;
            if (clientState == null) return new List<MapMarker>();

            uint currentTerritory = clientState.TerritoryType;
            return _markers.Where(m => m.TerritoryType == currentTerritory).ToList();
        }

        public void HighlightMarker(MapMarker marker)
        {
            _highlightedMarker = marker;
        }

        public MapMarker? GetHighlightedMarker()
        {
            return _highlightedMarker;
        }

        public void OnMapMarkerClicked(MapMarker marker)
        {
            OnMarkerClicked?.Invoke(marker);
        }

        public void OnMapMarkerHovered(MapMarker marker)
        {
            _highlightedMarker = marker;
            OnMarkerHovered?.Invoke(marker);
        }

        public void CreateCoordinateMarker(uint territoryType, uint mapId, string name, float x, float y, bool openMap = true)
        {
            DService.Instance().Log.Information($"[地图标记] CreateCoordinateMarker 开始: {name}, TerritoryType={territoryType}, MapId={mapId}, X={x:F1}, Y={y:F1}");
            
            try
            {
                var marker = new MapMarker
                {
                    TerritoryType = territoryType,
                    MapId = mapId,
                    Name = name,
                    Coordinate = new Vector2(x, y),
                    IsEliteMark = false,
                    ShowArea = true
                };

                _markers.Add(marker);

                DService.Instance().Log.Information($"创建坐标标记: {name} 在 ({x:F1}, {y:F1})");

                if (openMap)
                {
                    OpenGameMap(territoryType, mapId, x, y);
                }
            }
            catch (Exception ex)
            {
                DService.Instance().Log.Warning($"创建坐标标记失败: {ex.Message}");
            }
        }

        private void OpenGameMap(uint territoryType, uint mapId, float x, float y)
        {
            try
            {
                // 直接使用传入的怪物所在区域，而不是玩家当前区域
                MapUtils.SetMapFlag(mapId, territoryType, x, y);
                
                DService.Instance().Log.Information(
                    $"[地图标记] ✅ 已成功在地图上标记位置！\n" +
                    $"  怪物区域: {territoryType}\n" +
                    $"  目标地图ID: {mapId}\n" +
                    $"  坐标: ({x:F1}, {y:F1})");
            }
            catch (Exception ex)
            {
                DService.Instance().Log.Error($"[地图标记] 打开地图失败: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            ClearMarkers();
        }
    }
}
