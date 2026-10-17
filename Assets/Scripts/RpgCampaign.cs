using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RpgAreaProgress
{
    public int area, quest, bandits;
    public bool boss;
    public List<string> defeated = new List<string>();
}

public static class RpgCampaign
{
    public static readonly string[] Names = { "Havranov • náměstí", "Severní hlídka", "Cesta k mostu", "Poslední obrana" };
    public static readonly Vector2[] Starts = { Vector2.zero, new Vector2(0, 6), new Vector2(12, 3.5f), new Vector2(-6, 3) };
}
