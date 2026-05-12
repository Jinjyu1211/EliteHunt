using System;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using OmenTools;
using System.Numerics;

namespace EliteHunt.Utils;

public static class MapUtils
{
    public static void SetMapFlag(uint mapIdFromData, uint territoryType, float x, float y)
    {
        try
        {
            DService.Instance().Log.Information($"[地图标记] SetMapFlag 开始: mapIdFromData={mapIdFromData}, TerritoryType={territoryType}, 数据坐标X:{x:F1}, Y:{y:F1}");
            
            // 获取真实的地图 ID
            var territoryTypeRow = DService.Instance().Data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>()?.GetRow(territoryType);
            uint realMapId = territoryTypeRow?.Map.RowId ?? mapIdFromData;
            
            // 获取地图缩放比例
            float mapScale = GetMapScale(territoryType);
            
            // 方案1：假设数据里的坐标是地图坐标，直接用（之前的方案）
            // float useMapX = x;
            // float useMapY = y;
            
            // 方案2：假设数据里的坐标是世界坐标，需要转换（测试用）
            float useMapX = ConvertWorldToMap(x, mapScale);
            float useMapY = ConvertWorldToMap(y, mapScale);
            
            DService.Instance().Log.Information($"[地图标记] 真实地图 ID: {realMapId}, 缩放比例: {mapScale}");
            DService.Instance().Log.Information($"[地图标记] 使用地图坐标: ({useMapX:F1}, {useMapY:F1})");
            
            var mapLink = new MapLinkPayload((ushort)territoryType, realMapId, useMapX, useMapY);
            DService.Instance().GameGUI.OpenMapWithMapLink(mapLink);
            
            DService.Instance().Log.Information($"[地图标记] 成功创建地图链接并打开地图");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[地图标记] 打开地图失败: {ex.Message}");
            DService.Instance().Log.Error($"[地图标记] 堆栈: {ex.StackTrace}");
        }
    }
    
    // 世界坐标转地图坐标（用于从游戏中获取怪物位置）
    public static float ConvertWorldToMap(float worldPos, float mapScale)
    {
        return 2048f / mapScale + worldPos / 50f + 1f;
    }
    
    // 地图坐标转世界坐标（反向转换）
    public static float ConvertMapToWorld(float mapPos, float mapScale)
    {
        return (mapPos - 2048f / mapScale - 1f) * 50f;
    }
    
    // 获取地图缩放比例
    public static float GetMapScale(uint territoryId)
    {
        return territoryId is >= 397 and <= 402 ? 95f : 100f;
    }
}
