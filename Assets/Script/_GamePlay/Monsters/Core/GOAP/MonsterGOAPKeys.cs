using CrashKonijn.Goap.Runtime;

namespace CrashKonijn.Goap.MonsterGen
{
    // --- WORLD KEYS (States) ---
    public class IsPlayerInSight : WorldKeyBase {}
    public class IsPlayerReachable : WorldKeyBase {}
    public class IsInvestigating : WorldKeyBase {}
    public class IsFleeing : WorldKeyBase {}
    public class IsPatrol : WorldKeyBase {}
    public class CanPatrol : WorldKeyBase {}
    public class HasKilledPlayer : WorldKeyBase {}
    public class HasSuspiciousLocation : WorldKeyBase {}
    public class IsAtSuspiciousLocation : WorldKeyBase {}
    public class IsTrackingTrace : WorldKeyBase {}
    public class HasInvestigated : WorldKeyBase {}
    public class HasKidnappedPlayer : WorldKeyBase {}
    public class IsLitByFlashlight : WorldKeyBase { }
    public class IsHiding : WorldKeyBase { }
    public class IsSafe : WorldKeyBase { }
    public class CanHide : WorldKeyBase { }

    

    // --- TARGET KEYS ---
    public class PlayerTarget : TargetKeyBase {}
    public class PlayerLastSeenTarget : TargetKeyBase {}
    public class PatrolTarget : TargetKeyBase {}
    public class InvestigateTarget : TargetKeyBase {}
    public class FreshTraceTarget : TargetKeyBase {}
    public class HideTarget : TargetKeyBase {} 
    public class WaitTarget : TargetKeyBase {} 

}