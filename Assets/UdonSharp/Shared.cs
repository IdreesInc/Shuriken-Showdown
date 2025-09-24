using UnityEngine;

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
            "#7dff66",
            "#ffd966",
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

    public static int MaxPlayers()
    {
        return 8;
    }
}