using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace CrashKonijn.Goap.MonsterGen
{
    public class CanHideSensor : LocalWorldSensorBase
    {
        private KidnapHideFinder finder;
        private KidnapMonsterBrain kidnapBrain;
        private KidnapMonsterConfig kidnapConfig;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            if (finder == null) finder = references.GetCachedComponent<KidnapHideFinder>();
            if (kidnapBrain == null) kidnapBrain = references.GetCachedComponent<MonsterBrain>() as KidnapMonsterBrain ;
            
            if (kidnapConfig == null) kidnapConfig = references.GetCachedComponent<MonsterConfigBase>() as KidnapMonsterConfig;

            if (kidnapBrain != null && kidnapBrain.PlayerAnchor != null && kidnapBrain.PlayerAnchor.Value != null)
            {
                if (kidnapConfig != null)
                {
                    float dist = Vector3.Distance(agent.Transform.position, kidnapBrain.PlayerAnchor.Value.position);
                    if (dist < kidnapConfig.playerComeCloseFleeDistance)
                    {
                        Debug.Log("Player is to close to hide.");
                        return 0;
                    }
                }
                if (kidnapBrain.CanHide == false)
                {
                    Debug.Log("Brain says can't hide now.");
                    return 0;
                }

                // 2. Availability Check
                if (finder != null)
                {
                    finder.SetPlayer(kidnapBrain.PlayerAnchor.Value);
                    return finder.FindBestHideSpot().HasValue ? 1 : 0;
                }
            }
            
            return 0;
        }
    }
}