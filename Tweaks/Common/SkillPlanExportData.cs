using System.Collections.Generic;

namespace Tweaks.Common;

public class SkillPlanItem
{
    public short Id { get; set; }
    public string? Name { get; set; }
    public bool Compacted { get; set; }
}

public class SkillPlanExportData
{
    public int Version { get; set; } = 1;
    public string? Name { get; set; }
    public string? Date { get; set; }
    public int CharId { get; set; }
    public string? CharName { get; set; }
    public int[]? GenericGridAllocation { get; set; }
    public List<SkillPlanItem> Neigong { get; set; } = new();
    public List<SkillPlanItem> Attack { get; set; } = new();
    public List<SkillPlanItem> Agility { get; set; } = new();
    public List<SkillPlanItem> Defense { get; set; } = new();
    public List<SkillPlanItem> Assistance { get; set; } = new();
}
