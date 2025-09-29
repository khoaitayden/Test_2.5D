using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    [SerializeField] private GameObject dirtTrail;

    public void ToggleDirtTrail(bool isOn)
    {
        if (dirtTrail != null)
        {
            dirtTrail.SetActive(isOn);
        }
    }
    
}
