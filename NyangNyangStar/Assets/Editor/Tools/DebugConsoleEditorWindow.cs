using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DebugTool의 필터만 조정하는 에디터 전용 창이다.
/// 로그 목록 렌더링과 런타임 창 제어 기능은 제거했다.
/// </summary>
public sealed class DebugConsoleEditorWindow : EditorWindow
{
    private readonly HashSet<int> _expandedObjects = new();
    private Vector2 _typeScroll;
    private Vector2 _hierarchyScroll;
    private string _objectSearch = string.Empty;
    private bool _hideTransform = true;

    [MenuItem("Tools/Debug/Debug Console Filter")]
    public static void Open()
    {
        GetWindow<DebugConsoleEditorWindow>("Debug Filter");
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawTypeFilters();
        EditorGUILayout.Space(8f);
        DrawObjectComponentFilters();
    }

    private void DrawHeader()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Debug Console Filter", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("필터를 통과한 DebugTool 로그만 Unity Console에 출력한다.");
            EditorGUILayout.LabelField("오브젝트/컴포넌트 필터는 저장되며, context를 넘긴 로그에만 적용된다.");
        }
    }

    private void DrawTypeFilters()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Debug Type", EditorStyles.boldLabel);

                if (GUILayout.Button("All On", GUILayout.Width(80f)))
                    DebugConsoleManager.SetAllTypesStatic(true);

                if (GUILayout.Button("All Off", GUILayout.Width(80f)))
                    DebugConsoleManager.SetAllTypesStatic(false);
            }

            _typeScroll = EditorGUILayout.BeginScrollView(_typeScroll, GUILayout.Height(96f));

            DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
            const int columns = 3;
            int index = 0;

            while (index < types.Length)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = 0; i < columns && index < types.Length; i++, index++)
                    {
                        DebugType type = types[index];
                        bool current = DebugConsoleManager.GetTypeEnabledStatic(type);
                        bool next = EditorGUILayout.ToggleLeft(type.ToString(), current, GUILayout.Width(140f));

                        if (next != current)
                            DebugConsoleManager.SetTypeEnabledStatic(type, next);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawObjectComponentFilters()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Object / Component", EditorStyles.boldLabel);

                if (GUILayout.Button("All Scene On", GUILayout.Width(110f)))
                    SetAllSceneObjects(true);

                if (GUILayout.Button("All Scene Off", GUILayout.Width(110f)))
                    SetAllSceneObjects(false);

                if (GUILayout.Button("Reset Saved Filters", GUILayout.Width(140f)))
                    DebugConsoleManager.ResetAllFiltersToDefaultStatic();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _objectSearch = EditorGUILayout.TextField("Search", _objectSearch);
                _hideTransform = EditorGUILayout.ToggleLeft("Hide Transform", _hideTransform, GUILayout.Width(120f));
            }

            _hierarchyScroll = EditorGUILayout.BeginScrollView(_hierarchyScroll);

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                EditorGUILayout.LabelField(scene.name, EditorStyles.boldLabel);
                GameObject[] roots = scene.GetRootGameObjects();

                foreach (GameObject root in roots)
                    DrawGameObjectNode(root, 0);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawGameObjectNode(GameObject go, int depth)
    {
        if (go == null)
            return;

        bool searchActive = !string.IsNullOrWhiteSpace(_objectSearch);
        if (searchActive && !ContainsSearchTarget(go, _objectSearch))
            return;

        int instanceId = go.GetInstanceID();
        bool expanded = searchActive || _expandedObjects.Contains(instanceId);
        bool objectEnabled = DebugConsoleManager.GetGameObjectEnabledStatic(go);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(depth * 16f);

            Rect foldoutRect = GUILayoutUtility.GetRect(16f, EditorGUIUtility.singleLineHeight, GUILayout.Width(16f));
            bool nextExpanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, true);
            if (!searchActive)
            {
                if (nextExpanded)
                    _expandedObjects.Add(instanceId);
                else
                    _expandedObjects.Remove(instanceId);
            }

            bool nextObjectEnabled = EditorGUILayout.Toggle(objectEnabled, GUILayout.Width(18f));
            if (nextObjectEnabled != objectEnabled)
                SetGameObjectSubtreeEnabled(go, nextObjectEnabled);

            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
        }

        if (!expanded)
            return;

        DrawComponents(go, depth + 1);

        for (int i = 0; i < go.transform.childCount; i++)
            DrawGameObjectNode(go.transform.GetChild(i).gameObject, depth + 1);
    }

    private void DrawComponents(GameObject go, int depth)
    {
        Component[] components = go.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            bool componentEnabled = DebugConsoleManager.GetComponentEnabledStatic(component);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 16f + 16f);

                bool nextComponentEnabled = EditorGUILayout.Toggle(componentEnabled, GUILayout.Width(18f));
                if (nextComponentEnabled != componentEnabled)
                    DebugConsoleManager.SetComponentEnabledStatic(component, nextComponentEnabled);

                EditorGUILayout.ObjectField(component, component.GetType(), true);
            }
        }
    }

    private static bool ContainsSearchTarget(GameObject go, string search)
    {
        if (go.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            string componentName = component.GetType().Name;
            if (componentName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (ContainsSearchTarget(go.transform.GetChild(i).gameObject, search))
                return true;
        }

        return false;
    }

    private static void SetAllSceneObjects(bool value)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
                SetGameObjectSubtreeEnabled(root, value);
        }
    }

    private static void SetGameObjectSubtreeEnabled(GameObject go, bool value)
    {
        if (go == null)
            return;

        DebugConsoleManager.SetGameObjectEnabledStatic(go, value);

        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component != null)
                DebugConsoleManager.SetComponentEnabledStatic(component, value);
        }

        for (int i = 0; i < go.transform.childCount; i++)
            SetGameObjectSubtreeEnabled(go.transform.GetChild(i).gameObject, value);
    }
}
