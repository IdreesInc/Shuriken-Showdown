
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class KillsToWinCounter : Counter
{

    void Start()
    {
        UpdateCounterText();
    }

    private void UpdateCounterText()
    {
        counterText.text = GameLogic.Get().GetKillsToWin() + " KILLS";
    }

    public override void OnIncrement()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Not the instance owner, cannot increment kills to win");
            return;
        }
        GameLogic.Get().ModifyKillsToWin(1);
        UpdateCounterText();
    }

    public override void OnDecrement()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Not the instance owner, cannot decrement kills to win");
            return;
        }
        GameLogic.Get().ModifyKillsToWin(-1);
        UpdateCounterText();
    }
}
