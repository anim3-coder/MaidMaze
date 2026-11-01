using Unity.Scripting.LifecycleManagement;
using UnityEngine;

public static partial class Setup
{
    [OnCodeInitializing]
    public static void Run()
    {
        Application.targetFrameRate = 60;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AfterRun()
    {
        profile.Apply();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += profile.Unload;
#endif
    }
    [AutoStaticsCleanup]
    private static SettingsProfile m_profile;
    public static SettingsProfile profile
    {
        get
        {
            if(m_profile == null)
                m_profile = Resources.Load<SettingsProfile>("Settings");
            return m_profile;
        }
    }
}