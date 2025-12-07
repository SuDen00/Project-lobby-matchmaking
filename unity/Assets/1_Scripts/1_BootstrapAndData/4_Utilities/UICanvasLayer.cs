using UnityEngine;

public enum UILayer
{
    Base    = 0,    // обычный интерфейс
    Overlay = 200,  // хедеры/оверлеи
    Popup   = 400,  // модальные окна/ошибки
    Loading = 800,  // экран загрузки (выше попапов)
    Debug   = 1200  // отладочная консоль и т.п.
}

[RequireComponent(typeof(Canvas))]
public sealed class UICanvasLayer : MonoBehaviour
{
    [SerializeField] private UILayer layer = UILayer.Base;
    [SerializeField] private int orderOffset; // если нужно подвинуть на пару уровней

    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = (int)layer + orderOffset;
    }

    public void SetLayer(UILayer l, int offset = 0)
    {
        var canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = (int)l + offset;
    }
}
