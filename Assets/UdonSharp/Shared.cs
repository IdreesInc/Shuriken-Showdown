using UnityEngine;

public static class Shared {

    private static Color hexToColor(string hex) {
        if (hex.StartsWith("#")) {
            hex = hex.Substring(1);
        }

        if (hex.Length != 6 && hex.Length != 8) {
            Debug.LogError("Invalid hex color: " + hex);
            return new Color32(0, 0, 0, 255);
        }

        float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        float a = 255;
        if (hex.Length == 8) {
            a = int.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
    public static Color[] Colors() {
        // TODO: Hardcode these colors when finalized
        Color[] colors = {
            hexToColor("#FF6B6B"),
            hexToColor("#97B9EC"),
            hexToColor("#95CF81"),
            hexToColor("#FFD87D"),
            hexToColor("#C79FE7"),
            hexToColor("#FBAC7F"),
            hexToColor("#90E3E9"),
            hexToColor("#FEA3C4"),
        };
        return colors;
    }

    public static Color[] ShurikenColors() {
        Color[] colors = {
            Color.grey,
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.black, // TODO: Fix
            Color.black, // TODO: Fix
            Color.cyan,
            Color.magenta
        };
        return colors;
    }

    public static int MaxPlayers() {
        return 8;
    }
}