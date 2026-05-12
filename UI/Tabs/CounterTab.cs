using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace EliteHunt.UI.Tabs;

public class CounterTab
{
	public void Draw()
	{
		ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), "📊 S怪计数器");
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.TextWrapped("此功能显示当前地图S级怪的击杀计数。");
		ImGui.Spacing();
		ImGui.Text("使用 /eh counter 可以打开独立的计数器窗口。");
		ImGui.Spacing();
		ImGui.TextDisabled("更多功能开发中...");
	}
}
