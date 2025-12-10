using UnityEngine;

[CreateAssetMenu(fileName = "NewMissionItem", menuName = "Mission Item")]
public class MissionItemSO : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite; 
    public int puzzleLayerIndex; 
}