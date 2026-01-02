using UnityEngine;

// Add 'TreeBranch' to the list
public enum SurfaceType 
{ 
    Dirt, 
    Grass, 
    Wood,       // Generic wood (Planks, Floors, Bridges)
    TreeBranch, // Sticks, Small branches (Snap sounds)
    Stone, 
    Log 
}

public class SurfaceIdentifier : MonoBehaviour
{
    public SurfaceType type;
}