using TriInspector;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

/// <summary>
/// Все данные сохранены в PlayerPrefs, Эта штука лишь интерфейс над ними
/// </summary>
[CreateAssetMenu(fileName = "SettingsProfile", menuName = "Game/SettingsProfile")]
public class SettingsProfile : ScriptableObject, IDataSourceViewHashProvider
{
    [SerializeField, DontCreateProperty] private UniversalRenderPipelineAsset PC_reference;
    [SerializeField, DontCreateProperty] private UniversalRenderPipelineAsset Mobile_reference;
    [Header("Main")]
    [CreateProperty, ShowInInspector] public string Language
    {
        get
        {
            return PlayerPrefs.GetString("selected-locale");
        }
        set
        {
            PlayerPrefs.GetString("selected-locale", value);
        }
    }
    [Header("Graphics")]
    [CreateProperty, ShowInInspector] public float renderScale
    {
        get
        {
            if (Application.isPlaying)
            {
                return PlayerPrefs.GetFloat("nativeRenderScale", URPAsset.renderScale);
            }
            else
            {
                return PlayerPrefs.GetFloat("nativeRenderScale");
            }
        }
        set
        {
            PlayerPrefs.SetFloat("nativeRenderScale", Mathf.Clamp(value, 0.5f, 2));
            Publish();
        }
    }
    [CreateProperty, ShowInInspector] public int renderScaleQuality
    {
        get
        {
            var storedValue = 1f;
            if (Application.isPlaying)
            {
                storedValue = PlayerPrefs.GetFloat("nativeRenderScale", URPAsset.renderScale);
            }
            else
            {
                storedValue = PlayerPrefs.GetFloat("nativeRenderScale");
            }

            int index = Mathf.RoundToInt((1f - storedValue) * 4f);
            return index;
        }
        set
        {
            PlayerPrefs.SetFloat("nativeRenderScale", 1f - ((int)value) * 0.25f);
            Publish();
        }
    }
    [CreateProperty, ShowInInspector] public bool IsAllowHDR
    {
        get
        {
            if (Application.isPlaying)
                return System.Convert.ToBoolean(PlayerPrefs.GetInt("IsAllowHDR", System.Convert.ToInt32(URPAsset.supportsHDR)));
            else
                return System.Convert.ToBoolean(PlayerPrefs.GetInt("IsAllowHDR"));
        }
        set
        {
            PlayerPrefs.SetInt("IsAllowHDR", System.Convert.ToInt32(value));
            Publish();
        }
    }
    public static MsaaQuality MSAA
    {
        get
        {
            if (Application.isPlaying)
                return (MsaaQuality)PlayerPrefs.GetInt("MSAAQuality", URPAsset.msaaSampleCount);
            else
                return (MsaaQuality)PlayerPrefs.GetInt("MSAAQuality");
        }
        set
        {
            PlayerPrefs.SetInt("MSAAQuality", (int)value);
        }
    }
    public static UpscalingFilterSelection UpscalingFilter
    {
        get
        {
            if (Application.isPlaying)
                return (UpscalingFilterSelection)PlayerPrefs.GetInt("UpscalingFilter", (int)URPAsset.upscalingFilter);
            else
                return (UpscalingFilterSelection)PlayerPrefs.GetInt("UpscalingFilter");
        }
        set
        {
            PlayerPrefs.SetInt("UpscalingFilter", (int)value);
        }
    }
    public static string PlayerDefaultName;
    [CreateProperty, ShowInInspector] public string PlayerName
    {
        get
        {
            if(PlayerDefaultName == null)
                PlayerDefaultName = GenerateRandomName();
            return PlayerPrefs.GetString("player_name", PlayerDefaultName);
        }
        set
        {
            PlayerPrefs.SetString("player_name", value);
            PlayerPrefs.Save();
        }
    }
    [CreateProperty, ShowInInspector] public int UpscalingFilterSelection
    {
        get
        {
            return (int)UpscalingFilter;
        }
        set
        {
            UpscalingFilter = (UpscalingFilterSelection)value;
        }
    }
    [CreateProperty, ShowInInspector] public int QualityMSAA
    {
        get
        {
            return Mathf.RoundToInt(Mathf.Log((int)MSAA,2));
        }
        set
        {
            MSAA = (MsaaQuality)(Mathf.Pow(2, value));
            Publish();
        }
    }

    private int m_viewVersion;
    private int m_applyVersion;
    public bool HasChange => GetViewHashCode() != GetApplyHashCode();
    public long GetViewHashCode() => m_viewVersion;
    public long GetApplyHashCode() => m_applyVersion;
    #region URP

    private static UniversalRenderPipelineAsset m_OriginalURPAsset;
    private static UniversalRenderPipelineAsset m_URPAsset;
    public static UniversalRenderPipelineAsset URPAsset
    {
        get
        {
            if(m_URPAsset == null)
            {
#if UNITY_STANDALONE
                m_OriginalURPAsset = Setup.profile.PC_reference;
#elif UNITY_ANDROID
                m_OriginalURPAsset = Setup.profile.Mobile_reference;
#endif
                if (m_OriginalURPAsset != null)
                {
                    m_URPAsset = Instantiate(m_OriginalURPAsset);
                    GraphicsSettings.defaultRenderPipeline = m_URPAsset;
                    QualitySettings.renderPipeline = m_URPAsset;
                }
            }
            return m_URPAsset;
        }
    }
#endregion
    public void Apply()
    {
        ///GRAPHICS
        URPAsset.msaaSampleCount = (int)MSAA;
        URPAsset.renderScale = renderScale;
        URPAsset.supportsHDR = IsAllowHDR;
        
        m_applyVersion = m_viewVersion;
    }
#if UNITY_EDITOR
    public void Unload(UnityEditor.PlayModeStateChange playMode)
    {
        if (playMode == PlayModeStateChange.ExitingPlayMode)
        {
            GraphicsSettings.defaultRenderPipeline = m_OriginalURPAsset;
            QualitySettings.renderPipeline = m_OriginalURPAsset;
            Destroy(m_URPAsset);
            UnityEditor.EditorApplication.playModeStateChanged -= Unload;
        }
    }
#endif
    public void Publish()
    {
        ++m_viewVersion;
    }

    public static float RoundToDecimalPlaces(float value, int decimalPlaces)
    {
        float multiplier = Mathf.Pow(10, decimalPlaces);
        return Mathf.Round(value * multiplier) / multiplier;
    }

    // Списки для генерации
    private static readonly string[] FirstNames =
    {
        "John", "Mike", "Alex", "Max", "Nick", "Sam", "Leo", "Dan",
        "Kate", "Anna", "Lily", "Emma", "Sara", "Mia", "Zoe", "Ivy"
    };

    private static readonly string[] LastNames =
    {
        "Hook", "Smith", "Stone", "Wolf", "Fox", "Kane", "Reed", "Cole",
        "Hart", "Gray", "Shaw", "Cross", "Frost", "Wild", "Storm"
    };

    private static string GenerateRandomName()
    {
        string first = FirstNames[Random.Range(0, FirstNames.Length)];
        string last = LastNames[Random.Range(0, LastNames.Length)];
        int number = Random.Range(100, 1000); // 100..999

        return $"{first}{last}{number}"; // JohnHook123
    }
}