using System.Collections.Generic;

namespace EliteHunt;

public static class Constants
{
    // 简化的怪物名称配置 - 中文（参考HuntHelper）
    public static readonly Dictionary<string, string[]> HuntMonsters = new()
    {
        { "Ruinator", new[] { "思考之物", "彷徨之物", "叹息之物" } },    // Mare Lamentorum
        { "Sphatika", new[] { "阿输陀花", "毕舍遮", "金刚尾" } },          // Thavnair
        { "Ixtab", new[] { "破裂的隆卡人偶", "破裂的隆卡石蒺藜", "破裂的隆卡器皿" } }, // The Tempest
        { "ForgivenPedantry", new[] { "矮人棉" } },                      // Labyrinthos
        { "SaltAndLight", new[] { "舍弃" } },                              // Garlemald
        { "Udumbara", new[] { "莱西", "狄亚卡" } },                        // Amh Araeng
        { "Okina", new[] { "无壳观梦螺", "观梦螺" } },                      // Kholusia
        { "Gandawera", new[] { "皇金矿", "星极花" } },                    // The Ruby Sea
        { "Leucrotta", new[] { "亚拉戈奇美拉", "小海德拉", "美拉西迪亚薇薇尔飞龙" } }, // The Azim Steppe
        { "Squonk", new[] { "唧唧咋咋" } },                               // The Sea of Clouds
        { "Minhocao", new[] { "土元精" } }                                 // Southern Thanalan
    };

    // 简化的正则表达式模式 - 中文
    public static readonly Dictionary<string, string> RegexPatterns = new()
    {
        { "Ruinator", "(?i)(思考之物|彷徨之物|叹息之物)打倒了" },
        { "Sphatika", "(?i)(阿输陀花|毕舍遮|金刚尾)打倒了" },
        { "Ixtab", "(?i)(破裂的隆卡人偶|破裂的隆卡石蒺藜|破裂的隆卡器皿)打倒了" },
        { "ForgivenPedantry", "(?i)获得了.*矮人棉" },
        { "SaltAndLight", "(?i)舍弃了.*" },
        { "Udumbara", "(?i)(莱西|狄亚卡)打倒了" },
        { "Okina", "(?i)(无壳观梦螺|观梦螺)打倒了" },
        { "Gandawera", "(?i)获得了.*(皇金矿|星极花)" },
        { "Leucrotta", "(?i)(亚拉戈奇美拉|小海德拉|美拉西迪亚薇薇尔飞龙)打倒了" },
        { "Squonk", "(?i)斯奎克发动了.*唧唧咋咋" },
        { "Minhocao", "(?i)土元精打倒了" }
    };

    // 地图ID映射（参考HuntHelper）
    public enum MapID : ushort
    {
        Unknown = 0,
        
        // 晓月 (Endwalker)
        Labyrinthos = 956,
        Thavnair = 957,
        Garlemald = 958,
        MareLamentorum = 959,
        Elpis = 961,
        UltimaThule = 960,
        
        // 暗影 (Shadowbringers)
        Lakeland = 813,
        Kholusia = 814,
        AmhAraeng = 815,
        IlMheg = 816,
        TheRaktikaGreatwood = 817,
        TheTempest = 818,
        
        // 红莲 (Stormblood)
        TheFringes = 612,
        TheRubySea = 613,
        Yanxia = 614,
        ThePeaks = 620,
        TheLochs = 621,
        TheAzimSteppe = 622,
        
        // 苍穹 (Heavensward)
        CoerthasWesternHighlands = 397,
        TheDravanianForelands = 398,
        TheDravanianHinterlands = 399,
        TheChurningMists = 400,
        TheSeaofClouds = 401,
        AzysLla = 402,
        
        // 新生 (A Realm Reborn)
        MiddleLaNoscea = 134,
        LowerLaNoscea = 135,
        EasternLaNoscea = 137,
        WesternLaNoscea = 138,
        UpperLaNoscea = 139,
        WesternThanalan = 140,
        CentralThanalan = 141,
        EasternThanalan = 145,
        SouthernThanalan = 146,
        NorthernThanalan = 147,
        CentralShroud = 148,
        EastShroud = 152,
        SouthShroud = 153,
        NorthShroud = 154,
        CoerthasCentralHighlands = 155,
        MorDhona = 156,
        OuterLaNoscea = 180
    }
}
