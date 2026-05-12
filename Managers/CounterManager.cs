using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Media;
using System.IO;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Textures;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using OmenTools;
using OmenTools.Dalamud.Services.ObjectTable.Abstractions.ObjectKinds;
using EliteHunt.Data;

namespace EliteHunt.Managers;

public class CounterManager
{
    private bool _countInBackground = true;
    private bool _windowVisible = false;
    private Vector2 _windowPos = new(100, 100);
    private Vector2 _windowSize = new(320, 280);
    private ushort _currentTerritoryType = 0;
    private readonly Dictionary<ushort, Dictionary<string, int>> _territoryTallies = new();
    
    // 坐标索引跟踪：用于点击怪物时循环切换不同位置
    private readonly Dictionary<string, int> _mobLocationIndex = new();
    
    // 窗口设置
    private bool _windowLocked = false;
    private float _windowOpacity = 1.0f;
    private string _backgroundImagePath = string.Empty;
    
    // 增强的击杀检测：HP追踪系统
    private readonly Dictionary<ulong, (string Name, uint CurrentHp, uint MaxHp, bool IsDead)> _trackedMobs = new();
    private DateTime _lastCheckTime = DateTime.MinValue;
    private DateTime _lastStatsLogTime = DateTime.MinValue;  // 统计日志专用时间戳
    private const int CHECK_INTERVAL_MS = 200; // 检测频率：200ms
    private const int STATS_LOG_INTERVAL_SEC = 60; // 统计日志频率：60秒（1分钟）
    
    // 声音提示相关
    private bool _soundEnabled = true;
    private DateTime _lastSoundTime = DateTime.MinValue;
    private const int SOUND_COOLDOWN_MS = 3000; // 声音冷却3秒，避免连续播放
    private HashSet<string> _completedMobs = new(); // 已完成目标的怪物集合
    
    // 防重复计数：记录已处理的击杀事件
    private HashSet<ulong> _processedKillEvents = new(); // 已处理的怪物ID（防止同一只被多次计数）
    
    // 目标数量缓存：避免频繁重复解析triggerDescription
    private Dictionary<string, int>? _cachedTargetCounts = null;
    private string? _cachedTriggerDescription = null;

    public CounterManager()
    {
        // 无需初始化声音资源，使用系统API直接播放
    }

    public void Initialize()
    {
        var clientState = DService.Instance().ClientState;
        if (clientState != null)
        {
            _currentTerritoryType = (ushort)clientState.TerritoryType;
            clientState.TerritoryChanged += ClientState_TerritoryChanged;
        }
        
        // 注册Framework.Update事件用于自动击杀检测
        try
        {
            DService.Instance().Framework.Update += OnFrameworkUpdate;
            DService.Instance().Log.Information(
                "[计数器] ✅✅✅ 增强版自动击杀监听已启用！\n" +
                "📝 检测机制：HP归零精确检测 + 全地图范围 + 所有玩家统计\n" +
                "🔊 功能：目标达成时自动播放提示音\n" +
                "⚡ 检测频率：200ms（高精度模式）\n" +
                "🎯 请进入支持的地图并测试。");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[计数器] ❌ 初始化Framework.Update失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Framework.Update回调 - 多重击杀检测机制
    /// </summary>
    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_countInBackground) return;

        // 控制检查频率
        if ((DateTime.Now - _lastCheckTime).TotalMilliseconds < CHECK_INTERVAL_MS) return;
        _lastCheckTime = DateTime.Now;

        try
        {
            var sRankMark = GetCurrentSRankMark();
            if (sRankMark == null || sRankMark.TargetMobs == null || sRankMark.TargetMobs.Count == 0) return;

            // 获取当前场景中的所有对象（全地图范围）
            var objectTable = DService.Instance().ObjectTable;
            if (objectTable == null) return;

            // 统计信息
            int totalObjects = 0;
            int targetObjectsFound = 0;

            // 收集当前存活的目标怪物ID和状态
            var currentAliveTargets = new HashSet<ulong>();
            var killEvents = new List<(string Name, ulong Id, string DetectionMethod)>();

            foreach (var obj in objectTable)
            {
                totalObjects++;
                if (obj == null) continue;
                
                // 只关注战斗NPC
                if (obj.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc) continue;
                
                string? mobName = obj.Name?.TextValue;
                if (string.IsNullOrEmpty(mobName)) continue;
                
                // 检查是否是目标怪物
                if (!sRankMark.TargetMobs.Contains(mobName)) continue;
                
                targetObjectsFound++;
                ulong objId = obj.GameObjectID;
                
                // 使用OmenTools的稳定接口获取死亡状态
                bool isDead = obj.IsDead;
                bool isTargetable = obj.IsTargetable;

                // 记录当前存在的怪物
                currentAliveTargets.Add(objId);

                // 更新或创建追踪记录
                if (_trackedMobs.TryGetValue(objId, out var previous))
                {
                    // ===== 方法1：IsDead状态变化检测（最可靠）=====
                    if (!previous.IsDead && isDead)
                    {
                        // 怪物刚刚死亡！
                        killEvents.Add((mobName, objId, "IsDead状态变化"));
                        
                        DService.Instance().Log.Information(
                            $"[计数器] 🎯💀 死亡检测: {mobName} " +
                            $"(ID: {objId:X}, IsDead: {previous.IsDead}->{isDead})");
                    }
                    
                    // 更新追踪状态
                    _trackedMobs[objId] = (mobName, 0, 0, isDead);
                }
                else
                {
                    // 新发现的怪物，添加到追踪列表
                    _trackedMobs[objId] = (mobName, 0, 0, isDead);
                    
                    // 只在首次发现时静默记录，不输出日志（避免刷屏）
                    // 如需调试可临时改为 Information 级别
                }
            }

            // ===== 方法2：对象消失检测（备用机制）=====
            var disappearedMobs = new List<(string Name, ulong Id)>();
            
            foreach (var kvp in _trackedMobs.ToList())
            {
                var (name, _, __, wasDead) = kvp.Value;
                
                // 如果怪物不在当前对象表中
                if (!currentAliveTargets.Contains(kvp.Key))
                {
                    // 如果之前不是死亡状态，说明它突然消失了（可能是被击杀后快速消失或超出范围）
                    if (!wasDead)
                    {
                        disappearedMobs.Add((name, kvp.Key));
                        // 静默处理，不输出日志
                    }
                    
                    // 从追踪列表中延迟移除（避免内存泄漏）
                    if ((DateTime.Now - _lastCheckTime).TotalSeconds > 5)
                    {
                        _trackedMobs.Remove(kvp.Key);
                    }
                }
            }

            // 输出调试统计信息（每60秒一次，避免日志刷屏）
            if ((DateTime.Now - _lastStatsLogTime).TotalSeconds >= STATS_LOG_INTERVAL_SEC)
            {
                _lastStatsLogTime = DateTime.Now;
                DService.Instance().Log.Information(
                    $"[计数器] 📊 状态报告: 总对象={totalObjects}, " +
                    $"目标={targetObjectsFound}, 追踪中={_trackedMobs.Count}, " +
                    $"已处理事件={_processedKillEvents.Count}");
            }

            // 处理所有检测到的击杀事件
            foreach (var (mobName, mobId, method) in killEvents)
            {
                ProcessKillEvent(mobName, mobId, method);
            }

            // 处理消失的怪物（备用检测机制）
            foreach (var (mobName, mobId) in disappearedMobs)
            {
                ProcessKillEvent(mobName, mobId, "对象消失");
            }
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[计数器] Framework.Update处理异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 处理击杀事件：更新计数、检查目标、播放提示
    /// </summary>
    private void ProcessKillEvent(string mobName, ulong mobId, string detectionMethod)
    {
        // 防止重复计数：同一只怪物只计数一次
        if (_processedKillEvents.Contains(mobId))
        {
            DService.Instance().Log.Debug(
                $"[计数器] ⚠️ 跳过重复事件: {mobName} (ID: {mobId:X}, 已处理过)");
            return;
        }
        
        // 标记为已处理
        _processedKillEvents.Add(mobId);
        
        // 增加计数
        IncrementCount(mobName);
        
        var tally = GetCurrentTerritoryTally();
        int currentCount = tally.GetValueOrDefault(mobName, 0);
        
        DService.Instance().Log.Information(
            $"[计数器] ✅🎉 击杀确认: {mobName} " +
            $"(ID: {mobId:X}, 检测方式: {detectionMethod}, 当前总计: {currentCount})");

        // 获取目标数量并检查是否达成
        int targetCount = GetTargetCount(mobName);
        
        if (currentCount >= targetCount && !_completedMobs.Contains(mobName))
        {
            // 目标达成！
            _completedMobs.Add(mobName);
            
            OnTargetCompleted(mobName, currentCount, targetCount);
        }
    }

    /// <summary>
    /// 获取目标怪物的击杀/采集目标数量（带缓存，避免频繁解析）
    /// 优先级：缓存 > KillCount配置 > GatherCount配置 > triggerDescription解析 > 默认值
    /// </summary>
    private int GetTargetCount(string mobName)
    {
        var sRankMark = GetCurrentSRankMark();
        if (sRankMark == null) return 1;

        // 方法0：检查缓存（性能优化关键！）
        if (_cachedTargetCounts != null && 
            _cachedTriggerDescription == sRankMark.TriggerDescription &&
            _cachedTargetCounts.TryGetValue(mobName, out var cachedTarget))
        {
            return cachedTarget; // 直接返回缓存结果，无需重新解析
        }

        // 缓存未命中或数据已变化，需要重新解析
        // 方法1：尝试从KillCount字典获取
        if (sRankMark.KillCount != null && sRankMark.KillCount.TryGetValue(mobName, out var killTarget))
        {
            UpdateCache(sRankMark, mobName, killTarget);
            return killTarget;
        }
        
        // 方法2：尝试从GatherCount字典获取
        if (sRankMark.GatherCount != null && sRankMark.GatherCount.TryGetValue(mobName, out var gatherTarget))
        {
            UpdateCache(sRankMark, mobName, gatherTarget);
            return gatherTarget;
        }

        // 方法3：从triggerDescription智能解析（只在首次或描述变化时执行）
        if (!string.IsNullOrEmpty(sRankMark.TriggerDescription))
        {
            // 一次性解析所有目标怪物并缓存
            ParseAndCacheAllTargets(sRankMark);
            
            // 再次尝试从缓存获取
            if (_cachedTargetCounts != null && _cachedTargetCounts.TryGetValue(mobName, out var parsedTarget))
            {
                return parsedTarget;
            }
        }

        return 1; // 最终默认值
    }
    
    /// <summary>
    /// 更新单个目标的缓存
    /// </summary>
    private void UpdateCache(SRankMark sRankMark, string mobName, int target)
    {
        if (_cachedTargetCounts == null || _cachedTriggerDescription != sRankMark.TriggerDescription)
        {
            _cachedTargetCounts = new Dictionary<string, int>();
            _cachedTriggerDescription = sRankMark.TriggerDescription;
            
            DService.Instance().Log.Debug($"[计数器] 🔄 目标数量缓存已重置");
        }
        
        _cachedTargetCounts[mobName] = target;
    }
    
    /// <summary>
    /// 一次性解析所有目标怪物并缓存（避免重复正则匹配）
    /// </summary>
    private void ParseAndCacheAllTargets(SRankMark sRankMark)
    {
        if (string.IsNullOrEmpty(sRankMark.TriggerDescription) || sRankMark.TargetMobs == null) return;
        
        var newCache = new Dictionary<string, int>();
        
        foreach (var targetMob in sRankMark.TargetMobs)
        {
            int parsedTarget = ParseTargetFromDescriptionInternal(sRankMark.TriggerDescription, targetMob);
            newCache[targetMob] = parsedTarget > 0 ? parsedTarget : 1;
        }
        
        _cachedTargetCounts = newCache;
        _cachedTriggerDescription = sRankMark.TriggerDescription;
        
        // 只在首次解析时输出日志（不是每次调用都输出）
        DService.Instance().Log.Information(
            $"[计数器] 📊 已解析目标数量: {string.Join(", ", newCache.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }

    /// <summary>
    /// 从触发描述中解析目标数量（内部方法，无日志输出）
    /// 支持格式："击杀100只XXX"、"采集50个XXX"、"击败200个XXX"
    /// </summary>
    private int ParseTargetFromDescriptionInternal(string description, string mobName)
    {
        try
        {
            // 匹配模式：数字 + 单位 + 怪物名称
            var patterns = new[]
            {
                $@"(\d+)\s*[只个]\s*{Regex.Escape(mobName)}",  // "100只思考之物"
                $@"{Regex.Escape(mobName)}\s*[:：xX]*\s*(\d+)",   // "思考之物:100"
                @"击杀\s*(\d+)\s*只",                      // "击杀100只"（通用）
                @"采集\s*(\d+)\s*个",                      // "采集50个"
                @"击败\s*(\d+)\s*个",                      // "击败200个"
                @"(\d+)\s*[只个]",                           // "100只"（最后备选）
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(description, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (int.TryParse(match.Groups[1].Value, out int target) && target > 0)
                    {
                        return target;
                    }
                }
            }
        }
        catch
        {
            // 静默处理异常，避免日志刷屏
        }

        return 0; // 表示未找到
    }

    /// <summary>
    /// 目标达成时的处理：播放声音、显示通知
    /// </summary>
    private void OnTargetCompleted(string mobName, int currentCount, int targetCount)
    {
        DService.Instance().Log.Information(
            $"[计数器] 🎊🎊🎊 目标达成! {mobName}: {currentCount}/{targetCount}");

        // 播放提示音（带冷却时间）
        PlayCompletionSound();

        // 在聊天框显示醒目通知
        ShowCompletionNotification(mobName, currentCount, targetCount);
    }

    /// <summary>
    /// 播放完成提示音
    /// </summary>
    private void PlayCompletionSound()
    {
        if (!_soundEnabled) return;
        
        // 冷却时间检查
        if ((DateTime.Now - _lastSoundTime).TotalMilliseconds < SOUND_COOLDOWN_MS) return;
        _lastSoundTime = DateTime.Now;

        try
        {
            // 使用系统API直接播放提示音（异步，不阻塞）
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch
                {
                    // 备选方案：使用控制台蜂鸣声
                    try
                    {
                        Console.Beep(800, 200); // 800Hz, 200ms
                    }
                    catch
                    {
                        // 忽略所有声音错误
                    }
                }
            });
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Debug($"[计数器] 播放声音失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示完成通知到聊天框
    /// </summary>
    private void ShowCompletionNotification(string mobName, int currentCount, int targetCount)
    {
        try
        {
            var notification = new SeStringBuilder()
                .AddUiForeground(50) // 金色
                .AddText($"[EliteHunt] 🎊 目标达成！")
                .AddUiForegroundOff()
                .AddText($" {mobName}")
                .AddUiForeground(2) // 绿色
                .AddText($" 已完成 {currentCount}/{targetCount}")
                .AddUiForegroundOff()
                .Build();

            DService.Instance().Chat.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = notification
            });
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Debug($"[计数器] 显示通知失败: {ex.Message}");
        }
    }

    public void SetConfigValues(bool countInBackground, Vector2 windowPos, Vector2 windowSize)
    {
        _countInBackground = countInBackground;
        _windowPos = windowPos;
        _windowSize = windowSize;
    }

    public (bool CountInBackground, Vector2 WindowPos, Vector2 WindowSize) GetConfigValues()
    {
        return (_countInBackground, _windowPos, _windowSize);
    }

    public void ToggleWindow()
    {
        _windowVisible = !_windowVisible;
    }

    public void DrawCounterWindow()
    {
        if (!_windowVisible) return;

        ImGui.SetNextWindowPos(_windowPos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(_windowSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowBgAlpha(_windowOpacity);

        var windowFlags = ImGuiWindowFlags.NoScrollbar;
        if (_windowLocked)
        {
            windowFlags |= ImGuiWindowFlags.NoMove;
        }

        bool isOpen = _windowVisible;
        if (ImGui.Begin("EliteHunt - S怪计数器", ref isOpen, windowFlags))
        {
            if (!isOpen)
            {
                _windowVisible = false;
            }

            DrawBackgroundImage();

            DrawWindowControls();

            ImGui.Separator();
            ImGui.Spacing();

            var sRankMark = GetCurrentSRankMark();
            if (sRankMark != null)
            {
                DrawSRankCounter(sRankMark);
            }
            else
            {
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "当前地图暂未配置S怪触发器");
                ImGui.TextDisabled("请进入支持的地图（如叹息海、萨维奈等）");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            DrawControls();
        }
        ImGui.End();

        _windowPos = ImGui.GetWindowPos();
    }

    private void DrawBackgroundImage()
    {
        if (string.IsNullOrEmpty(_backgroundImagePath) || !File.Exists(_backgroundImagePath))
            return;

        try
        {
            var drawList = ImGui.GetWindowDrawList();
            var windowPos = ImGui.GetWindowPos();
            var contentRegionMin = ImGui.GetWindowContentRegionMin();
            var contentRegionMax = ImGui.GetWindowContentRegionMax();

            var windowSize = contentRegionMax - contentRegionMin;

            drawList.AddRectFilledMultiColor(
                windowPos + contentRegionMin,
                windowPos + contentRegionMax,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.2f, 0.3f, 0.3f)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.25f, 0.35f, 0.3f)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.15f, 0.25f, 0.3f)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.18f, 0.28f, 0.3f))
            );

            drawList.AddText(
                windowPos + contentRegionMin + new Vector2(10, 10),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 0.5f)),
                "背景图片已设置 (当前版本不支持图片显示)"
            );

            DService.Instance().Log.Warning("[计数器] 当前版本不支持自定义背景图片显示，已使用渐变色替代");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[计数器] 绘制背景失败: {ex.Message}");
        }
    }

    private void DrawWindowControls()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));

        bool locked = _windowLocked;
        if (ImGui.Checkbox($"固定窗口", ref locked))
        {
            _windowLocked = locked;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(_windowLocked ? "✔ 窗口位置已固定，取消勾选可移动" : "勾选后固定窗口位置");
        }

        ImGui.SameLine();

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "|");
        ImGui.SameLine();
        ImGui.Text("背景透明度");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(120);
        if (ImGui.SliderFloat($"##opacity", ref _windowOpacity, 0.3f, 1.0f, $"{_windowOpacity * 100:F0}%"))
        {
            _windowOpacity = Math.Clamp(_windowOpacity, 0.3f, 1.0f);
        }
        
        if (ImGui.IsItemHovered())
        {
            float wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                _windowOpacity += wheel * 0.05f;
                _windowOpacity = Math.Clamp(_windowOpacity, 0.3f, 1.0f);
            }
            
            if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                _windowOpacity = 1.0f;
            }
            
            ImGui.SetTooltip($"滑轮调整透明度 (当前: {_windowOpacity * 100:F0}%)\n右键重置为100%");
        }

        ImGui.SameLine();

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "|");
        ImGui.SameLine();

        ImGui.Text("背景图片");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        ImGui.InputText($"##bgpath", ref _backgroundImagePath, 256);
        ImGui.SameLine();
        if (ImGui.Button("浏览"))
        {
            OpenBackgroundImageDialog();
        }
        if (!string.IsNullOrEmpty(_backgroundImagePath))
        {
            ImGui.SameLine();
            if (ImGui.Button("清除"))
            {
                _backgroundImagePath = string.Empty;
            }
        }

        ImGui.PopStyleVar(2);
    }

    private void OpenBackgroundImageDialog()
    {
        try
        {
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*";
                dialog.Title = "选择背景图片";
                dialog.InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _backgroundImagePath = dialog.FileName;
                    DService.Instance().Log.Information($"[计数器] 背景图片已设置: {_backgroundImagePath}");
                }
            }
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Error($"[计数器] 打开文件选择对话框失败: {ex.Message}");
        }
    }

    private void DrawControls()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 0));

        bool bgChecked = _countInBackground;
        if (ImGui.Checkbox("后台计数", ref bgChecked))
        {
            _countInBackground = bgChecked;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("启用/禁用自动击杀检测");
        }

        ImGui.SameLine();

        bool soundChecked = _soundEnabled;
        if (ImGui.Checkbox("声音提示", ref soundChecked))
        {
            _soundEnabled = soundChecked;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("目标达成时播放提示音");
        }

        ImGui.SameLine();

        if (ImGui.Button("重置"))
        {
            ResetCurrentTerritoryTally();
        }

        ImGui.PopStyleVar();
    }

    private SRankMark? GetCurrentSRankMark()
    {
        var sRankManager = Plugin.P?.SRankDataManager;
        if (sRankManager == null) return null;

        var clientState = DService.Instance().ClientState;
        if (clientState == null) return null;

        return sRankManager.GetSRankMark((ushort)clientState.TerritoryType);
    }

    private void DrawSRankCounter(SRankMark sRankMark)
    {
        // S怪名称（带图标）
        ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), $"⚔️ {sRankMark.Name}");
        ImGui.Separator();
        ImGui.Spacing();

        // 触发条件描述
        ImGui.TextWrapped(sRankMark.TriggerDescription);
        ImGui.Spacing();

        if (sRankMark.TargetMobs.Count > 0)
        {
            if (ImGui.BeginTable("CounterTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("怪物名称", ImGuiTableColumnFlags.None, ImGui.GetWindowWidth() * 0.55f);
                ImGui.TableSetupColumn("计数", ImGuiTableColumnFlags.None, ImGui.GetWindowWidth() * 0.25f);
                ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.None, ImGui.GetWindowWidth() * 0.20f);
                ImGui.TableHeadersRow();

                var tally = GetCurrentTerritoryTally();

                foreach (var mobName in sRankMark.TargetMobs)
                {
                    ImGui.TableNextColumn();

                    // 可点击的怪物名称
                    var selectableLabel = $"{mobName}##selectable_{mobName}";
                    ImGui.Selectable(selectableLabel, false, ImGuiSelectableFlags.SpanAllColumns);

                    // 地图坐标标记功能
                    if (sRankMark.MobLocations != null && sRankMark.MobLocations.TryGetValue(mobName, out var locations) && locations.Count > 0)
                    {
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), $"📍 {mobName} 刷新位置（点击怪物名称循环切换）：");
                            ImGui.Separator();
                            
                            // 显示所有位置
                            for (int i = 0; i < locations.Count; i++)
                            {
                                var loc = locations[i];
                                var bullet = i == (_mobLocationIndex.TryGetValue(mobName, out var idx) ? idx % locations.Count : 0) ? "●" : "○";
                                ImGui.BulletText($"{bullet} 位置{i + 1}: X:{loc.X:F1}, Y:{loc.Y:F1} - {loc.Description}");
                            }
                            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.4f, 1.0f), "提示：点击怪物名称可循环切换位置");
                            ImGui.EndTooltip();
                        }

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            // 获取当前索引（没有则为0）
                            if (!_mobLocationIndex.TryGetValue(mobName, out var currentIndex))
                            {
                                currentIndex = 0;
                            }
                            
                            // 计算下一个索引
                            var nextIndex = (currentIndex + 1) % locations.Count;
                            _mobLocationIndex[mobName] = nextIndex;
                            
                            // 使用当前索引对应的位置
                            var targetLoc = locations[currentIndex];
                            DService.Instance().Log.Information($"[地图标记] 点击了怪物: {mobName}，位置{currentIndex + 1}/{locations.Count}");
                            CreateMapMarker(mobName, new List<MobLocation> { targetLoc });
                        }
                    }

                    ImGui.TableNextColumn();

                    int count = tally.GetValueOrDefault(mobName, 0);
                    int targetCount = GetTargetCount(mobName);

                    // 计数显示（带颜色）
                    if (count >= targetCount)
                    {
                        ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.3f, 1.0f), $"{count}/{targetCount}");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), $"{count}/{targetCount}");
                    }

                    ImGui.TableNextColumn();

                    // 状态指示
                    if (count >= targetCount)
                    {
                        ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.5f, 1.0f), "✅完成");
                    }
                    else if (count >= targetCount * 0.8)
                    {
                        ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), "⏳接近");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "进行中");
                    }
                }

                ImGui.EndTable();
            }
        }

        ImGui.Spacing();
        
        // 进度条显示总进度
        DrawOverallProgress(sRankMark);

        ImGui.Spacing();
        if (sRankMark.CooldownHours != null)
        {
            ImGui.TextDisabled($"⏰ 冷却时间: {sRankMark.CooldownHours.Min}-{sRankMark.CooldownHours.Max} 小时");
        }
    }

    /// <summary>
    /// 绘制总体进度条
    /// </summary>
    private void DrawOverallProgress(SRankMark sRankMark)
    {
        var tally = GetCurrentTerritoryTally();
        
        int totalTarget = 0;
        int totalCurrent = 0;
        
        foreach (var mobName in sRankMark.TargetMobs)
        {
            int target = GetTargetCount(mobName);
            int current = tally.GetValueOrDefault(mobName, 0);
            
            totalTarget += target;
            totalCurrent += Math.Min(current, target); // 不超过目标
        }

        float progress = totalTarget > 0 ? (float)totalCurrent / totalTarget : 0;

        ImGui.Text($"总体进度: {totalCurrent}/{totalTarget} ({progress * 100:F1}%)");
        
        // 进度条
        ImGui.ProgressBar(progress, new Vector2(-1, 0), $"{progress * 100:F0}%");
    }

    /// <summary>
    /// 创建地图坐标标记
    /// </summary>
    private unsafe void CreateMapMarker(string mobName, List<MobLocation> locations)
    {
        DService.Instance().Log.Information($"[地图标记] CreateMapMarker 开始: {mobName}");
        
        uint currentTerritoryType = DService.Instance().ClientState.TerritoryType;
        var sRankData = Plugin.P?.SRankDataManager;
        if (sRankData == null)
        {
            DService.Instance().Log.Error("[地图标记] SRankDataManager 为 null");
            return;
        }

        DService.Instance().Log.Information($"[地图标记] 当前 TerritoryType: {currentTerritoryType}");
        
        // 我们数据文件中的 mapId 实际上是 TerritoryType，所以直接用 currentTerritoryType 查找
        var mapInfo = sRankData.GetTerritory((ushort)currentTerritoryType);
        if (mapInfo == null)
        {
            DService.Instance().Log.Error($"[地图标记] 找不到对应的 Territory 信息，TerritoryType={currentTerritoryType}");
            return;
        }

        DService.Instance().Log.Information($"[地图标记] 找到 Territory: {mapInfo.MapName} (MapId={mapInfo.MapId})");

        var markerManager = Plugin.P?.MapMarkerManager;
        if (markerManager == null)
        {
            DService.Instance().Log.Error("[地图标记] MapMarkerManager 为 null");
            return;
        }

        var firstLoc = locations[0];
        DService.Instance().Log.Information($"[地图标记] 标记坐标: X={firstLoc.X:F1}, Y={firstLoc.Y:F1} - {firstLoc.Description}");
        
        markerManager.CreateCoordinateMarker(
            currentTerritoryType,
            currentTerritoryType,  // 用 TerritoryType 作为 MapId（因为数据中是这样存储的）
            $"{mobName} - {firstLoc.Description}",
            (float)firstLoc.X,
            (float)firstLoc.Y,
            true
        );

        DService.Instance().Log.Information($"[地图标记] 已为 {mobName} 标记坐标: ({firstLoc.X}, {firstLoc.Y})");
    }

    private Dictionary<string, int> GetCurrentTerritoryTally()
    {
        var clientState = DService.Instance().ClientState;
        if (clientState == null) return new Dictionary<string, int>();

        var territoryType = (ushort)clientState.TerritoryType;
        if (!_territoryTallies.TryGetValue(territoryType, out var tally))
        {
            tally = new Dictionary<string, int>();
            _territoryTallies[territoryType] = tally;
        }
        return tally;
    }

    private void IncrementCount(string mobName)
    {
        var tally = GetCurrentTerritoryTally();
        if (!tally.ContainsKey(mobName))
        {
            tally[mobName] = 0;
        }
        tally[mobName]++;
    }

    private void ResetCurrentTerritoryTally()
    {
        var clientState = DService.Instance().ClientState;
        if (clientState == null) return;

        var territoryType = (ushort)clientState.TerritoryType;
        if (_territoryTallies.ContainsKey(territoryType))
        {
            _territoryTallies[territoryType].Clear();
        }
        
        // 清除所有追踪状态
        _trackedMobs.Clear();
        _completedMobs.Clear();
        _processedKillEvents.Clear(); // 清除已处理的击杀事件（允许重新计数）
        
        DService.Instance().Log.Information("[计数器] 计数器和所有追踪状态已重置");
    }

    private void DrawBackgroundToggleButton()
    {
        bool isChecked = _countInBackground;
        if (ImGui.Checkbox("后台计数", ref isChecked))
        {
            _countInBackground = isChecked;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("启用/禁用自动击杀检测");
        }
    }

    private void ClientState_TerritoryChanged(uint territoryType)
    {
        _currentTerritoryType = (ushort)territoryType;
        
        // 地图切换时清除所有缓存和追踪状态（但不清除计数）
        _trackedMobs.Clear();
        _completedMobs.Clear();
        _processedKillEvents.Clear();
        
        // 清除目标数量缓存，确保新地图的数据被重新解析
        _cachedTargetCounts = null;
        _cachedTriggerDescription = null;
        
        DService.Instance().Log.Information($"[计数器] 地图切换至 {territoryType}，已重置所有追踪状态和缓存");
    }

    public void Cleanup()
    {
        var clientState = DService.Instance().ClientState;
        if (clientState != null)
        {
            clientState.TerritoryChanged -= ClientState_TerritoryChanged;
        }

        // 取消订阅Framework.Update事件
        try
        {
            DService.Instance().Framework.Update -= OnFrameworkUpdate;
            DService.Instance().Log.Information("[计数器] Framework.Update已移除");
        }
        catch (Exception ex)
        {
            DService.Instance().Log.Warning($"[计数器] 移除Framework.Update失败: {ex.Message}");
        }
        
        _trackedMobs.Clear();
        _completedMobs.Clear();
    }
}
