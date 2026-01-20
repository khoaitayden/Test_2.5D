using UnityEngine;

public class MonsterTouchSensor : MonoBehaviour
{
    public bool IsTouchingPlayer { get; private set; }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IsTouchingPlayer = true;
            Debug.Log("Monster started touching Player!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IsTouchingPlayer = false;
            Debug.Log("Monster stopped touching Player!");
        }
    }
}