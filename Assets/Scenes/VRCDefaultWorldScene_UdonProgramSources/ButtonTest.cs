
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ButtonTest : UdonSharpBehaviour
{
    void Start()
    {
        
    }

    public override void Interact()
    {
        Debug.Log("Object interacted!");
    }
}
