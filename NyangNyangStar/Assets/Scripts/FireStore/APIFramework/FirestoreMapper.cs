using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Firebase.Firestore;
using UnityEngine;


public static class FirestoreMapper
{

    private const BindingFlags kFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;


    private static readonly Dictionary<Type, FieldInfo[]> s_FieldCache = new Dictionary<Type, FieldInfo[]>();

    private static readonly Dictionary<Type, FieldInfo[]> s_SubCache = new Dictionary<Type, FieldInfo[]>();

    private static readonly Dictionary<Type, FieldInfo[]> s_PocoCache = new Dictionary<Type, FieldInfo[]>();

    private static readonly Dictionary<Type, FieldInfo> s_MapKeyCache = new Dictionary<Type, FieldInfo>();

    private static readonly Dictionary<Type, FieldInfo[]> s_MapValueCache = new Dictionary<Type, FieldInfo[]>();

    private enum FieldKind { Scalar, FixedMap, DynamicMap, Array, SubCollection, Ignore }

    // [FirestoreMap] 오용 경고를 필드당 1회만 출력하기 위한 기록
    private static readonly HashSet<FieldInfo> s_WarnedMapMisuse = new HashSet<FieldInfo>();
    // 중첩 배열 경고를 필드당 1회만 출력하기 위한 기록
    private static readonly HashSet<FieldInfo> s_WarnedNestedArray = new HashSet<FieldInfo>();

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
            dict[KeyOf(f)] = SerializeValue(f.GetValue(target), f.FieldType, f);
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
            f.SetValue(target, DeserializeValue(raw, f.FieldType, f));
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


    // 한 값을 Firestore에 넣을 형태로 변환한다
    public static object SerializeValue(object value, Type type, FieldInfo field)
    {
        var kind = Classify(type, field);
        if (value == null)
        {
            if (kind == FieldKind.DynamicMap) return new Dictionary<string, object>();  // null → 빈 맵 {}
            if (kind == FieldKind.Array)      return new List<object>();                // null → 빈 배열 []
            return null;
        }

        switch (kind)
        {
            case FieldKind.Scalar:     return SerializeScalar(value);
            case FieldKind.FixedMap:   return SerializeFixedMap(value, type);
            case FieldKind.DynamicMap: return SerializeDynamicMap(value, type);
            case FieldKind.Array:      return SerializeArray(value, type, field);
            default:                   return value;
        }
    }


    public static object DeserializeValue(object raw, Type type, FieldInfo field)
    {
        switch (Classify(type, field))
        {
            case FieldKind.FixedMap:   return DeserializeFixedMap(raw, type);
            case FieldKind.DynamicMap: return DeserializeDynamicMap(raw, type);
            case FieldKind.Array:      return DeserializeArray(raw, type);
            default:                   return DeserializeScalar(raw, type);
        }
    }


    private static FieldKind Classify(Type type, FieldInfo field)
    {
        if (field != null)
        {
            if (field.IsDefined(typeof(FirestoreIgnoreAttribute), true)) return FieldKind.Ignore;
            if (field.IsDefined(typeof(FirestoreMapAttribute), true))
            {

                if (!type.IsArray && type.IsGenericType && IsCollection(type))
                    return FieldKind.DynamicMap;

                if (s_WarnedMapMisuse.Add(field))
                {
                    string hint = IsCollection(type)
                        ? "배열은 List<T>로 바꿔야 동적 맵이 됩니다."
                        : "struct/class는 마커 없이 자동으로 고정 맵 처리되니 어트리뷰트를 제거하세요.";
                    Debug.LogWarning($"[FirestoreMapper] '{field.DeclaringType?.Name}.{field.Name}': [FirestoreMap]은 List<T> 필드 전용입니다. {hint} (타입 기준으로 자동 분류해 계속 진행합니다.)");
                }

            }
        }
        if (type == null) return FieldKind.Scalar;
        if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return FieldKind.Ignore;
        if (IsSubCollectionType(type)) return FieldKind.SubCollection;
        if (IsScalar(type)) return FieldKind.Scalar;
        if (IsCollection(type)) return FieldKind.Array;     
        return FieldKind.FixedMap;                          
    }


    private static object SerializeScalar(object value)
    {
        if (value == null) return null;
        if (value is Enum) return Convert.ToInt64(value);
        return value;
    }


    private static object SerializeFixedMap(object obj, Type type)
    {
        var dict = new Dictionary<string, object>();
        foreach (var f in GetPocoFields(type))
            dict[KeyOf(f)] = SerializeValue(f.GetValue(obj), f.FieldType, f);
        return dict;
    }


    private static object SerializeDynamicMap(object listObj, Type listType)
    {
        var map = new Dictionary<string, object>();
        if (!(listObj is IEnumerable list)) return map;

        Type elem = GetElementType(listType);
        FieldInfo keyField = GetMapKeyField(elem);
        FieldInfo[] valueFields = GetMapValueFields(elem);

        foreach (var item in list)
        {
            if (item == null) continue;
            string key = Convert.ToString(keyField.GetValue(item), CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[FirestoreMapper] {elem.Name}: 맵 키가 비어 원소를 건너뜁니다.");
                continue;
            }
            if (map.ContainsKey(key))
                Debug.LogWarning($"[FirestoreMapper] 중복 키 '{key}' — 마지막 값으로 덮어씁니다.");

            var valDict = new Dictionary<string, object>();
            foreach (var vf in valueFields)
                valDict[KeyOf(vf)] = SerializeValue(vf.GetValue(item), vf.FieldType, vf);
            map[key] = valDict;  
        }
        return map;
    }


    private static object DeserializeFixedMap(object raw, Type type)
    {
        object instance = Activator.CreateInstance(type);   
        if (!(raw is IDictionary<string, object> map)) return instance;

        foreach (var f in GetPocoFields(type))
            if (map.TryGetValue(KeyOf(f), out var v))
                f.SetValue(instance, DeserializeValue(v, f.FieldType, f));
        return instance;
    }


    private static object DeserializeDynamicMap(object raw, Type listType)
    {
        var list = (IList)Activator.CreateInstance(listType);
        if (!(raw is IDictionary<string, object> map)) return list;

        Type elem = GetElementType(listType);
        FieldInfo keyField = GetMapKeyField(elem);
        FieldInfo[] valueFields = GetMapValueFields(elem);

        foreach (var kv in map)
        {
            object item = Activator.CreateInstance(elem);
            keyField.SetValue(item, DeserializeScalar(kv.Key, keyField.FieldType));
            if (kv.Value is IDictionary<string, object> inner)
                foreach (var vf in valueFields)
                    if (inner.TryGetValue(KeyOf(vf), out var vv))
                        vf.SetValue(item, DeserializeValue(vv, vf.FieldType, vf));
            list.Add(item);
        }
        return list;
    }

 
    private static object SerializeArray(object value, Type collectionType, FieldInfo field)
    {
        var result = new List<object>();
        if (!(value is IEnumerable list)) return result;

        Type elemType = GetElementType(collectionType);
        foreach (var item in list)
        {
            if (item == null) { result.Add(null); continue; }

            object s = SerializeValue(item, elemType, null);
            if (s is IList)   // Firestore 는 배열 안 배열을 허용하지 않는다
            {
                if (field == null || s_WarnedNestedArray.Add(field))
                    Debug.LogWarning($"[FirestoreMapper] '{(field != null ? field.DeclaringType?.Name + "." + field.Name : collectionType.Name)}': " +
                                     "Firestore는 배열 안 배열을 허용하지 않습니다 — 해당 원소를 건너뜁니다. " +
                                     "내부 배열을 struct로 감싸면 array-of-maps로 저장할 수 있습니다.");
                continue;
            }
            result.Add(s);
        }
        return result;
    }


    private static object DeserializeArray(object raw, Type collectionType)
    {
        if (raw == null) return DefaultOf(collectionType);

        if (!(raw is IList items))
        {
            Debug.LogWarning($"[FirestoreMapper] array 복원 실패: 서버 값이 배열이 아닙니다({raw.GetType().Name}) → 기본값 유지. 대상: {collectionType.Name}");
            return DefaultOf(collectionType);
        }

        Type elemType = GetElementType(collectionType);

        if (collectionType.IsArray)                                      
        {
            var arr = Array.CreateInstance(elemType, items.Count);
            for (int i = 0; i < items.Count; i++)
                arr.SetValue(DeserializeValue(items[i], elemType, null), i);
            return arr;
        }

        if (collectionType.IsGenericType && typeof(IList).IsAssignableFrom(collectionType))   
        {
            var list = (IList)Activator.CreateInstance(collectionType);
            foreach (var r in items)
                list.Add(DeserializeValue(r, elemType, null));
            return list;
        }

        Debug.LogWarning($"[FirestoreMapper] '{collectionType.Name}': T[]/List<T> 외 컬렉션은 복원을 지원하지 않습니다.");
        return DefaultOf(collectionType);
    }


    private static object DeserializeScalar(object raw, Type target)
    {
        if (raw == null) return DefaultOf(target);
        if (target.IsInstanceOfType(raw)) return raw;

        Type underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (underlying.IsEnum)
        {
            if (raw is string es) return Enum.Parse(underlying, es, true);
            return Enum.ToObject(underlying, Convert.ToInt64(raw));
        }

        try
        {
            return Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
        }
        catch
        {
            Debug.LogWarning($"[FirestoreMapper] 값 변환 실패: {raw?.GetType().Name} → {target.Name}. 기본값 유지.");
            return DefaultOf(target);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // 에디터 검증
    // ──────────────────────────────────────────────────────────────


    public static void ValidateMapKeys(BaseFireStore target)
    {
        foreach (var f in GetMappedFields(target.GetType()))
        {
            if (Classify(f.FieldType, f) != FieldKind.DynamicMap) continue;
            if (!(f.GetValue(target) is IEnumerable list)) continue;

            Type elem = GetElementType(f.FieldType);
            FieldInfo keyField;
            try { keyField = GetMapKeyField(elem); }
            catch (Exception e) { Debug.LogWarning($"[FirestoreMapper] {e.Message}"); continue; }

            var seen = new HashSet<string>();
            foreach (var item in list)
            {
                if (item == null) continue;
                string key = Convert.ToString(keyField.GetValue(item), CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(key)) continue;
                if (!seen.Add(key))
                    Debug.LogWarning($"[FirestoreMapper] '{target.name}'의 '{KeyOf(f)}' 맵에 중복 키 '{key}'가 있습니다. 저장 시 마지막 값만 반영됩니다.");
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


    private static FieldInfo[] GetPocoFields(Type type)
    {
        if (s_PocoCache.TryGetValue(type, out var cached)) return cached;

        var result = new List<FieldInfo>();
        foreach (var f in EnumeratePocoFields(type))
        {
            if (!IsSerialized(f)) continue;
            if (f.IsDefined(typeof(FirestoreIgnoreAttribute), true)) continue;
            if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType)) continue;
            result.Add(f);
        }
        var arr = result.ToArray();
        s_PocoCache[type] = arr;
        return arr;
    }


    private static FieldInfo GetMapKeyField(Type elem)
    {
        if (s_MapKeyCache.TryGetValue(elem, out var cached)) return cached;

        FieldInfo key = null;
        foreach (var f in GetPocoFields(elem))
        {
            if (!f.IsDefined(typeof(FirestoreMapKeyAttribute), true)) continue;
            if (key != null) throw new InvalidOperationException($"{elem.Name}: [FirestoreMapKey]가 둘 이상입니다.");
            key = f;
        }
        if (key == null) throw new InvalidOperationException($"{elem.Name}: [FirestoreMapKey] 필드가 필요합니다(동적 맵 원소).");
        s_MapKeyCache[elem] = key;
        return key;
    }

    private static FieldInfo[] GetMapValueFields(Type elem)
    {
        if (s_MapValueCache.TryGetValue(elem, out var cached)) return cached;

        FieldInfo keyField = GetMapKeyField(elem);
        var result = new List<FieldInfo>();
        foreach (var f in GetPocoFields(elem))
            if (f != keyField) result.Add(f);
        var arr = result.ToArray();
        s_MapValueCache[elem] = arr;
        return arr;
    }


    private static IEnumerable<FieldInfo> EnumerateDeclaredFields(Type type)
    {
        for (Type t = type; t != null && t != typeof(BaseFireStore); t = t.BaseType)
            foreach (var f in t.GetFields(kFlags))
                yield return f;
    }


    private static IEnumerable<FieldInfo> EnumeratePocoFields(Type type)
    {
        for (Type t = type; t != null && t != typeof(object) && t != typeof(ValueType); t = t.BaseType)
            foreach (var f in t.GetFields(kFlags))
                yield return f;
    }

    private static bool IsSerialized(FieldInfo f)
    {
        if (f.IsDefined(typeof(NonSerializedAttribute), false)) return false;
        if (f.IsPublic) return true;
        return f.IsDefined(typeof(SerializeField), false);
    }

    private static bool IsSubCollectionField(FieldInfo f) => IsSubCollectionType(f.FieldType);

    private static bool IsSubCollectionType(Type ft)
    {
        if (typeof(BaseFireStore).IsAssignableFrom(ft)) return true;
        if (ft.IsArray && typeof(BaseFireStore).IsAssignableFrom(ft.GetElementType())) return true;
        if (ft.IsGenericType)
            foreach (var arg in ft.GetGenericArguments())
                if (typeof(BaseFireStore).IsAssignableFrom(arg)) return true;
        return false;
    }


    private static bool IsScalar(Type type)
    {
        Type t = Nullable.GetUnderlyingType(type) ?? type;
        if (t.IsPrimitive) return true;           
        if (t.IsEnum) return true;
        if (t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime)) return true;
        if (t == typeof(Timestamp)) return true;
        return false;
    }


    private static bool IsCollection(Type type)
    {
        if (type == typeof(string)) return false;
        if (type.IsArray) return true;
        return typeof(IEnumerable).IsAssignableFrom(type);
    }


    private static Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray) return collectionType.GetElementType();
        if (collectionType.IsGenericType) return collectionType.GetGenericArguments()[0];
        return typeof(object);
    }

    private static object DefaultOf(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;
}
