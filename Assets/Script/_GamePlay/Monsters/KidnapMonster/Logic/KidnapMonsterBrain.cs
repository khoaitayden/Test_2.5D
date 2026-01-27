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
        // Kidnap Monster wants to Kidnap
        provider.RequestGoal<KidnapGoal>();
    }
}