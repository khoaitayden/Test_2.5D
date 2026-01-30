using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class DrunkMonsterBrain : MonsterBrain
{
    protected override string GetAgentTypeName()
    {
        return "DrunkMonsterAgent";
    }

    protected override void RequestInitialGoal()
    {
        DecideGoal();
    }

    private void DecideGoal()
    {
        if (IsFleeing)
        {
            provider.RequestGoal<FleeGoal>();
        }
        else
        {
            provider.RequestGoal<KillPlayerGoal>();
        }
    }

    protected override void UpdateGOAPState()
    {
        base.UpdateGOAPState(); 
        DecideGoal();           
    }
}