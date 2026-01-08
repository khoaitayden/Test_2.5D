using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.MonsterGen.Capabilities;
namespace CrashKonijn.Goap.MonsterGen.AgentTypes
{
public class MonsterAgentTypeFactory : AgentTypeFactoryBase
{
    public override IAgentTypeConfig Create()
    {
    var factory = new AgentTypeBuilder("ScriptMonsterAgent");

    // CORRECTED: We now add the single, more descriptive MonsterCapabilityFactory
            factory.AddCapability<MonsterCapabilityFactory>();

            return factory.Build();
        }
    }
}