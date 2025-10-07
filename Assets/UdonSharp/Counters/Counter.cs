using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Base counter class for counter UI components
/// </summary>
public abstract class Counter : UdonSharpBehaviour
{
    protected bool ownerOnly = false;

    // Needs to be wired up to the Label Text component in the Counter UI
    public TMPro.TextMeshProUGUI counterText;
    protected Button leftButton;
    protected Button rightButton;

    void Update()
    {
        if (leftButton == null || rightButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>();
            if (buttons.Length != 2)
            {
                LogError("Counter does not have exactly 2 buttons");
                return;
            }
            leftButton = buttons[0];
            rightButton = buttons[1];
        }

        if (ownerOnly)
        {
            bool isOwner = Networking.IsOwner(gameObject);
            leftButton.interactable = isOwner;
            rightButton.interactable = isOwner;
        }
        else
        {
            leftButton.interactable = true;
            rightButton.interactable = true;
        }
    }

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
