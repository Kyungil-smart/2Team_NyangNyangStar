using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// 게임 코드에서 호출하는 디버그 로그 진입점이다.
/// 필터를 통과한 로그만 Unity Console에 출력한다.
/// </summary>
public static class DebugTool
{
    [Conditional("UNITY_EDITOR")]
    public static void Log(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(DebugLogLevel.Log, text, type, context, memberName, filePath, lineNumber);
    }

    [Conditional("UNITY_EDITOR")]
    public static void Warning(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(DebugLogLevel.Warning, text, type, context, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// 기존 코드의 오타 호출을 깨지 않기 위한 호환용 메서드이다.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void Warnning(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Warning(text, type, context, memberName, filePath, lineNumber);
    }

    [Conditional("UNITY_EDITOR")]
    public static void Error(
        string text,
        DebugType type,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(DebugLogLevel.Error, text, type, context, memberName, filePath, lineNumber);
    }

    [Conditional("UNITY_EDITOR")]
    public static void MissingComponent(
        string text = null,
        Object context = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string message = string.IsNullOrWhiteSpace(text)
            ? "컴포넌트를 찾을 수 없습니다."
            : $"{text}을(를) 찾을 수 없습니다.";

        Write(DebugLogLevel.Warning, message, DebugType.Missing, context, memberName, filePath, lineNumber);
    }

    public static void DebugPrintAll(bool value)
    {
        DebugConsoleManager.SetGlobalEnabled(value);
    }

    public static void DebugSelect(DebugType type, bool value)
    {
        DebugConsoleManager.SetTypeEnabledStatic(type, value);
    }

    private static void Write(
        DebugLogLevel level,
        string text,
        DebugType type,
        Object context,
        string memberName,
        string filePath,
        int lineNumber)
    {
        if (!DebugConsoleManager.GetLevelEnabledStatic(level))
            return;

        if (!DebugConsoleManager.IsAllowedStatic(type, context))
            return;

        string message = BuildMessage(level, text, type, context, memberName, filePath, lineNumber);

        switch (level)
        {
            case DebugLogLevel.Warning:
                Debug.LogWarning(message, context);
                break;

            case DebugLogLevel.Error:
                Debug.LogError(message, context);
                break;

            default:
                Debug.Log(message, context);
                break;
        }
    }

    private static string BuildMessage(
        DebugLogLevel level,
        string text,
        DebugType type,
        Object context,
        string memberName,
        string filePath,
        int lineNumber)
    {
        string color = GetColor(type);
        string sourceName = GetSourceName(context, filePath);
        string safeMemberName = memberName == ".ctor" ? "생성자" : memberName;
        string source = $"{sourceName}.{safeMemberName} : {Mathf.Max(1, lineNumber)}";
        string levelText = level == DebugLogLevel.Log ? string.Empty : $"/{level}";

        return $"<color={color}>[{type}{levelText}] {text}</color>\n" +
               $"<color=#DAA520>출처 : [{source}]</color>";
    }

    private static string GetSourceName(Object context, string filePath)
    {
        if (context is Component component)
            return $"{component.gameObject.name}/{component.GetType().Name}";

        if (context is GameObject go)
            return go.name;

        if (context != null)
            return context.name;

        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName;
    }

    private static string GetColor(DebugType type)
    {
        return type switch
        {
            DebugType.Game => "#B388FF",
            DebugType.Character => "#FFD166",
            DebugType.Zombie => "#FF3B30",
            DebugType.Spawner => "#00C2FF",
            DebugType.Wave => "#FF7A00",
            DebugType.Node => "#A3FF12",
            DebugType.Network => "#00E676",
            DebugType.UI => "#FF4FD8",
            DebugType.Data => "#00D1B2",
            DebugType.Audio => "#4F6BFF",
            DebugType.Missing => "#FFFF00",
            DebugType.CombatNet => "#FF6B35",   // 전투 라인 네트워크 (B)
            DebugType.EconomyNet => "#00B894",  // 자원·성장 라인 네트워크 (F)
            _ => "#D0D0D0"
        };
    }
}

/// <summary>
/// 로그의 기능 영역을 구분하기 위한 타입이다.
/// </summary>
public enum DebugType
{
    Game,
    Data,
    Network,
    Character,
    Zombie,
    Wave,
    Spawner,
    Node,
    UI,
    Audio,
    Missing,
    Default,
    CombatNet,    // 전투 라인 네트워크 로그 (B 소유 NetworkBehaviour 들)
    EconomyNet,   // 자원·성장 라인 네트워크 로그 (F 소유 NetworkBehaviour 들)
}
