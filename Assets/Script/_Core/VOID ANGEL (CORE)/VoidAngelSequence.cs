using UnityEngine;
using Unity.Cinemachine; 

public class VoidAngelSequence : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 4.0f;
    [SerializeField] private float startDistance = 30f;
    [SerializeField] private float endDistance = 1.0f;
    

    private Transform targetPlayer;
    private Camera mainCamera;
    private CinemachineBrain cBrain;
    
    private float timer;
    private bool isRunning = false;

    private Vector3 relativeSpawnDirection;

    public void StartSequence(Transform player, Camera cam)
    {
        targetPlayer = player;
        mainCamera = cam;

        cBrain = mainCamera.GetComponent<CinemachineBrain>();
        if (cBrain != null) cBrain.enabled = false;

        Vector3 camFwd = mainCamera.transform.forward;
        camFwd.y = -0.5f; // Bias downwards
        relativeSpawnDirection = camFwd.normalized;

        gameObject.SetActive(true);
        timer = 0f;
        isRunning = true;
    }

    void LateUpdate()
    {
        if (!isRunning || targetPlayer == null) return;

        timer += Time.deltaTime;
        float percent = Mathf.Clamp01(timer / duration);


        float currentDist = Mathf.Lerp(startDistance, endDistance, percent * percent); 

        transform.position = mainCamera.transform.position + (relativeSpawnDirection * currentDist);

        transform.LookAt(mainCamera.transform);

        Quaternion neededRotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, neededRotation, Time.deltaTime);


        if (percent >= 1.0f)
        {
            Shutdown();
        }
    }

    private void Shutdown()
    {
        Debug.Log("VOID DEATH.");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}