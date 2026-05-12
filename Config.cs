using Dalamud.Configuration;
using Dalamud.Plugin;
using System.Numerics;
using OmenTools;

namespace EliteHunt;

public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // 通用设置
    public bool Enabled { get; set; } = true;
    public bool AutoTeleport { get; set; } = true;
    public float AutoTeleportAetheryteDistanceDiff { get; set; } = 3f;
    public bool UseMount { get; set; } = true;
    
    // 寻路设置
    public bool ForceFlyingPathfinding { get; set; } = false;
    
    // 标签页状态
    public int SelectedTab { get; set; } = 0;
    
    // 狩猎任务设置
    public bool ShowLocalHunts { get; set; } = true;
    public bool ShowHuntBounties { get; set; } = true;
    public bool AutoOpenMap { get; set; } = true;
    public bool ShowKillCount { get; set; } = true;
    public bool EnableXivEspIntegration { get; set; } = false;
    public bool AutoSetEspSearchOnNextHuntCommand { get; set; } = false;
    public bool IncludeAreaOnMap { get; set; } = false;
    public bool SuppressEliteMarkLocationWarning { get; set; } = false;
    
    // 击杀计数设置
    public bool CounterEnabled { get; set; } = false;
    public bool CountInBackground { get; set; } = true;
    public int SFoundCount { get; set; } = 0;
    public int AFoundCount { get; set; } = 0;
    public int BFoundCount { get; set; } = 0;
    public Vector2 CounterWindowPos { get; set; } = new(100, 100);
    public Vector2 CounterWindowSize { get; set; } = new(200, 300);
    
    // 语音通知 (TTS)
    public bool TTSEnabled { get; set; } = true;
    public int TTSVolume { get; set; } = 50;
    public string TTSAMessage { get; set; } = "<rank> Nearby";
    public string TTSBMessage { get; set; } = "<rank> Nearby";
    public string TTSSMessage { get; set; } = "<rank> in zone";
    
    // 聊天通知
    public bool ChatAEnabled { get; set; } = false;
    public bool ChatBEnabled { get; set; } = false;
    public bool ChatSEnabled { get; set; } = true;
    public string ChatAMessage { get; set; } = "[A] <name> 已发现！";
    public string ChatBMessage { get; set; } = "[B] <name> 已发现！";
    public string ChatSMessage { get; set; } = "[S] <name> 在 <zone> 出现了！";
    
    // FlyText 通知
    public bool FlyTextAEnabled { get; set; } = false;
    public bool FlyTextBEnabled { get; set; } = false;
    public bool FlyTextSEnabled { get; set; } = true;
    
    // 地图设置
    public bool MapOverlayEnabled { get; set; } = true;
    
    // 指向设置
    public bool PointerEnabled { get; set; } = false;
    
    // 列车设置
    public bool HuntTrainEnabled { get; set; } = false;
    
    // 子功能开关
    public bool HuntTaskEnabled { get; set; } = true;
    public bool AutoTeleportEnabled { get; set; } = true;
    public bool MapMarkerEnabled { get; set; } = true;
    
    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        
    }
    
    public void Save()
    {
        DService.Instance().PI.SavePluginConfig(this);
    }
}
