using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class UISettings : MonoBehaviour
{
    [SerializeField] private PanelRenderer doc;
    private string[] tabNames = { "Main", "Graphics", "Controls" };
    private SettingsProfile profile => Setup.profile;
    private VisualElement root;
    private Button applyButton;
    private DropdownField languageDropdown;
    private Button[] tabButtons;
    private VisualElement[] tabContents;
    public void SetEnable(bool value)
    {
        root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

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
        root = rootElement.Q<VisualElement>("SettingsPanel");
        applyButton = root.Q<Button>("applyButton");
        applyButton.clicked += OnApplyButton;

        #region Buttons
        tabButtons = new Button[tabNames.Length];
        tabContents = new VisualElement[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            string name = tabNames[i];
            tabButtons[i] = root.Q<Button>($"TabButton_{name}");
            tabContents[i] = root.Q<VisualElement>($"Tab_{name}");
            int index = i;
            tabButtons[i].clicked += () => SwitchToTab(index);
        }
        root.Q<Button>($"TabButton_Exit").clicked += () => SetEnable(false);
        #endregion

        #region Language
        languageDropdown = root.Q<DropdownField>("Language_Dropdown");
        var localeList = LocalizationSettings.AvailableLocales.Locales;
        var choices = new List<string>();
        int selectedIndex = 0;

        for (int i = 0; i < localeList.Count; i++)
        {
            var locale = localeList[i];
            choices.Add(locale.LocaleName);

            if (LocalizationSettings.SelectedLocale == locale)
            {
                selectedIndex = i;
            }
        }

        languageDropdown.choices = choices;
        languageDropdown.value = choices[selectedIndex];
        languageDropdown.RegisterValueChangedCallback(OnLocaleChanged);
        #endregion
        
        SetEnable(false);
    }
    private void SwitchToTab(int index)
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            bool isActive = (i == index);
            tabButtons[i].EnableInClassList("pagination-button--active", isActive);
            tabContents[i].EnableInClassList("tab-active", isActive);
        }
    }
    private void Update()
    {
        if(applyButton != null)
            applyButton.enabledSelf = profile.HasChange;
    }
    private void OnApplyButton()
    {
        profile.Apply();
    }
    private void OnDestroy()
    {
        if(applyButton != null) applyButton.clicked -= OnApplyButton;
    }
    private void OnLocaleChanged(ChangeEvent<string> evt)
    {
        // Находим индекс выбранной локали по названию
        int selectedIndex = languageDropdown.choices.IndexOf(evt.newValue);

        if (selectedIndex == -1)
            return;

        // Устанавливаем новую локаль
        var newLocale = LocalizationSettings.AvailableLocales.Locales[selectedIndex];
        LocalizationSettings.SelectedLocale = newLocale;
    }
}

public enum Quality
{
    High = 4,
    Medium = 3,
    Low = 2,
    VeryLow = 1,
}