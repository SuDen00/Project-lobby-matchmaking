using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PanelItem
{
    public GameObject panel;
}

public class PanelManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels;
    [SerializeField] private GameObject defaultPanel;

    private GameObject current;       
    
    private void Awake()
    {
        foreach (var p in panels)
            if (p != null) p.SetActive(false);

        if (defaultPanel != null)
        {
            current = defaultPanel;
            current.SetActive(true);
        }
    }

    public void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow == null) return;
        if (panelToShow == current) return; 

        if (current != null)
            current.SetActive(false);

        current = panelToShow;
        current.SetActive(true);
    }
}
