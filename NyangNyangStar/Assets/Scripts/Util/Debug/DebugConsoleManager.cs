using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// DebugTool의 필터 상태만 관리하는 경량 매니저이다.
/// 런타임 디버그 창, 로그 저장, 로그 렌더링 기능은 제거했다.
/// </summary>
public class DebugConsoleManager : MonoBehaviour
{
    private const string PrefPrefix = "DebugConsole.Filter";
    private const string GlobalEnabledKey = PrefPrefix + ".GlobalEnabled";
    private const string LevelLogKey = PrefPrefix + ".Level.Log";
    private const string LevelWarningKey = PrefPrefix + ".Level.Warning";
    private const string LevelErrorKey = PrefPrefix + ".Level.Error";
    private const string DisabledGameObjectKeysKey = PrefPrefix + ".DisabledGameObjects";
    private const string DisabledComponentKeysKey = PrefPrefix + ".DisabledComponents";

    private static bool _initialized;
    private static bool _globalEnabled = true;
    private static bool _showLogLevelLog = true;
    private static bool _showLogLevelWarning = true;
    private static bool _showLogLevelError = true;
    private static bool[] _typeFilters;
    private static int _changeVersion;

    private static readonly Dictionary<int, bool> GameObjectSessionFilters = new();
    private static readonly Dictionary<int, bool> ComponentSessionFilters = new();
    private static readonly HashSet<string> DisabledGameObjectKeys = new();
    private static readonly HashSet<string> DisabledComponentKeys = new();

    public static DebugConsoleManager Instance { get; private set; }

    public int ChangeVersion => _changeVersion;

    public bool GlobalEnabled
    {
        get => GetGlobalEnabled();
        set => SetGlobalEnabled(value);
    }

    /// <summary>
    /// 이전 구조와의 호환용 속성이다.
    /// 현재 DebugTool은 항상 Unity Console로 직접 출력하므로 값이 출력 경로를 바꾸지는 않는다.
    /// </summary>
    public bool MirrorToUnityConsole { get; set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Initialize();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool GetGlobalEnabled()
    {
        Initialize();
        return _globalEnabled;
    }

    public static void SetGlobalEnabled(bool value)
    {
        Initialize();
        if (_globalEnabled == value)
            return;

        _globalEnabled = value;
        PlayerPrefs.SetInt(GlobalEnabledKey, value ? 1 : 0);
        PlayerPrefs.Save();
        MarkChanged();
    }

    public static bool GetTypeEnabledStatic(DebugType type)
    {
        Initialize();
        int index = (int)type;
        if (index < 0 || index >= _typeFilters.Length)
            return true;

        return _typeFilters[index];
    }

    public bool GetTypeEnabled(DebugType type)
    {
        return GetTypeEnabledStatic(type);
    }

    public static void SetTypeEnabledStatic(DebugType type, bool value)
    {
        Initialize();
        int index = (int)type;
        if (index < 0 || index >= _typeFilters.Length)
            return;

        if (_typeFilters[index] == value)
            return;

        _typeFilters[index] = value;
        PlayerPrefs.SetInt(GetTypePrefKey(type), value ? 1 : 0);
        PlayerPrefs.Save();
        MarkChanged();
    }

    public void SetTypeEnabled(DebugType type, bool value)
    {
        SetTypeEnabledStatic(type, value);
    }

    public static void SetAllTypesStatic(bool value)
    {
        Initialize();
        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));

        foreach (DebugType type in types)
        {
            int index = (int)type;
            if (index < 0 || index >= _typeFilters.Length)
                continue;

            _typeFilters[index] = value;
            PlayerPrefs.SetInt(GetTypePrefKey(type), value ? 1 : 0);
        }

        PlayerPrefs.Save();
        MarkChanged();
    }

    public void SetAllTypes(bool value)
    {
        SetAllTypesStatic(value);
    }

    public static bool GetLevelEnabledStatic(DebugLogLevel level)
    {
        Initialize();

        return level switch
        {
            DebugLogLevel.Warning => _showLogLevelWarning,
            DebugLogLevel.Error => _showLogLevelError,
            _ => _showLogLevelLog
        };
    }

    public bool GetLevelEnabled(DebugLogLevel level)
    {
        return GetLevelEnabledStatic(level);
    }

    public static void SetLevelEnabledStatic(DebugLogLevel level, bool value)
    {
        Initialize();

        switch (level)
        {
            case DebugLogLevel.Warning:
                if (_showLogLevelWarning == value)
                    return;

                _showLogLevelWarning = value;
                PlayerPrefs.SetInt(LevelWarningKey, value ? 1 : 0);
                break;

            case DebugLogLevel.Error:
                if (_showLogLevelError == value)
                    return;

                _showLogLevelError = value;
                PlayerPrefs.SetInt(LevelErrorKey, value ? 1 : 0);
                break;

            default:
                if (_showLogLevelLog == value)
                    return;

                _showLogLevelLog = value;
                PlayerPrefs.SetInt(LevelLogKey, value ? 1 : 0);
                break;
        }

        PlayerPrefs.Save();
        MarkChanged();
    }

    public void SetLevelEnabled(DebugLogLevel level, bool value)
    {
        SetLevelEnabledStatic(level, value);
    }

    public void SetAllLevels(bool value)
    {
        SetLevelEnabledStatic(DebugLogLevel.Log, value);
        SetLevelEnabledStatic(DebugLogLevel.Warning, value);
        SetLevelEnabledStatic(DebugLogLevel.Error, value);
    }

    public void SetWarningAndErrorOnly()
    {
        SetLevelEnabledStatic(DebugLogLevel.Log, false);
        SetLevelEnabledStatic(DebugLogLevel.Warning, true);
        SetLevelEnabledStatic(DebugLogLevel.Error, true);
    }

    public void SetErrorOnly()
    {
        SetLevelEnabledStatic(DebugLogLevel.Log, false);
        SetLevelEnabledStatic(DebugLogLevel.Warning, false);
        SetLevelEnabledStatic(DebugLogLevel.Error, true);
    }

    public static bool GetGameObjectEnabledStatic(GameObject go)
    {
        Initialize();

        if (go == null)
            return true;

        string key = BuildPersistentKey(go);
        if (!string.IsNullOrEmpty(key))
            return !DisabledGameObjectKeys.Contains(key);

        return GetGameObjectEnabledStatic(go.GetInstanceID());
    }

    public bool GetGameObjectEnabled(GameObject go)
    {
        return GetGameObjectEnabledStatic(go);
    }

    public static bool GetGameObjectEnabledStatic(int instanceId)
    {
        Initialize();

        if (instanceId == 0)
            return true;

        return !GameObjectSessionFilters.TryGetValue(instanceId, out bool value) || value;
    }

    public bool GetGameObjectEnabled(int instanceId)
    {
        return GetGameObjectEnabledStatic(instanceId);
    }

    public static void SetGameObjectEnabledStatic(GameObject go, bool value)
    {
        Initialize();

        if (go == null)
            return;

        GameObjectSessionFilters[go.GetInstanceID()] = value;

        string key = BuildPersistentKey(go);
        if (string.IsNullOrEmpty(key))
        {
            MarkChanged();
            return;
        }

        bool changed = SetDisabledKeyState(DisabledGameObjectKeys, key, value);
        if (!changed)
            return;

        SavePersistentFilters();
        MarkChanged();
    }

    public void SetGameObjectEnabled(GameObject go, bool value)
    {
        SetGameObjectEnabledStatic(go, value);
    }

    /// <summary>
    /// 인스턴스 ID만 받은 경우에는 안정적인 저장 키를 만들 수 없으므로 현재 에디터 세션에서만 유지한다.
    /// 저장이 필요한 경우 GameObject를 넘기는 오버로드를 사용한다.
    /// </summary>
    public static void SetGameObjectEnabledStatic(int instanceId, bool value)
    {
        Initialize();

        if (instanceId == 0)
            return;

        if (GameObjectSessionFilters.TryGetValue(instanceId, out bool current) && current == value)
            return;

        GameObjectSessionFilters[instanceId] = value;
        MarkChanged();
    }

    public static bool GetComponentEnabledStatic(Component component)
    {
        Initialize();

        if (component == null)
            return true;

        string key = BuildPersistentKey(component);
        if (!string.IsNullOrEmpty(key))
            return !DisabledComponentKeys.Contains(key);

        return GetComponentEnabledStatic(component.GetInstanceID());
    }

    public bool GetComponentEnabled(Component component)
    {
        return GetComponentEnabledStatic(component);
    }

    public static bool GetComponentEnabledStatic(int instanceId)
    {
        Initialize();

        if (instanceId == 0)
            return true;

        return !ComponentSessionFilters.TryGetValue(instanceId, out bool value) || value;
    }

    public bool GetComponentEnabled(int instanceId)
    {
        return GetComponentEnabledStatic(instanceId);
    }

    public static void SetComponentEnabledStatic(Component component, bool value)
    {
        Initialize();

        if (component == null)
            return;

        ComponentSessionFilters[component.GetInstanceID()] = value;

        string key = BuildPersistentKey(component);
        if (string.IsNullOrEmpty(key))
        {
            MarkChanged();
            return;
        }

        bool changed = SetDisabledKeyState(DisabledComponentKeys, key, value);
        if (!changed)
            return;

        SavePersistentFilters();
        MarkChanged();
    }

    public void SetComponentEnabled(Component component, bool value)
    {
        SetComponentEnabledStatic(component, value);
    }

    /// <summary>
    /// 인스턴스 ID만 받은 경우에는 안정적인 저장 키를 만들 수 없으므로 현재 에디터 세션에서만 유지한다.
    /// 저장이 필요한 경우 Component를 넘기는 오버로드를 사용한다.
    /// </summary>
    public static void SetComponentEnabledStatic(int instanceId, bool value)
    {
        Initialize();

        if (instanceId == 0)
            return;

        if (ComponentSessionFilters.TryGetValue(instanceId, out bool current) && current == value)
            return;

        ComponentSessionFilters[instanceId] = value;
        MarkChanged();
    }

    public static bool IsAllowedStatic(DebugType type, Object context)
    {
        Initialize();

        if (!_globalEnabled)
            return false;

        if (!GetTypeEnabledStatic(type))
            return false;

        if (context is GameObject go)
            return GetGameObjectEnabledStatic(go);

        if (context is Component component)
            return GetGameObjectEnabledStatic(component.gameObject) && GetComponentEnabledStatic(component);

        return true;
    }

    public bool IsAllowed(DebugType type, Object context)
    {
        return IsAllowedStatic(type, context);
    }

    public bool IsAllowed(DebugType type, int gameObjectId, int componentId)
    {
        Initialize();

        if (!_globalEnabled)
            return false;

        if (!GetTypeEnabledStatic(type))
            return false;

        if (gameObjectId != 0 && !GetGameObjectEnabledStatic(gameObjectId))
            return false;

        if (componentId != 0 && !GetComponentEnabledStatic(componentId))
            return false;

        return true;
    }

    public static void ResetAllFiltersToDefaultStatic()
    {
        Initialize();

        _globalEnabled = true;
        PlayerPrefs.SetInt(GlobalEnabledKey, 1);
        SetAllTypesStatic(true);
        SetLevelEnabledStatic(DebugLogLevel.Log, true);
        SetLevelEnabledStatic(DebugLogLevel.Warning, true);
        SetLevelEnabledStatic(DebugLogLevel.Error, true);

        GameObjectSessionFilters.Clear();
        ComponentSessionFilters.Clear();
        DisabledGameObjectKeys.Clear();
        DisabledComponentKeys.Clear();
        SavePersistentFilters();
        MarkChanged();
    }

    public void ResetAllFiltersToDefault()
    {
        ResetAllFiltersToDefaultStatic();
    }

    public void ClearLogs()
    {
        // Unity Console을 사용하므로 별도 저장 로그를 비우는 작업은 없다.
    }

    private static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _globalEnabled = PlayerPrefs.GetInt(GlobalEnabledKey, 1) == 1;
        _showLogLevelLog = PlayerPrefs.GetInt(LevelLogKey, 1) == 1;
        _showLogLevelWarning = PlayerPrefs.GetInt(LevelWarningKey, 1) == 1;
        _showLogLevelError = PlayerPrefs.GetInt(LevelErrorKey, 1) == 1;

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        int length = 0;

        foreach (DebugType type in types)
            length = Mathf.Max(length, (int)type + 1);

        _typeFilters = new bool[Mathf.Max(1, length)];

        foreach (DebugType type in types)
        {
            int index = (int)type;
            if (index < 0 || index >= _typeFilters.Length)
                continue;

            _typeFilters[index] = PlayerPrefs.GetInt(GetTypePrefKey(type), 1) == 1;
        }

        LoadPersistentFilters();
    }

    private static string GetTypePrefKey(DebugType type)
    {
        return $"{PrefPrefix}.Type.{type}";
    }

    private static bool SetDisabledKeyState(HashSet<string> disabledKeys, string key, bool enabled)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        if (enabled)
            return disabledKeys.Remove(key);

        return disabledKeys.Add(key);
    }

    private static void LoadPersistentFilters()
    {
        DisabledGameObjectKeys.Clear();
        DisabledComponentKeys.Clear();

        LoadKeySet(DisabledGameObjectKeysKey, DisabledGameObjectKeys);
        LoadKeySet(DisabledComponentKeysKey, DisabledComponentKeys);
    }

    private static void SavePersistentFilters()
    {
        SaveKeySet(DisabledGameObjectKeysKey, DisabledGameObjectKeys);
        SaveKeySet(DisabledComponentKeysKey, DisabledComponentKeys);
        PlayerPrefs.Save();
    }

    private static void LoadKeySet(string prefKey, HashSet<string> target)
    {
        string json = PlayerPrefs.GetString(prefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            FilterKeyList data = JsonUtility.FromJson<FilterKeyList>(json);
            if (data?.Keys == null)
                return;

            foreach (string key in data.Keys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    target.Add(key);
            }
        }
        catch
        {
            target.Clear();
        }
    }

    private static void SaveKeySet(string prefKey, HashSet<string> source)
    {
        FilterKeyList data = new FilterKeyList
        {
            Keys = new List<string>(source)
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(prefKey, json);
    }

    private static string BuildPersistentKey(Object target)
    {
        if (target == null)
            return string.Empty;

#if UNITY_EDITOR
        try
        {
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(target);
            string idText = id.ToString();

            if (!string.IsNullOrWhiteSpace(idText) && !idText.Contains("-0-0"))
                return idText;
        }
        catch
        {
            // GlobalObjectId 생성에 실패한 경우 아래 fallback 키를 사용한다.
        }
#endif

        return BuildFallbackKey(target);
    }

    private static string BuildFallbackKey(Object target)
    {
        if (target is GameObject go)
            return $"GameObject:{GetSceneKey(go)}:{GetTransformPath(go.transform)}";

        if (target is Component component)
        {
            string gameObjectKey = BuildFallbackKey(component.gameObject);
            string typeKey = component.GetType().AssemblyQualifiedName ?? component.GetType().FullName;
            int componentIndex = GetComponentIndex(component);
            return $"Component:{gameObjectKey}:{typeKey}:{componentIndex}";
        }

        string objectType = target.GetType().AssemblyQualifiedName ?? target.GetType().FullName;
        return $"Object:{objectType}:{target.name}";
    }

    private static string GetSceneKey(GameObject go)
    {
        if (go.scene.IsValid())
        {
            if (!string.IsNullOrWhiteSpace(go.scene.path))
                return go.scene.path;

            if (!string.IsNullOrWhiteSpace(go.scene.name))
                return go.scene.name;
        }

        return "NoScene";
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        Stack<string> names = new Stack<string>();
        Transform current = transform;

        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static int GetComponentIndex(Component target)
    {
        if (target == null)
            return -1;

        Component[] components = target.gameObject.GetComponents(target.GetType());
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == target)
                return i;
        }

        return 0;
    }

    private static void MarkChanged()
    {
        _changeVersion++;
    }

    [Serializable]
    private sealed class FilterKeyList
    {
        public List<string> Keys = new();
    }
}
