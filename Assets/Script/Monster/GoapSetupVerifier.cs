using CrashKonijn.Goap.Runtime;
using UnityEngine;

public class GoapSetupVerifier : MonoBehaviour
{
    private GoapActionProvider provider;
    
    private void Start()
    {
        provider = GetComponent<GoapActionProvider>();
        
        if (provider == null)
        {
            Debug.LogError("[Verifier] NO GoapActionProvider found on this GameObject!");
            return;
        }
        
        Debug.Log($"[Verifier] GoapActionProvider found");
        Debug.Log($"[Verifier] Agent Type: {provider.AgentType?.Id ?? "NULL"}");
        Debug.Log($"[Verifier] Agent Type Behaviour: {provider.AgentTypeBehaviour?.name ?? "NULL"}");
        
        // Wait a frame for everything to initialize
        Invoke(nameof(VerifyAfterInit), 1f);
    }
    
    private void VerifyAfterInit()
    {
        Debug.Log("=== GOAP SETUP VERIFICATION ===");
        
        if (provider.AgentType == null)
        {
            Debug.LogError("[Verifier] AgentType is NULL! The monster won't work!");
            Debug.LogError("[Verifier] Make sure you have a GoapBehaviour in the scene with MonsterAgentTypeFactory!");
            return;
        }
        
        var agentType = provider.AgentType;
        Debug.Log($"[Verifier] Agent Type ID: {agentType.Id}");
        Debug.Log($"[Verifier] SUCCESS! Agent Type is assigned!");
        
        Debug.Log("=== END VERIFICATION ===");
    }
}