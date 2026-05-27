using Data.LibrarySystem;
using Data.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;


public class DataManager : ISubManager
{
    private GameObject _root;
    
    public void Init()
    {
        _root = GameObject.Find("@Data");
            
        if (_root == null)
        {
            _root = new GameObject { name = "@Data" };
            Object.DontDestroyOnLoad(_root);
        }

        DebugTool.Log("데이터 매니저 초기화 완료", DebugType.Game);
    }

    public void Clear()
    {
        if (_root == null) 
            return;
        
        Object.Destroy(_root);
        _root = null;
        
        DebugTool.Log("데이터 매니저 제거 완료", DebugType.Game);
    }
}
