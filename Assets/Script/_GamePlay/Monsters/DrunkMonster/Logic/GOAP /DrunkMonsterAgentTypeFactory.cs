using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
namespace CrashKonijn.Goap.MonsterGen.AgentTypes
{
public class DrunkMonsterAgentTypeFactory : AgentTypeFactoryBase
{
    public override IAgentTypeConfig Create()
    {
    var factory = new AgentTypeBuilder("DrunkMonsterAgent");

            factory.AddCapability<DrunkMonsterCapabilityFactory>();

            return factory.Build();
        }
    }
}