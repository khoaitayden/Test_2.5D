using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
namespace CrashKonijn.Goap.MonsterGen.AgentTypes
{
    public class KidnapMonsterAgentTypeFactory : AgentTypeFactoryBase
    {
        public override IAgentTypeConfig Create()
        {
            var factory = new AgentTypeBuilder("KidnapMonsterAgent");
            factory.AddCapability<KidnapMonsterCapabilityFactory>();
            return factory.Build();
        }
    }
}