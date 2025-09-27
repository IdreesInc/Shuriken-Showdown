using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Base counter class for counter UI components
/// </summary>
public abstract class Counter : UdonSharpBehaviour
{

    // Needs to be wired up to the Label Text component in the Counter UI
    public TMPro.TextMeshProUGUI counterText;

    // Needs to be wired up to Plus Button click event
    public abstract void OnIncrement();

    // Needs to be wired up to Minus Button click event
    public abstract void OnDecrement();

    protected void Log(string message)
    {
        Shared.Log("Counter", message);
    }
    protected void LogError(string message)
    {
        Shared.LogError("Counter", message);
    }
}
