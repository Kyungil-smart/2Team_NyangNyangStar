using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Data.Parsing
{
    [Serializable]
    public struct SheetData
    {
        [field: SerializeField] public string URL { get; private set; }
        [field: SerializeField] public SheetType Type { get; private set; }
        public char SplitSymbol => Type == SheetType.CSV ? ',' : '\t';


        public SheetData(string url, SheetType type)
        {
            URL = url;
            Type = type;
        }

        public IEnumerator Load(Action<char, string[]> SuccessCallback)
        {
            if (string.IsNullOrEmpty(URL))
            {
                DebugTool.Error("URL이 비어있습니다.", DebugType.Data);
                yield break;
            }
            string sheetId = ExtractSheetId(URL);
            string gid = ExtractGid(URL);
            
            if(string.IsNullOrEmpty(sheetId) || string.IsNullOrEmpty(gid))
            {
                DebugTool.Error("URL에서 Sheet ID를 찾을 수 없습니다. URL 형식을 확인해주세요.", DebugType.Data);
                yield break;
            }

            string format = Type == SheetType.CSV ? "csv" : "tsv";

            string exportURL = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format={format}&gid={gid}";

            using (UnityWebRequest uwr = UnityWebRequest.Get(exportURL))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    DebugTool.Error($"Failed to load sheet data from {exportURL} : {uwr.error}", DebugType.Data);

                    yield break;
                }

                string sheetDataText = uwr.downloadHandler.text;

                // \r (캐리지 리턴) 문제 방지 및 빈 줄 무시
                string[] lines = sheetDataText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                SuccessCallback?.Invoke(SplitSymbol, lines);
                DebugTool.Log($"시트 데이터 로드 성공 {exportURL}", DebugType.Data);
            }
            // --- URL 추출용 헬퍼 메서드 ---
        
            string ExtractSheetId(string url)
            {
                // "/d/" 뒤에 오는 영문/숫자/기호 조합을 추출
                var match = Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
                return match.Success ? match.Groups[1].Value : string.Empty;
            }

            string ExtractGid(string url)
            {
                // "gid=" 뒤에 오는 숫자 조합을 추출
                var match = Regex.Match(url, @"[#&?]?gid=([0-9]+)");
            
                // gid가 포함되지 않은 URL이라면 기본값인 "0"(첫 번째 시트)을 반환
                return match.Success ? match.Groups[1].Value : "0"; 
            }
        }
    }
    
    public enum SheetType
    {
        CSV,
        JSON,
        TSV
    }
}