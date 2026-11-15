using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;


public class UI_PlayerName : MonoBehaviour
{
    [SerializeField] private PanelRenderer panelRenderer;
    private Label label;
    private string lastSetName;
    private void OnEnable()
    {
        panelRenderer.RegisterUIReloadCallback(Reload);
    }
    private void Reload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
    {
        label = rootElement.Q<Label>("name");
        if (label != null)
            label.text = lastSetName;
    }

    public void SetName(string Name)
    {
        lastSetName = Name;
        if(label != null)
            label.text = Name;
    }

    private void OnDisable()
    {
        panelRenderer.UnregisterUIReloadCallback(Reload);
    }
}