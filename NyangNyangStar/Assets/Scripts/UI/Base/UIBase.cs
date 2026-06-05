using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UI.Base
{
    public abstract class UIBase : MonoBehaviour
    {
        protected Dictionary<Type, Object[]> _objects = new();

        public abstract void Init();

        protected void Bind<T>(Type type) where T : Object
        {
            string[] names = Enum.GetNames(type);
            
            Object[] objects = new Object[names.Length];
            _objects[typeof(T)] = objects;

            for (int i = 0; i < names.Length; i++)
            {
                if(typeof(T) == typeof(GameObject))
                    objects[i] = FindChild(gameObject, names[i], true);
                else 
                    objects[i] = FindChild<T>(gameObject, names[i], true);
                
                if(objects[i] == null)
                    DebugTool.Warning($"Failed to find child {names[i]} on {gameObject}", DebugType.Game);
            }
        }

        protected T Get<T>(int index) where T : Object
        {
            if (!_objects.TryGetValue(typeof(T), out Object[] objects))
            {
                DebugTool.Warning($"{typeof(T).Name} 타입이 바인딩되지 않았습니다.",  DebugType.Game);
                return null;
            }

            if (index < 0 || index >= objects.Length)
            {
                DebugTool.Log($"{typeof(T).Name} 배열 범위를 벗어난 인덱스 입니다." +
                              $"Index : {index}, Length : {objects.Length}", DebugType.Game);
                return null;
            }

            if (objects[index] == null)
            {
                DebugTool.Warning($"{typeof(T).Name} 객체가 null 입니다. Index: {index}", DebugType.Game);
                return null;
            }

            return objects[index] as T;
        }
        
        protected GameObject GetObject(int idx) { return Get<GameObject>(idx); }
        protected TMP_Text GetText(int idx) { return Get<TMP_Text>(idx); }
        protected Button GetButton(int idx) { return Get<Button>(idx); }
        protected Image GetImage(int idx) { return Get<Image>(idx); }

        public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : Object
        {
            if (go == null)
                return null;

            if (recursive == false)
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    Transform transform = go.transform.GetChild(i);

                    if (IsNameMatched(transform.name, name))
                    {
                        T component = transform.GetComponent<T>();

                        if (component != null)
                            return component;
                    }
                }
            else
                foreach (T component in go.GetComponentsInChildren<T>(true))
                    if (IsNameMatched(component.name, name))
                        return component;

            return null;
        }
    
        public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
        {
            Transform transform = FindChild<Transform>(go, name, recursive);
            if (transform == null)
                return null;
            
            return transform.gameObject;
        }
        
        private static string RemoveWhiteSpace(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
        }

        private static bool IsNameMatched(string targetName, string searchName)
        {
            if (string.IsNullOrEmpty(searchName))
                return true;

            return RemoveWhiteSpace(targetName) == RemoveWhiteSpace(searchName);
        }
    }
}