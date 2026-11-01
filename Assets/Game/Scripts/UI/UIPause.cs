using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public partial class UIPause : MonoBehaviour
{
    [SerializeField, RequiredGet(IncludeSelf = true)] private UIDocument doc;
    private VisualElement ui;
    [SerializeReference, TableList] private List<UIPart> parts = new ()
    {
        new PauseMenu(),
    };

    private void Awake()
    {
        ui = doc.rootVisualElement;

        foreach (var part in parts)
            part.OnInit(this);
    }

    private void OnDestroy()
    {
        foreach (var part in parts)
            part.OnShutdown(this);
    }

    private interface UIPart
    {
        void OnInit(UIPause massage);
        void OnShutdown(UIPause massage);
    }

    [Serializable]
    private class PauseMenu : UIPart
    {
        [SerializeField] private InputActionAsset action;
        private Button pauseButton;
        private Button continueButton;
        private Button backToMenuButton;
        private VisualElement menuRoot;

        public void OnInit(UIPause massage)
        {
            var ui = massage.ui;
            pauseButton = ui.Q<Button>("pause");
            continueButton = ui.Q<Button>("Continue");
            backToMenuButton = ui.Q<Button>("BackToMenu");

            menuRoot = ui.Q<VisualElement>("Fade");
            menuRoot.style.display = DisplayStyle.None;

            action.FindAction("ESC").performed += OnESC;

            pauseButton.clicked += OnPause;
            continueButton.clicked += OnContinue;
            backToMenuButton.clicked += OnBackToMenu;
        }

        private void OnESC(InputAction.CallbackContext context)
        {
            menuRoot.style.display = menuRoot.style.display == DisplayStyle.None? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnPause()
        {
            menuRoot.style.display = DisplayStyle.Flex;
        }

        private void OnContinue()
        {
            menuRoot.style.display = DisplayStyle.None;
        }

        private void OnBackToMenu()
        {
            if(Player.current != null &&
               Player.current.isServer &&
               Player.current.isClient)
                NetGame.singleton.StopHost();
            else
                NetGame.singleton.StopClient();
        }

        public void OnShutdown(UIPause massage)
        {
            action.FindAction("ESC").performed -= OnESC;
            pauseButton.clicked -= OnPause;
            continueButton.clicked -= OnContinue;
            backToMenuButton.clicked -= OnBackToMenu;
        }
    }
}