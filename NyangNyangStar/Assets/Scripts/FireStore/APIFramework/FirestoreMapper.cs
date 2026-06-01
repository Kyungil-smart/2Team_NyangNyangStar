using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Firebase.Firestore;
using UnityEngine;


public static class FirestoreMapper
{

    private const BindingFlags kFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // 타입별 매핑 대상 필드 캐시
    private static readonly Dictionary<Type, FieldInfo[]> s_FieldCache = new Dictionary<Type, FieldInfo[]>();
    // 타입별 서브컬렉션 필드 캐시
    private static readonly Dictionary<Type, FieldInfo[]> s_SubCache = new Dictionary<Type, FieldInfo[]>();

    // ──────────────────────────────────────────────────────────────
    // 경로 조립
    // ──────────────────────────────────────────────────────────────


    public static DocumentReference ResolveDocument(
        FirebaseFirestore db, string template, string userId, string documentId)
    {
        if (db == null) throw new InvalidOperationException("Firestore db가 주입되지 않았습니다. InitDataBase를 먼저 호출하세요.");
        if (string.IsNullOrEmpty(template)) throw new InvalidOperationException("[FirestorePath] 어트리뷰트가 없습니다.");

        string[] segs = template.Split('/');
        if (segs.Length % 2 != 0)
            throw new InvalidOperationException($"경로 템플릿은 컬렉션/문서 쌍으로 끝나야 합니다(짝수 세그먼트): '{template}'");

        CollectionReference col = null;
        DocumentReference doc = null;

        for (int i = 0; i < segs.Length; i++)
        {
            string token = Substitute(segs[i], userId, documentId);
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException($"경로 토큰이 비어 있습니다: '{segs[i]}' in '{template}'");

            if (i % 2 == 0) // 컬렉션
                col = (doc == null) ? db.Collection(token) : doc.Collection(token);
            else            // 문서
                doc = col.Document(token);
        }
        return doc;
    }

    private static string Substitute(string seg, string userId, string documentId)
    {
        if (seg.Length >= 2 && seg[0] == '{' && seg[seg.Length - 1] == '}')
        {
            string name = seg.Substring(1, seg.Length - 2);
            switch (name)
            {
                case "userId": return userId;
                case "docId":
                case "documentId": return documentId;
                default:
                    throw new InvalidOperationException($"알 수 없는 경로 토큰 '{{{name}}}'. 지원: {{userId}}, {{docId}}");
            }
        }
        return seg; 
    }

    // ──────────────────────────────────────────────────────────────
    // 필드 <-> dictionary
    // ──────────────────────────────────────────────────────────────


    public static Dictionary<string, object> ToDictionary(BaseFireStore target)
    {
        var dict = new Dictionary<string, object>();
        foreach (var f in GetMappedFields(target.GetType()))
            dict[KeyOf(f)] = f.GetValue(target);
        return dict;
    }


    public static void ApplyFromSnapshot(BaseFireStore target, DocumentSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists) return;

        foreach (var f in GetMappedFields(target.GetType()))
        {
            string key = KeyOf(f);
            if (!snapshot.ContainsField(key)) continue;

            object raw = snapshot.GetValue<object>(key);
            f.SetValue(target, ConvertValue(raw, f.FieldType));
        }
    }


    public static IEnumerable<BaseFireStore> GetSubCollections(BaseFireStore target)
    {
        foreach (var f in GetSubFields(target.GetType()))
        {
            object v = f.GetValue(target);
            if (v == null) continue;

            if (v is BaseFireStore single)
            {
                yield return single;
            }
            else if (v is IEnumerable list)
            {
                foreach (var item in list)
                    if (item is BaseFireStore sub && sub != null)
                        yield return sub;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // 내부 헬퍼
    // ──────────────────────────────────────────────────────────────

    private static string KeyOf(FieldInfo f)
    {
        var attr = f.GetCustomAttribute<FirestoreFieldAttribute>();
        if (attr != null && !string.IsNullOrEmpty(attr.Key)) return attr.Key;
        return ToPascalCase(f.Name);
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        // Unity 관례인 선행 'm_' / '_' 제거
        if (name.StartsWith("m_")) name = name.Substring(2);
        else if (name.StartsWith("_")) name = name.Substring(1);
        if (name.Length == 0) return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }


    private static FieldInfo[] GetMappedFields(Type type)
    {
        if (s_FieldCache.TryGetValue(type, out var cached)) return cached;

        var result = new List<FieldInfo>();
        foreach (var f in EnumerateDeclaredFields(type))
        {
            if (!IsSerialized(f)) continue;
            if (f.IsDefined(typeof(FirestoreIgnoreAttribute), true)) continue;
            if (IsSubCollectionField(f)) continue;
            if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType)) continue;
            result.Add(f);
        }
        var arr = result.ToArray();
        s_FieldCache[type] = arr;
        return arr;
    }

    private static FieldInfo[] GetSubFields(Type type)
    {
        if (s_SubCache.TryGetValue(type, out var cached)) return cached;

        var result = new List<FieldInfo>();
        foreach (var f in EnumerateDeclaredFields(type))
        {
            if (!IsSerialized(f)) continue;
            if (IsSubCollectionField(f)) result.Add(f);
        }
        var arr = result.ToArray();
        s_SubCache[type] = arr;
        return arr;
    }


    private static IEnumerable<FieldInfo> EnumerateDeclaredFields(Type type)
    {
        for (Type t = type; t != null && t != typeof(BaseFireStore); t = t.BaseType)
            foreach (var f in t.GetFields(kFlags))
                yield return f;
    }

    private static bool IsSerialized(FieldInfo f)
    {
        if (f.IsDefined(typeof(NonSerializedAttribute), false)) return false;
        if (f.IsPublic) return true;
        return f.IsDefined(typeof(SerializeField), false);
    }

    private static bool IsSubCollectionField(FieldInfo f)
    {
        Type ft = f.FieldType;
        if (typeof(BaseFireStore).IsAssignableFrom(ft)) return true;
        if (ft.IsArray && typeof(BaseFireStore).IsAssignableFrom(ft.GetElementType())) return true;
        if (ft.IsGenericType)
            foreach (var arg in ft.GetGenericArguments())
                if (typeof(BaseFireStore).IsAssignableFrom(arg)) return true;
        return false;
    }


    private static object ConvertValue(object raw, Type target)
    {
        if (raw == null) return null;
        if (target.IsInstanceOfType(raw)) return raw;

        Type underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (underlying.IsEnum)
            return Enum.ToObject(underlying, Convert.ToInt64(raw));

        try
        {
            return Convert.ChangeType(raw, underlying);
        }
        catch
        {
            Debug.LogWarning($"[FirestoreMapper] 값 변환 실패: {raw?.GetType().Name} → {target.Name}. 기본값 유지.");
            return target.IsValueType ? Activator.CreateInstance(target) : null;
        }
    }
}
