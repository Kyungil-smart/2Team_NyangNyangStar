#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AddressableFolderRegisterWindow : EditorWindow
{
    private Object _targetFolder;

    private AddressableAssetSettings _settings;
    private List<AddressableAssetGroup> _groups = new();
    private string[] _groupNames = new string[0];
    private int _selectedGroupIndex;

    private string _addressPrefix = "";
    private string _label = "";

    private bool _removeFolderEntry = true;
    private bool _moveExistingEntry = true;
    
    private bool _useFileNameOnly = true;
    private bool _clearExistingEntriesBeforeRegister = true;

    private string _excludeExtensions = ".cs,.meta,.asmdef,.dll";
    
    [MenuItem("Tools/Addressables/폴더 하위 파일 개별 등록")]
    private static void Open()
    {
        AddressableFolderRegisterWindow window =
            GetWindow<AddressableFolderRegisterWindow>("Addressable Folder Register");

        window.minSize = new Vector2(560f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(560f, 420f);
        RefreshSettings();
    }

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(8);

        _targetFolder = EditorGUILayout.ObjectField(
            "등록할 폴더",
            _targetFolder,
            typeof(Object),
            false
        );

        if (GUILayout.Button("현재 선택한 폴더 가져오기"))
        {
            _targetFolder = Selection.activeObject;
        }

        EditorGUILayout.Space(8);

        if (_settings == null)
        {
            EditorGUILayout.HelpBox(
                "AddressableAssetSettings를 찾을 수 없습니다. Addressables Groups 창을 먼저 생성하세요.",
                MessageType.Error
            );

            if (GUILayout.Button("Addressables 설정 새로고침"))
            {
                RefreshSettings();
            }

            return;
        }

        if (_groups.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "등록 가능한 Addressables Group이 없습니다.",
                MessageType.Error
            );

            if (GUILayout.Button("Group 목록 새로고침"))
            {
                RefreshSettings();
            }

            return;
        }

        _selectedGroupIndex = EditorGUILayout.Popup(
            "등록할 Group",
            _selectedGroupIndex,
            _groupNames
        );

        EditorGUILayout.Space(8);

        _useFileNameOnly = EditorGUILayout.ToggleLeft(
            "Address를 확장자 없는 파일명으로만 생성",
            _useFileNameOnly
        );
        
        _addressPrefix = EditorGUILayout.TextField("Address Prefix", _addressPrefix);

        EditorGUILayout.HelpBox(
            "예: Prefabs/UI 를 입력하면 Address가 Prefabs/UI/파일명 형태로 생성됩니다.",
            MessageType.Info
        );

        _label = EditorGUILayout.TextField("자동 추가 Label", _label);

        EditorGUILayout.Space(8);
        
        _removeFolderEntry = DrawToggleRow(
            "폴더 자체 Entry 제거",
            _removeFolderEntry
        );

        _moveExistingEntry = DrawToggleRow(
            "기존 Entry가 있으면 선택 Group으로 이동",
            _moveExistingEntry
        );

        _excludeExtensions = EditorGUILayout.TextField(
            "제외 확장자",
            _excludeExtensions
        );

        EditorGUILayout.Space(12);

        _clearExistingEntriesBeforeRegister = EditorGUILayout.ToggleLeft(
            "등록 전 하위 파일 기존 Entry 삭제",
            _clearExistingEntriesBeforeRegister
        );
        
        using (new EditorGUI.DisabledScope(!CanRegister()))
        {
            if (GUILayout.Button("하위 파일 기존 Entry 삭제 후 재등록"))
            {
                RegisterFolderChildren();
            }
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Addressables 설정 새로고침"))
        {
            RefreshSettings();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField(
            "선택한 폴더 하위 파일을 개별 Addressable Entry로 등록합니다.",
            EditorStyles.boldLabel
        );
    }

    private void RefreshSettings()
    {
        _settings = AddressableAssetSettingsDefaultObject.Settings;
        _groups.Clear();

        if (_settings == null)
            return;

        foreach (AddressableAssetGroup group in _settings.groups)
        {
            if (group == null)
                continue;

            _groups.Add(group);
        }

        _groupNames = new string[_groups.Count];

        for (int i = 0; i < _groups.Count; i++)
        {
            _groupNames[i] = _groups[i].Name;
        }

        if (_selectedGroupIndex >= _groups.Count)
            _selectedGroupIndex = 0;
    }

    private bool CanRegister()
    {
        if (_targetFolder == null)
            return false;

        string folderPath = AssetDatabase.GetAssetPath(_targetFolder);

        if (string.IsNullOrEmpty(folderPath))
            return false;

        if (!AssetDatabase.IsValidFolder(folderPath))
            return false;

        if (_settings == null)
            return false;

        if (_groups.Count == 0)
            return false;

        return true;
    }

    private void RegisterFolderChildren()
    {
        string folderPath = AssetDatabase.GetAssetPath(_targetFolder);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("선택한 대상이 폴더가 아닙니다.");
            return;
        }

        AddressableAssetGroup targetGroup = _groups[_selectedGroupIndex];

        if (targetGroup == null)
        {
            Debug.LogError("등록할 Addressables Group이 없습니다.");
            return;
        }

        if (_removeFolderEntry)
        {
            RemoveFolderEntry(folderPath);
        }

        HashSet<string> excludeSet = BuildExcludeExtensionSet();

        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

        List<string> targetGuids = new();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (AssetDatabase.IsValidFolder(assetPath))
                continue;

            string extension = Path.GetExtension(assetPath).ToLower();

            if (excludeSet.Contains(extension))
                continue;

            targetGuids.Add(guid);
        }

        int removedCount = 0;
        int registeredCount = 0;

        if (_clearExistingEntriesBeforeRegister)
        {
            foreach (string guid in targetGuids)
            {
                AddressableAssetEntry existingEntry = _settings.FindAssetEntry(guid);

                if (existingEntry == null)
                    continue;

                _settings.RemoveAssetEntry(guid);
                removedCount++;
            }
        }

        HashSet<string> usedAddresses = new();

        foreach (string guid in targetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            AddressableAssetEntry entry = _settings.CreateOrMoveEntry(guid, targetGroup);

            string address = BuildAddress(folderPath, assetPath);

            if (!usedAddresses.Add(address))
            {
                Debug.LogWarning($"중복 Address 감지: {address} / 파일명만 키로 사용할 경우 중복될 수 있습니다.");
            }

            entry.address = address;

            if (!string.IsNullOrWhiteSpace(_label))
            {
                entry.SetLabel(_label.Trim(), true, true, true);
            }

            registeredCount++;
        }

        _settings.SetDirty(
            AddressableAssetSettings.ModificationEvent.EntryMoved,
            null,
            true
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Addressable 재등록 완료 / Group: {targetGroup.Name}, 기존 Entry 삭제: {removedCount}개, 재등록: {registeredCount}개"
        );
    }

    private void RemoveFolderEntry(string folderPath)
    {
        string folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
        AddressableAssetEntry folderEntry = _settings.FindAssetEntry(folderGuid);

        if (folderEntry != null)
        {
            _settings.RemoveAssetEntry(folderGuid);
        }
    }

    private HashSet<string> BuildExcludeExtensionSet()
    {
        HashSet<string> result = new();

        string[] extensions = _excludeExtensions.Split(',');

        foreach (string extension in extensions)
        {
            string trimmed = extension.Trim().ToLower();

            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (!trimmed.StartsWith("."))
                trimmed = "." + trimmed;

            result.Add(trimmed);
        }

        return result;
    }

    private string BuildAddress(string rootFolderPath, string assetPath)
    {
        string prefix = _addressPrefix.Trim().Replace("\\", "/").Trim('/');

        if (_useFileNameOnly)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);

            if (string.IsNullOrEmpty(prefix))
                return fileNameWithoutExtension;

            return $"{prefix}/{fileNameWithoutExtension}";
        }

        string relativePath = assetPath
            .Replace(rootFolderPath + "/", "")
            .Replace("\\", "/");

        string addressWithoutExtension = Path.ChangeExtension(relativePath, null)
            .Replace("\\", "/");

        if (string.IsNullOrEmpty(prefix))
            return addressWithoutExtension;

        return $"{prefix}/{addressWithoutExtension}";
    }
    
    private bool DrawToggleRow(string label, bool value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(
                label,
                EditorStyles.wordWrappedLabel,
                GUILayout.ExpandWidth(true)
            );

            value = EditorGUILayout.Toggle(
                value,
                GUILayout.Width(18f)
            );
        }

        return value;
    }
}
#endif