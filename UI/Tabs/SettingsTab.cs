using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace EliteHunt.UI.Tabs;

public class SettingsTab
{
	public void Draw()
	{
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 1.0f, 1.0f), "⚙️ 设置");
		ImGui.Separator();
		ImGui.Spacing();

		var config = Plugin.P?.Config;
		if (config == null)
		{
			ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), "配置无法加载");
			return;
		}

		bool enabled = config.Enabled;
		if (ImGui.Checkbox("启用插件", ref enabled))
		{
			config.Enabled = enabled;
			config.Save();
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		bool mapMarkerEnabled = config.MapMarkerEnabled;
		if (ImGui.Checkbox("启用地图标记", ref mapMarkerEnabled))
		{
			config.MapMarkerEnabled = mapMarkerEnabled;
			config.Save();
		}

		bool counterEnabled = config.CounterEnabled;
		if (ImGui.Checkbox("启用击杀计数", ref counterEnabled))
		{
			config.CounterEnabled = counterEnabled;
			config.Save();
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		bool showLocalHunts = config.ShowLocalHunts;
		if (ImGui.Checkbox("显示本地狩猎", ref showLocalHunts))
		{
			config.ShowLocalHunts = showLocalHunts;
			config.Save();
		}

		bool showKillCount = config.ShowKillCount;
		if (ImGui.Checkbox("显示击杀计数", ref showKillCount))
		{
			config.ShowKillCount = showKillCount;
			config.Save();
		}

		ImGui.Spacing();
		ImGui.TextDisabled("更多设置开发中...");
	}
}
