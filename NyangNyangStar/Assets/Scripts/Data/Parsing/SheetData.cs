using System;
using System.Collections;
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

        public bool loadComplete;

        public SheetData(string url, SheetType type)
        {
            URL = url;
            Type = type;
            loadComplete = false;
        }

        public IEnumerator Load(Action<char, string[]> SuccessCallback)
        {
            string sheetId = URL.Split("d/")[1].Split('/')[0];

            string gid = URL.Split("gid=")[1].Split('&')[0].Split('#')[0];

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

                string[] lines = sheetDataText.Split('\n');

                SuccessCallback?.Invoke(SplitSymbol, lines);
                loadComplete = true;
                DebugTool.Log($"Successfully loaded sheet data from {exportURL}", DebugType.Data);
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