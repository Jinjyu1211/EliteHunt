using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using EliteHunt.UI.Tabs;

namespace EliteHunt.UI
{
    public class MainWindow
    {
        public bool Visible = false;
        private int SelectedTab = 0;
        private readonly string[] TabNames = new[]
        {
            "地图视图",
            "S怪计数器",
            "设置"
        };

        private readonly MapViewTab _mapViewTab;
        private readonly CounterTab _counterTab;
        private readonly SettingsTab _settingsTab;

        private bool _isAnimating = false;
        private float _animationProgress = 0f;
        private double _lastTime = 0;

        public MainWindow()
        {
            _mapViewTab = new MapViewTab();
            _counterTab = new CounterTab();
            _settingsTab = new SettingsTab();
        }

        public void Draw()
        {
            if (!Visible) return;

            UpdateAnimation();

            ImGui.SetNextWindowSize(new Vector2(900, 650), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(800, 500), new Vector2(1200, 800));

            var windowFlags = ImGuiWindowFlags.NoResize;
            
            if (ImGui.Begin("EliteHunt - 精英狩猎助手", ref Visible, windowFlags))
            {
                DrawHeader();
                
                ImGui.Columns(2, "maincolumns", false);
                ImGui.SetColumnWidth(0, 200);

                DrawSidebar();

                ImGui.NextColumn();

                DrawContent();

                ImGui.Columns(1);
            }
            ImGui.End();

            _mapViewTab.DrawMapWindow();
        }

        private void UpdateAnimation()
        {
            if (_isAnimating)
            {
                double currentTime = ImGui.GetTime();
                if (currentTime - _lastTime > 0.016)
                {
                    _animationProgress += 0.05f;
                    _lastTime = currentTime;
                    if (_animationProgress >= 1f)
                    {
                        _animationProgress = 1f;
                        _isAnimating = false;
                    }
                }
            }
        }

        private void DrawHeader()
        {
            var headerHeight = 60f;
            var gradientStart = new Vector4(0.15f, 0.25f, 0.35f, 1f);
            var gradientEnd = new Vector4(0.25f, 0.35f, 0.45f, 1f);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, 0));
            if (ImGui.BeginChild("Header", new Vector2(0, headerHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                DrawGradientRect(gradientStart, gradientEnd, new Vector2(ImGui.GetContentRegionAvail().X, headerHeight));
                
                ImGui.SetCursorPos(new Vector2(20, (headerHeight - 30) / 2));
                ImGui.PushFont(ImGui.GetFont());
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.95f, 0.9f, 0.7f, 1f));
                ImGui.Text("⚔️ EliteHunt");
                ImGui.PopStyleColor();
                ImGui.PopFont();

                ImGui.SameLine();
                ImGui.SetCursorPos(new Vector2(150, (headerHeight - 20) / 2));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.7f, 0.8f, 1f));
                ImGui.Text("精英狩猎助手 v1.0.0");
                ImGui.PopStyleColor();

                var config = Plugin.P?.Config;
                if (config != null)
                {
                    ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().X - 120, (headerHeight - 24) / 2));
                    var statusColor = config.Enabled ? new Vector4(0.3f, 0.8f, 0.3f, 1f) : new Vector4(0.8f, 0.3f, 0.3f, 1f);
                    ImGui.PushStyleColor(ImGuiCol.Text, statusColor);
                    ImGui.Text(config.Enabled ? "● 已启用" : "● 已禁用");
                    ImGui.PopStyleColor();
                }

                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
        }

        private void DrawGradientRect(Vector4 start, Vector4 end, Vector2 size)
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            
            drawList.AddRectFilledMultiColor(
                pos,
                pos + size,
                ImGui.ColorConvertFloat4ToU32(start),
                ImGui.ColorConvertFloat4ToU32(end),
                ImGui.ColorConvertFloat4ToU32(end),
                ImGui.ColorConvertFloat4ToU32(start)
            );
        }

        private void DrawSidebar()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 10));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));

            var sidebarBg = new Vector4(0.12f, 0.15f, 0.2f, 1f);
            var hoverBg = new Vector4(0.25f, 0.3f, 0.4f, 1f);
            var activeBg = new Vector4(0.35f, 0.45f, 0.55f, 1f);

            var iconList = new[] { "🗺️", "📊", "⚙️" };

            ImGui.PushStyleColor(ImGuiCol.WindowBg, sidebarBg);
            if (ImGui.BeginChild("Sidebar", new Vector2(0, ImGui.GetContentRegionAvail().Y), true, ImGuiWindowFlags.NoScrollbar))
            {
                for (int i = 0; i < TabNames.Length; i++)
                {
                    bool isActive = SelectedTab == i;
                    bool isHovered;
                    
                    ImGui.PushStyleColor(ImGuiCol.Button, isActive ? activeBg : sidebarBg);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverBg);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeBg);
                    
                    isHovered = ImGui.Button($"{iconList[i]}  {TabNames[i]}", new Vector2(ImGui.GetContentRegionAvail().X, 40));
                    
                    ImGui.PopStyleColor(3);

                    if (isHovered)
                    {
                        SelectedTab = i;
                        _isAnimating = true;
                        _animationProgress = 0f;
                        _lastTime = ImGui.GetTime();
                    }

                    if (isActive)
                    {
                        DrawActiveIndicator();
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                DrawFeatureToggleSection();

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
        }

        private void DrawActiveIndicator()
        {
            var drawList = ImGui.GetWindowDrawList();
            var lastItemRect = ImGui.GetItemRectMax();
            var indicatorPos = new Vector2(ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMin().X, lastItemRect.Y - 40);
            drawList.AddRectFilled(indicatorPos, indicatorPos + new Vector2(4, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.8f, 1f, 1f)));
        }

        private void DrawFeatureToggleSection()
        {
            var config = Plugin.P?.Config;
            if (config == null) return;

            ImGui.TextColored(new Vector4(0.6f, 0.7f, 0.8f, 1f), "功能开关");
            ImGui.Spacing();

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4);
            
            bool mapMarker = config.MapMarkerEnabled;
            if (ImGui.Checkbox("[地图标记]", ref mapMarker))
            {
                config.MapMarkerEnabled = mapMarker;
                config.Save();
            }

            bool counter = config.CounterEnabled;
            if (ImGui.Checkbox("[击杀计数]", ref counter))
            {
                config.CounterEnabled = counter;
                config.Save();
            }

            ImGui.PopStyleVar();
        }

        private void DrawContent()
        {
            var contentBg = new Vector4(0.08f, 0.1f, 0.12f, 1f);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, contentBg);
            if (ImGui.BeginChild("Content", new Vector2(0, ImGui.GetContentRegionAvail().Y), true))
            {
                if (_isAnimating)
                {
                    ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - 200) / 2);
                    ImGui.SetCursorPosY((ImGui.GetContentRegionAvail().Y - 50) / 2);
                    ImGui.ProgressBar(_animationProgress, new Vector2(200, 20), "加载中...");
                }
                else
                {
                    DrawTabContent();
                }

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
        }

        private void DrawTabContent()
        {
            var config = Plugin.P?.Config;
            if (config == null)
            {
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "错误: 配置未加载");
                return;
            }

            switch (SelectedTab)
            {
                case 0:
                    _mapViewTab.Draw();
                    break;
                case 1:
                    if (config.CounterEnabled)
                        _counterTab.Draw();
                    else
                        DrawFeatureDisabled("击杀计数");
                    break;
                case 2:
                    _settingsTab.Draw();
                    break;
            }
        }

        private void DrawFeatureDisabled(string featureName)
        {
            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - 250) / 2);
            ImGui.SetCursorPosY((ImGui.GetContentRegionAvail().Y - 80) / 2);
            
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
            ImGui.Text("🔒 功能已禁用");
            ImGui.Spacing();
            ImGui.Text($"「{featureName}」功能当前处于关闭状态");
            ImGui.Text("请在左侧功能开关中启用此功能");
            ImGui.PopStyleColor();
        }

        public void Toggle()
        {
            Visible = !Visible;
            if (Visible)
            {
                _isAnimating = true;
                _animationProgress = 0f;
                _lastTime = ImGui.GetTime();
            }
        }
    }
}
