using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace EliteHunt.UI.Tabs;

public class MapViewTab
{
	public void Draw()
	{
		ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), "🗺️ 地图视图");
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.TextWrapped("此功能将显示当前地图上的狩猎目标位置。");
		ImGui.Spacing();
		ImGui.TextDisabled("开发中...");
	}

	public void DrawMapWindow()
	{
		// 可以在这里绘制额外的地图窗口
	}
}
