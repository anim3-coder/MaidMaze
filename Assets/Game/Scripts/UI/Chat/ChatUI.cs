using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private int maxLines = 10;
    [SerializeField] private InputActionReference openChatAction;

    private VisualElement _chatRoot;
    private ScrollView _log;
    private VisualElement _content;
    private TextField _input;
    private readonly Queue<Label> _lines = new();
    private InputActionMap _gameplayMap;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        _chatRoot = document.rootVisualElement.Q<VisualElement>("chat-root");
        _log = document.rootVisualElement.Q<ScrollView>("chat-log");
        _content = document.rootVisualElement.Q<VisualElement>("chat-content");
        _input = document.rootVisualElement.Q<TextField>("chat-input");

        _input.style.display = DisplayStyle.None;
        _input.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
        _gameplayMap = InputSystem.actions.FindActionMap("Player");
        CloseChat();
    }

    private void OnEnable()
    {
        if (openChatAction != null)
            openChatAction.action.performed += OnOpenChatPerformed;
    }

    private void OnDisable()
    {
        if (openChatAction != null)
            openChatAction.action.performed -= OnOpenChatPerformed;
    }

    private void OnOpenChatPerformed(InputAction.CallbackContext context)
    {
        if (!IsOpen) OpenChat();
    }

    private void OpenChat()
    {
        IsOpen = true;
        _gameplayMap?.Disable();
        _chatRoot.AddToClassList("chat-active");
        _input.style.display = DisplayStyle.Flex;
        _input.schedule.Execute(() => _input.Focus()).ExecuteLater(0);
    }

    private void CloseChat()
    {
        IsOpen = false;
        _gameplayMap?.Enable();
        _chatRoot.RemoveFromClassList("chat-active");
        _input.style.display = DisplayStyle.None;
        _input.SetValueWithoutNotify(string.Empty);
    }

    private void OnInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            Submit(_input.value);
            CloseChat();
            evt.StopImmediatePropagation();
        }
        else if (evt.keyCode == KeyCode.Escape)
        {
            CloseChat();
            evt.StopImmediatePropagation();
        }
    }

    private void Submit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        OnLocalSubmit?.Invoke(text);
    }

    public event Action<string> OnLocalSubmit;

    // Вызывается сетевым слоем
    public void ReceiveMessage(string sender, string text) => AddMessage(sender, text);

    public void AddMessage(string sender, string text, bool isLocal = false, bool isSystem = false)
    {
        var label = new Label($"<{sender}> {text}");
        label.enableRichText = false;              // защита от <color=...> в тексте
        label.AddToClassList("chat-line");
        if (isLocal) label.AddToClassList("chat-line-local");
        if (isSystem) label.AddToClassList("sender-system");

        _content.Add(label);
        _lines.Enqueue(label);

        while (_lines.Count > maxLines)
            _lines.Dequeue().RemoveFromHierarchy();

        _log.scrollOffset = new Vector2(0f, float.MaxValue);
    }
}