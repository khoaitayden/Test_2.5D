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
        provider.RequestGoal<KillPlayerGoal>();
    }
}