using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LocalizedButton : Button
{
    public static BindingId keyProperty = nameof(key);

    [UxmlAttribute]
    public string key;

    public LocalizedButton()
    {
    }
}