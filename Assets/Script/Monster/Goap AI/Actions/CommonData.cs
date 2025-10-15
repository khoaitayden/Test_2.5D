using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;

public class CommonData : IActionData
{
    public ITarget Target { get; set; }
    public float Timer { get; set; }
}