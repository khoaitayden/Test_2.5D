using CrashKonijn.Docs.GettingStarted.Capabilities;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Docs.GettingStarted.AgentTypes
{
    public class MonsterAgentTypeFactory : AgentTypeFactoryBase
    {
        public override IAgentTypeConfig Create()
        {
            var factory = new AgentTypeBuilder("ScriptMonsterAgent");
            
            factory.AddCapability<PatrolCapabilityFactory>();

            return factory.Build();
        }
    }
}