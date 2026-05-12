using Dalamud.Plugin;
using OmenTools;
using EliteHunt.Managers;
using EliteHunt.Data;
using EliteHunt.UI;
using EliteHunt.Utils;
using System;
using System.Numerics;
using Framework = Dalamud.Plugin.Services.IFramework;
using Dalamud.Bindings.ImGui;

namespace EliteHunt;

public class Plugin : IDalamudPlugin
{
    public string Name => "EliteHunt - 精英狩猎助手";

    internal static Plugin? P;

    private IDalamudPluginInterface _pluginInterface = null!;
    
    public Config Config { get; private set; } = null!;
    
    public UI.MainWindow MainWindow { get; private set; } = null!;
    
    public TaskHelper TaskHelper { get; private set; } = null!;

    public HuntData HuntData { get; private set; } = null!;
    
    public HuntDataManager HuntDataManager { get; private set; } = null!;
    
    public CounterManager CounterManager { get; private set; } = null!;

    public SRankDataManager SRankDataManager { get; private set; } = null!;

    public MapMarkerManager MapMarkerManager { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        
        _pluginInterface = pluginInterface;
        
        DService.Init(pluginInterface);
        
        Config = pluginInterface.GetPluginConfig() as Config ?? new Config();
        Config.Initialize(pluginInterface);
        
        HuntData = new HuntData();
        
        HuntDataManager = new HuntDataManager();
        SRankDataManager = new SRankDataManager();
        CounterManager = new CounterManager();
        MapMarkerManager = new MapMarkerManager();
        
        CounterManager.SetConfigValues(Config.CountInBackground, Config.CounterWindowPos, Config.CounterWindowSize);
        
        MainWindow = new UI.MainWindow();
        
        TaskHelper = new TaskHelper();
        
        InitializeManagers();
        
        DService.Instance().Command.AddHandler("/eh", new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "EliteHunt 插件命令 - 打开主窗口"
        });
        DService.Instance().Command.AddHandler("/eh reload", new Dalamud.Game.Command.CommandInfo(OnReloadCommand)
        {
            HelpMessage = "重新加载狩猎数据"
        });
        
        DService.Instance().UIBuilder.Draw += OnDraw;
        
        _pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        _pluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        
        DService.Instance().Log.Information("EliteHunt loaded!");
    }
    
    private void OpenConfigUi()
    {
        MainWindow.Visible = true;
    }
    
    private void OpenMainUi()
    {
        MainWindow.Visible = true;
    }

    private void InitializeManagers()
    {
        HuntDataManager.Initialize();
        CounterManager.Initialize();

        HuntDataManager.OnTerritoryChanged += OnTerritoryChanged;
    }

    private void OnTerritoryChanged()
    {
        HuntDataManager.RefreshKillCounts();
    }

    private void OnCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args) || args.Equals("toggle", StringComparison.OrdinalIgnoreCase) || args.Equals("ui", StringComparison.OrdinalIgnoreCase))
        {
            MainWindow.Toggle();
        }
        else if (args.Equals("reload", StringComparison.OrdinalIgnoreCase))
        {
            ReloadHuntData();
        }
        else if (args.Equals("counter", StringComparison.OrdinalIgnoreCase))
        {
            CounterManager.ToggleWindow();
        }
    }

    private void OnReloadCommand(string command, string args)
    {
        ReloadHuntData();
    }

    public void ReloadHuntData()
    {
        DService.Instance().Log.Information("重新加载狩猎数据...");
        HuntDataManager.LoadHuntData();
        DService.Instance().Chat.Print("正在重新加载狩猎数据...");
    }

    private void OnDraw()
    {
        MainWindow.Draw();
        DrawLocalHuntsWindow();
        CounterManager.DrawCounterWindow();
    }

    private void DrawLocalHuntsWindow()
    {
        if (!Config.ShowLocalHunts) return;
        
        var currentHunts = HuntDataManager.GetCurrentAreaHunts();
        if (currentHunts == null || currentHunts.Count == 0) return;

        bool isOpen = true;
        ImGui.Begin("EliteHunt - 本地狩猎", ref isOpen, ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoDocking);
        
        foreach (var hunt in currentHunts)
        {
            var remaining = hunt.NeededKills - hunt.CurrentKills;
            if (Config.ShowKillCount && remaining > 0)
            {
                ImGui.Text($"{hunt.Name} ({hunt.CurrentKills}/{hunt.NeededKills})");
            }
            else if (Config.ShowKillCount)
            {
                ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.3f, 1f), $"{hunt.Name} ✓");
            }
            else
            {
                ImGui.Text(hunt.Name ?? "Unknown");
            }
        }
        
        ImGui.End();
        
        if (!isOpen)
        {
            Config.ShowLocalHunts = false;
            Config.Save();
        }
    }

    public void Dispose()
    {
        var (countInBackground, windowPos, windowSize) = CounterManager.GetConfigValues();
        Config.CountInBackground = countInBackground;
        Config.CounterWindowPos = windowPos;
        Config.CounterWindowSize = windowSize;
        Config.Save();

        DService.Instance().UIBuilder.Draw -= OnDraw;
        
        HuntDataManager.OnTerritoryChanged -= OnTerritoryChanged;
        
        HuntDataManager.Cleanup();
        CounterManager.Cleanup();
        
        DService.Instance().Command.RemoveHandler("/eh");
        DService.Instance().Command.RemoveHandler("/eh reload");
        TaskHelper.Dispose();
        DService.Uninit();
        P = null;
    }
}
