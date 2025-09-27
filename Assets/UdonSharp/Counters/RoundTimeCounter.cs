
using UdonSharp;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class RoundTimeCounter : Counter
{

    void Start()
    {
        UpdateCounterText();
    }

    private void UpdateCounterText()
    {
        counterText.text = GameLogic.Get().GetRoundTimeLimit() + " SEC";
    }

    public override void OnIncrement()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Not the instance owner, cannot increment round time limit");
            return;
        }
        GameLogic.Get().ModifyRoundTimeLimit(10);
        UpdateCounterText();
    }

    public override void OnDecrement()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Not the instance owner, cannot decrement round time limit");
            return;
        }
        GameLogic.Get().ModifyRoundTimeLimit(-10);
        UpdateCounterText();
    }
}
    