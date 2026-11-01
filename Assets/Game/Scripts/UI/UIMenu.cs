using TriInspector;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class UIMenu : MonoBehaviour
{
    [SerializeField, RequiredGet(IncludeSelf = true)] private PanelRenderer doc;
    [SerializeField] private UISettings settings;
    [SerializeField] private UIPlayMenu menu;
    private VisualElement ui;

    private Button playButton;
    private Button settingsButton;
    private Button quitButton;

    private void OnEnable()
    {
        doc.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        doc.UnregisterUIReloadCallback(OnUIReload);
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement rootElement, int v)
    {
        ui = rootElement;
        playButton = ui.Q<Button>("Play");
        settingsButton = ui.Q<Button>("Settings");
        quitButton = ui.Q<Button>("Quit");

        playButton.clicked += OnPlay;
        settingsButton.clicked += OnSettings;
        quitButton.clicked += OnQuit;
    }

    private void OnDestroy()
    {
        if(playButton != null) playButton.clicked -= OnPlay;
        if (settingsButton != null) settingsButton.clicked -= OnSettings;
        if (quitButton != null) quitButton.clicked -= OnQuit;
    }

    private void OnPlay()
    {
        menu.SetEnable(true);
    }

    private void OnSettings()
    {
        settings.SetEnable(true);
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
