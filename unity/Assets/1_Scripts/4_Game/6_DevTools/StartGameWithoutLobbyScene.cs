using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class StartGameWithoutLobbyScene : NetworkBehaviour
{
    [SerializeField] private GameObject scriptsRoot;
    [SerializeField] private Button startButton;

    private bool buttonActivated = false;

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonPressed);
        startButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!buttonActivated && isServer)
        {
            startButton.gameObject.SetActive(true);
            buttonActivated = true;
        }

        if (Input.GetKeyUp(KeyCode.L) && isServer)
        {
            OnStartButtonPressed();
        }
    }

    private void OnStartButtonPressed()
    {
        if (!isServer) return;

        scriptsRoot.SetActive(true);
        Destroy(startButton.gameObject);

        RpcStartGame();
    }

    [ClientRpc]
    private void RpcStartGame()
    {
        if (isServer) return;  
        scriptsRoot.SetActive(true);
    }
}
