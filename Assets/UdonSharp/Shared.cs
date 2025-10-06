using UnityEngine;
using VRC.SDKBase;

public static class Shared
{

    private static Color HexToColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        if (hex.Length != 6 && hex.Length != 8)
        {
            Debug.LogError("Invalid hex color: " + hex);
            return new Color32(0, 0, 0, 255);
        }

        float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        float a = 255;
        if (hex.Length == 8)
        {
            a = int.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    public static string[] ColorStrings()
    {
        // Hex strings for colors, useful for rich text in UI
        return new string[] {
            "#ff4c4c",
            "#6699ff",
            "#6fe35b",
            "#ffcf3f",
            "#d580ff",
            "#ff9966",
            "#66ffff",
            "#ff80bf",
        };
    }
    public static Color[] Colors()
    {
        // Colors become desaturated when applied to UI elements
        // Notably, this works well with shurikens because we want those to be more saturated
        Color[] colors = new Color[ColorStrings().Length];
        for (int i = 0; i < ColorStrings().Length; i++)
        {
            colors[i] = HexToColor(ColorStrings()[i]);
        }
        return colors;
    }

    public static Color[] LightenedColors()
    {
        Color[] colors = Colors();
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.Lerp(colors[i], Color.white, 0.3f);
        }
        return colors;
    }

    public static Color[] DarkenedColors()
    {
        Color[] colors = Colors();
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.Lerp(colors[i], Color.black, 0.3f);
        }
        return colors;
    }

    public static string[] DarkenedColorStrings()
    {
        Color[] darkenedColors = DarkenedColors();
        string[] hexStrings = new string[darkenedColors.Length];
        for (int i = 0; i < darkenedColors.Length; i++)
        {
            Color32 c = darkenedColors[i];
            hexStrings[i] = $"#{c.r:X2}{c.g:X2}{c.b:X2}";
        }
        return hexStrings;
    }

    public static void Log(string prefix, string message, VRCPlayerApi player = null)
    {
        string logPrefix = GetLogStart(prefix, player);
        Debug.Log($"{logPrefix} <color=white>{message}</color>");
        Debug.Log(GameLogic.Get());
    }

    public static void LogError(string prefix, string message, VRCPlayerApi player = null)
    {
        string logPrefix = GetLogStart(prefix, player);
        Debug.LogError($"{logPrefix} <color=white>{message}</color>");
    }

    public static int MaxPlayers()
    {
        return 8;
    }

    private static string GetLogStart(string prefix, VRCPlayerApi player = null)
    {
        string name = "";
        int slot = -1;
        if (player != null)
        {
            name = " " + (string.IsNullOrEmpty(player.displayName) ? "Unnamed Player" : player.displayName);
            slot = GameLogic.Get().GetPlayerSlot(player.playerId);
        }
        string color = "#ff00ce";
        if (slot != -1)
        {
            color = Shared.ColorStrings()[slot];
        }
        return $"<color={color}>[{prefix}{name}]:</color>";
    }
}