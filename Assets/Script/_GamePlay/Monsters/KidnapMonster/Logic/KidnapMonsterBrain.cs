using CrashKonijn.Goap.MonsterGen;
using UnityEngine;

public class KidnapMonsterBrain : MonsterBrain
{
    protected override string GetAgentTypeName()
    {
        return "KidnapMonsterAgent";
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
            provider.RequestGoal<KidnapGoal>();
        }
    }

    protected override void UpdateGOAPState()
    {
        base.UpdateGOAPState();
        DecideGoal();
    }
}