using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EliteHunt.Data;

public class SRankData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = "";

    [JsonPropertyName("source")]
    public List<string> Source { get; set; } = new();

    [JsonPropertyName("generalNotes")]
    public List<string> GeneralNotes { get; set; } = new();

    [JsonPropertyName("maps")]
    public List<MapVersion> Maps { get; set; } = new();
}

public class MapVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("territories")]
    public List<Territory> Territories { get; set; } = new();
}

public class Territory
{
    [JsonPropertyName("mapId")]
    public ushort MapId { get; set; }

    [JsonPropertyName("mapName")]
    public string MapName { get; set; } = "";

    [JsonPropertyName("territoryName")]
    public string TerritoryName { get; set; } = "";

    [JsonPropertyName("isCity")]
    public bool IsCity { get; set; }

    [JsonPropertyName("sRankMark")]
    public SRankMark? SRankMark { get; set; }
}

public class SRankMark
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("englishName")]
    public string EnglishName { get; set; } = "";

    [JsonPropertyName("triggerType")]
    public string TriggerType { get; set; } = "";

    [JsonPropertyName("triggerDescription")]
    public string TriggerDescription { get; set; } = "";

    [JsonPropertyName("targetMobs")]
    public List<string> TargetMobs { get; set; } = new();

    [JsonPropertyName("killCount")]
    public Dictionary<string, int>? KillCount { get; set; }

    [JsonPropertyName("gatherCount")]
    public Dictionary<string, int>? GatherCount { get; set; }

    [JsonPropertyName("regexPatterns")]
    public Dictionary<string, string>? RegexPatterns { get; set; }

    [JsonPropertyName("timeCondition")]
    public string? TimeCondition { get; set; }

    [JsonPropertyName("weatherCondition")]
    public string? WeatherCondition { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("requires")]
    public List<string>? Requires { get; set; }

    [JsonPropertyName("probability")]
    public bool Probability { get; set; }

    [JsonPropertyName("isWeatherTrigger")]
    public bool IsWeatherTrigger { get; set; }

    [JsonPropertyName("cooldownHours")]
    public CooldownHours? CooldownHours { get; set; }

    [JsonPropertyName("mobLocations")]
    public Dictionary<string, List<MobLocation>>? MobLocations { get; set; }
}

public class MobLocation
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}

public class CooldownHours
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }
}
