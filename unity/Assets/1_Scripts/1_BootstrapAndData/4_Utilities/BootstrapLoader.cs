using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootstrapLoader : MonoBehaviour
{
    [Scene, SerializeField] private string firstSceneToLoad;

    private void Start() => StartCoroutine(Loader());

    private IEnumerator Loader()
    {
        yield return new WaitForSecondsRealtime(2);
        SceneManager.LoadScene(firstSceneToLoad);
    } 
}
