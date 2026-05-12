using System;
using System.Numerics;
using OmenTools;
using EliteHunt.Data;

namespace EliteHunt.Managers;

public class NavigationManager
{
    private bool _isNavigating = false;
    private bool _isPaused = false;
    private MobHuntEntry? _currentTarget = null;
    private Vector3 _targetLocation = Vector3.Zero;
    private string _targetName = "";
    
    public event Action? OnNavigationStarted;
    public event Action? OnNavigationCompleted;
    public event Action? OnNavigationFailed;
    
    public bool IsNavigating => _isNavigating;
    public bool IsPaused => _isPaused;
    public MobHuntEntry? CurrentTarget => _currentTarget;
    public Vector3 TargetLocation => _targetLocation;
    public string TargetName => _targetName;

    public void Initialize()
    {
        DService.Instance().Log.Information("[导航] NavigationManager 已初始化");
    }

    /// <summary>
    /// 导航到指定的精英怪物
    /// </summary>
    public void NavigateToHunt(MobHuntEntry hunt)
    {
        if (_isNavigating)
        {
            DService.Instance().Chat.PrintError($"[导航] 已经在导航中，请先停止当前导航");
            return;
        }

        try
        {
            _currentTarget = hunt;
            _targetName = hunt.Name ?? "Unknown";
            
            DService.Instance().Log.Information($"[导航] 开始导航到: {_targetName} (TerritoryType: {hunt.TerritoryType})");
            DService.Instance().Chat.Print($"[导航] 正在导航到: {_targetName}");
            
            _isNavigating = true;
            _isPaused = false;
            OnNavigationStarted?.Invoke();
            
            // 触发Vnavmesh导航
            ExecuteVnavmeshNavigation(hunt.TerritoryType, hunt.MapId, hunt.Name ?? "Unknown");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] 导航失败: {ex.Message}");
            _isNavigating = false;
            OnNavigationFailed?.Invoke();
        }
    }

    /// <summary>
    /// 导航到指定坐标
    /// </summary>
    public void NavigateToLocation(uint territoryType, float x, float y, float z, string targetName = "目标位置")
    {
        if (_isNavigating)
        {
            DService.Instance().Chat.PrintError($"[导航] 已经在导航中，请先停止当前导航");
            return;
        }

        try
        {
            _targetLocation = new Vector3(x, y, z);
            _targetName = targetName;
            
            DService.Instance().Log.Information($"[导航] 开始导航到坐标: ({x:F1}, {y:F1}, {z:F1}) - {targetName}");
            DService.Instance().Chat.Print($"[导航] 正在导航到: {targetName}");
            
            _isNavigating = true;
            _isPaused = false;
            OnNavigationStarted?.Invoke();
            
            // 触发Vnavmesh导航到坐标
            ExecuteVnavmeshNavigationToCoordinates(territoryType, x, y, z);
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] 导航失败: {ex.Message}");
            _isNavigating = false;
            OnNavigationFailed?.Invoke();
        }
    }

    /// <summary>
    /// 停止导航
    /// </summary>
    public void StopNavigation()
    {
        if (!_isNavigating)
        {
            return;
        }

        try
        {
            DService.Instance().Log.Information("[导航] 停止导航");
            DService.Instance().Chat.Print("[导航] 导航已停止");
            
            _isNavigating = false;
            _isPaused = false;
            _currentTarget = null;
            _targetName = "";
            
            // 调用Vnavmesh停止导航
            StopVnavmeshNavigation();
            
            OnNavigationCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] 停止导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 暂停导航
    /// </summary>
    public void PauseNavigation()
    {
        if (!_isNavigating || _isPaused)
        {
            return;
        }

        try
        {
            _isPaused = true;
            DService.Instance().Log.Information("[导航] 导航已暂停");
            DService.Instance().Chat.Print("[导航] 导航已暂停");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] 暂停导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 恢复导航
    /// </summary>
    public void ResumeNavigation()
    {
        if (!_isNavigating || !_isPaused)
        {
            return;
        }

        try
        {
            _isPaused = false;
            DService.Instance().Log.Information("[导航] 导航已恢复");
            DService.Instance().Chat.Print("[导航] 导航已恢复");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] 恢复导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前导航信息
    /// </summary>
    public (bool IsNavigating, bool IsPaused, string TargetName) GetNavigationStatus()
    {
        return (_isNavigating, _isPaused, _targetName);
    }

    /// <summary>
    /// 执行Vnavmesh导航 - 导航到怪物
    /// </summary>
    private void ExecuteVnavmeshNavigation(uint territoryType, uint mapId, string targetName)
    {
        try
        {
            // 调用Vnavmesh插件的导航功能
            // 这里需要通过Dalamud的插件通信来调用Vnavmesh
            var currentTerritory = DService.Instance().ClientState?.TerritoryType;
            
            if (currentTerritory != territoryType)
            {
                DService.Instance().Chat.PrintError($"[导航] 目标在不同的区域，需要先传送到该区域");
                _isNavigating = false;
                return;
            }

            // 使用Vnavmesh提供的导航命令
            // 标准的Vnavmesh命令格式通常是通过插件API调用
            DService.Instance().Log.Information($"[导航] 调用Vnavmesh导航到: {targetName}");
            
            // 这里可以扩展为调用实际的Vnavmesh API
            // 例如通过 IPC (Inter-Process Communication) 或直接调用
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] Vnavmesh导航执行失败: {ex.Message}");
            _isNavigating = false;
            OnNavigationFailed?.Invoke();
        }
    }

    /// <summary>
    /// 执行Vnavmesh导航 - 导航到坐标
    /// </summary>
    private void ExecuteVnavmeshNavigationToCoordinates(uint territoryType, float x, float y, float z)
    {
        try
        {
            var currentTerritory = DService.Instance().ClientState?.TerritoryType;
            
            if (currentTerritory != territoryType)
            {
                DService.Instance().Chat.PrintError($"[导航] 目标在不同的区域，需要先传送到该区域");
                _isNavigating = false;
                return;
            }

            DService.Instance().Log.Information($"[导航] 调用Vnavmesh导航到坐标: ({x:F1}, {y:F1}, {z:F1})");
            
            // 这里可以扩展为调用实际的Vnavmesh API
            // 通过坐标导航
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] Vnavmesh坐标导航执行失败: {ex.Message}");
            _isNavigating = false;
            OnNavigationFailed?.Invoke();
        }
    }

    /// <summary>
    /// 停止Vnavmesh导航
    /// </summary>
    private void StopVnavmeshNavigation()
    {
        try
        {
            DService.Instance().Log.Information("[导航] 调用Vnavmesh停止导航");
            
            // 这里可以扩展为调用实际的Vnavmesh停止API
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[导航] 停止Vnavmesh导航失败: {ex.Message}");
        }
    }

    public void Cleanup()
    {
        if (_isNavigating)
        {
            StopNavigation();
        }
        
        DService.Instance().Log.Information("[导航] NavigationManager 已清理");
    }
}
