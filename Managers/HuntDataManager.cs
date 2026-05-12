using System;
using System.Collections.Generic;
using System.Linq;
using EliteHunt.Data;
using OmenTools;

namespace EliteHunt.Managers;

public class HuntDataManager
{
	private readonly HuntData _huntData;
	private readonly Dictionary<uint, List<MobHuntEntry>> _territoryHunts = new();

	public event Action? OnTerritoryChanged;

	public HuntDataManager()
	{
		_huntData = new HuntData();
	}

	public void Initialize()
	{
		LoadHuntData();
		var clientState = DService.Instance().ClientState;
		if (clientState != null)
		{
			clientState.TerritoryChanged += ClientState_TerritoryChanged;
		}
	}

	public void LoadHuntData()
	{
		try
		{
			DService.Instance().Log.Information("[HuntDataManager] 加载狩猎数据");
			_territoryHunts.Clear();
		}
		catch (Exception ex)
		{
			DService.Instance().Log.Error($"[HuntDataManager] 加载失败: {ex.Message}");
		}
	}

	public List<MobHuntEntry> GetCurrentAreaHunts()
	{
		var clientState = DService.Instance().ClientState;
		if (clientState == null) return new List<MobHuntEntry>();

		uint territoryType = clientState.TerritoryType;
		return _territoryHunts.TryGetValue(territoryType, out var hunts)
			? hunts
			: new List<MobHuntEntry>();
	}

	public void RefreshKillCounts()
	{
		DService.Instance().Log.Information("[HuntDataManager] 刷新击杀计数");
	}

	public void Cleanup()
	{
		var clientState = DService.Instance().ClientState;
		if (clientState != null)
		{
			clientState.TerritoryChanged -= ClientState_TerritoryChanged;
		}
	}

	private void ClientState_TerritoryChanged(uint territoryType)
	{
		OnTerritoryChanged?.Invoke();
	}
}
