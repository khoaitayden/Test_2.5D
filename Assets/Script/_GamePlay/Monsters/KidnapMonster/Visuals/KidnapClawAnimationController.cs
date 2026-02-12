using UnityEngine;
public class KidnapClawAnimationController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("IK Settings")]
    [SerializeField] private Transform ikTarget; 
    [SerializeField] private Transform monsterChest; 
    [Tooltip("How fast the IK target snaps to the player")]
    [SerializeField] private float ikFollowSpeed = 10f;

    private Transform _playerTarget;
    private int animGrabHash;
    private float currentBlendValue;
    private Vector3 _restPositionLocal;

    void Awake()
    {
        animGrabHash = Animator.StringToHash("GrabBlend");
        
        if (ikTarget != null && monsterChest != null)
        {
            _restPositionLocal = monsterChest.InverseTransformPoint(ikTarget.position);
        }
    }

    public void UpdateClawBlend(float value, Transform player)
    {
        currentBlendValue = value;
        Debug.Log(value);
        _playerTarget = player; 

        if (animator != null)
        {
            animator.SetFloat(animGrabHash, value);
        }
    }

    void LateUpdate()
    {
        if (ikTarget == null || monsterChest == null) return;

        Vector3 desiredPos;

        if (currentBlendValue > 0.1f && _playerTarget != null)
        {
            Vector3 playerAimPos = _playerTarget.position + Vector3.up * 1.2f;

            Vector3 worldRestPos = monsterChest.TransformPoint(_restPositionLocal);
            desiredPos = Vector3.Lerp(worldRestPos, playerAimPos, currentBlendValue);
        }
        else
        {
            desiredPos = monsterChest.TransformPoint(_restPositionLocal);
        }

        ikTarget.position = Vector3.Lerp(ikTarget.position, desiredPos, Time.deltaTime * ikFollowSpeed);
    }
}