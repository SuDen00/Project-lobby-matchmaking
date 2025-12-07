using Unity.VisualScripting;
using UnityEngine;

public class OfflineTest : MonoBehaviour
{
    [SerializeField] private bool isOfflineScene;
    [SerializeField] private GameObject root;

    private void Start()
    {
        if (isOfflineScene)
        {
            root.SetActive(true);
        }
        else
        {
            root.SetActive(false);
        }
    }
}