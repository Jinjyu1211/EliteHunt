using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace EliteHunt;

public class DService
{
    private static DService? _instance;
    private readonly IDalamudPluginInterface _pluginInterface;

    public IClientState ClientState => Service.ClientState;
    public IFramework Framework => Service.Framework;
    public IPluginLog Log => Service.PluginLog;
    public IObjectTable ObjectTable => Service.ObjectTable;
    public ICommandManager Command => Service.Commands;
    public IChatGui Chat => Service.Chat;
    public IDataManager Data => Service.DataManager;
    public IDalamudPluginInterface PI => _pluginInterface;
    public IGameGui GameGUI => Service.GameGui;

    private DService(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public static void Init(IDalamudPluginInterface pluginInterface)
    {
        _instance = new DService(pluginInterface);
    }

    public static DService Instance()
    {
        return _instance ?? throw new System.InvalidOperationException("DService has not been initialized");
    }

    public static void Uninit()
    {
        _instance = null;
    }
}