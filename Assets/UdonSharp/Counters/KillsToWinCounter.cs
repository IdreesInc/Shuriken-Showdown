
using UdonSharp;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class KillsToWinCounter : Counter
{

    void Start()
    {
        ownerOnly = true;
        UpdateCounterText();
    }

    private void UpdateCounterText()
    {
        counterText.text = GameLogic.Get().GetMaxScore() + " KILLS";
    }

    public override void OnIncrement()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Not the instance owner, cannot increment kills to win");
            return;
        }
        GameLogic.Get().ModifyMaxScore(1);
        UpdateCounterText();
    }

    public override void OnDecrement()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Not the instance owner, cannot decrement kills to win");
            return;
        }
        GameLogic.Get().ModifyMaxScore(-1);
        UpdateCounterText();
    }
}
