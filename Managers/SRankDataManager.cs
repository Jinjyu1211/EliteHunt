using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using EliteHunt.Data;
using OmenTools;

namespace EliteHunt.Managers;

public class SRankDataManager
{
    private SRankData? _data;
    private readonly Dictionary<ushort, Territory> _territoryCache = new();

    public SRankData? Data => _data;
    public bool IsLoaded => _data != null;

    public SRankDataManager()
    {
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "EliteHunt.Data.SRankData.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                DService.Instance().Log.Warning("[SRankDataManager] 无法找到嵌入的资源文件 SRankData.json");
                return;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            _data = JsonSerializer.Deserialize<SRankData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (_data != null)
            {
                BuildTerritoryCache();
                DService.Instance().Log.Information($"[SRankDataManager] 成功加载数据，版本: {_data.Version}，包含 {_territoryCache.Count} 个地图");
            }
            else
            {
                DService.Instance().Log.Error("[SRankDataManager] 反序列化数据失败");
            }
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[SRankDataManager] 加载数据失败: {ex.Message}");
        }
    }

    private void BuildTerritoryCache()
    {
        _territoryCache.Clear();

        if (_data?.Maps == null) return;

        foreach (var mapVersion in _data.Maps)
        {
            foreach (var territory in mapVersion.Territories)
            {
                if (!_territoryCache.ContainsKey(territory.MapId))
                {
                    _territoryCache[territory.MapId] = territory;
                }
            }
        }
    }

    public Territory? GetTerritory(ushort mapId)
    {
        return _territoryCache.TryGetValue(mapId, out var territory) ? territory : null;
    }

    public SRankMark? GetSRankMark(ushort mapId)
    {
        var territory = GetTerritory(mapId);
        return territory?.SRankMark;
    }

    public List<Territory> GetAllTerritories()
    {
        return _territoryCache.Values.ToList();
    }

    public List<MapVersion> GetAllMapVersions()
    {
        return _data?.Maps ?? new List<MapVersion>();
    }

    public List<Territory> GetTerritoriesWithSRank()
    {
        return _territoryCache.Values
            .Where(t => t.SRankMark != null)
            .ToList();
    }

    public bool HasSRank(ushort mapId)
    {
        var mark = GetSRankMark(mapId);
        return mark != null;
    }
}
