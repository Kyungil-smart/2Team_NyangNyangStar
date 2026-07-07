using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    // NyangQuariumMergeBoardBinder -> 씬에 NyangQuariumMergeBoard 프리팹이 뜨면 런타임 컴포넌트 자동 부착
    public sealed class NyangQuariumMergeBoardBinder : MonoBehaviour
    {
        private const string RootName = "NyangQuariumMergeBoard";
        private const string GeneratorName = "Item Generator Button";
        private const string BoardName = "Item Board";
        private const string InfoName = "ItemInfo";
        private const string ClearAllButtonName = "All Item Sell Button";
        private const string RewardRootName = "Reward Root";
        private const string QuestBoardPanelName = "QuestBoardPanel";
        private const string LevelUpButtonName = "LevelUp Button";
        private static NyangQuariumMergeBoardBinder _instance;

        // 게임 시작, 씬 로드 시 부트스트랩 러너 등록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureRunner();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRunner();
        }

        // DontDestroyOnLoad 러너가 없으면 하나 만들어 둠
        private static void EnsureRunner()
        {
            if (_instance != null)
                return;

            GameObject runner = new("@NyangQuariumMergeBoardBinder");
            DontDestroyOnLoad(runner);
            _instance = runner.AddComponent<NyangQuariumMergeBoardBinder>();
        }

        private void OnEnable()
        {
            StartCoroutine(WatchMergeBoard());
        }

        // 매 프레임 씬에 머지보드가 있는지 감시해서 Init 해줌
        private IEnumerator WatchMergeBoard()
        {
            while (true)
            {
                BootstrapCurrentScene();
                yield return null;
            }
        }

        // 프리팹 자식 이름 기준으로 Board / Generator / Info / Navigation 연결
        private static void BootstrapCurrentScene()
        {
            GameObject rootObject = FindActiveGameObject(RootName);
            if (rootObject == null)
                return;

            GameObject generatorObject = FindChildGameObject(rootObject.transform, GeneratorName);
            GameObject boardObject = FindChildGameObject(rootObject.transform, BoardName);
            GameObject infoObject = FindChildGameObject(rootObject.transform, InfoName);
            GameObject rewardRootObject = FindChildGameObject(rootObject.transform, RewardRootName);

            if (generatorObject == null || boardObject == null || infoObject == null)
                return;

            NyangQuariumItemInfoPanel infoPanel = infoObject.GetComponent<NyangQuariumItemInfoPanel>();
            if (infoPanel == null)
                infoPanel = infoObject.AddComponent<NyangQuariumItemInfoPanel>();

            NyangQuariumItemBoard board = boardObject.GetComponent<NyangQuariumItemBoard>();
            if (board == null)
                board = boardObject.AddComponent<NyangQuariumItemBoard>();

            board.Init(infoPanel);

            GameObject clearAllButtonObject = FindChildGameObject(rootObject.transform, ClearAllButtonName);
            if (clearAllButtonObject != null)
            {
                NyangQuariumClearAllButton clearAllButton =
                    clearAllButtonObject.GetComponent<NyangQuariumClearAllButton>();

                if (clearAllButton == null)
                    clearAllButton = clearAllButtonObject.AddComponent<NyangQuariumClearAllButton>();

                clearAllButton.Init(board);
            }

            NyangQuariumRewardQueue rewardQueue = null;
            if (rewardRootObject != null)
            {
                rewardQueue = rewardRootObject.GetComponent<NyangQuariumRewardQueue>();
                if (rewardQueue == null)
                    rewardQueue = rewardRootObject.AddComponent<NyangQuariumRewardQueue>();

                rewardQueue.Init(board);
            }

            NyangQuariumItemGenerator generator = generatorObject.GetComponent<NyangQuariumItemGenerator>();
            if (generator == null)
                generator = generatorObject.AddComponent<NyangQuariumItemGenerator>();

            generator.Init(board, rewardQueue);

            NyangQuariumMergeBoardNavigation navigation = rootObject.GetComponent<NyangQuariumMergeBoardNavigation>();
            if (navigation == null)
                navigation = rootObject.AddComponent<NyangQuariumMergeBoardNavigation>();

            navigation.Init();

            GameObject levelUpButtonObject = FindChildGameObject(rootObject.transform, LevelUpButtonName);
            if (levelUpButtonObject != null)
            {
                NyangQuariumMergeBoardLevelUpButton levelUpButton =
                    levelUpButtonObject.GetComponent<NyangQuariumMergeBoardLevelUpButton>();

                if (levelUpButton == null)
                    levelUpButton = levelUpButtonObject.AddComponent<NyangQuariumMergeBoardLevelUpButton>();

                levelUpButton.Init();
            }

            GameObject questBoardPanelObject = FindChildGameObject(rootObject.transform, QuestBoardPanelName);
            if (questBoardPanelObject != null)
            {
                NyangQuariumMergeQuestBoardUI questBoardUI =
                    questBoardPanelObject.GetComponent<NyangQuariumMergeQuestBoardUI>();

                if (questBoardUI == null)
                    questBoardUI = questBoardPanelObject.AddComponent<NyangQuariumMergeQuestBoardUI>();

                questBoardUI.Init();
            }
        }

        // 활성화된 자식 오브젝트만 이름으로 찾음
        private static GameObject FindChildGameObject(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == objectName)
                    return child.gameObject;
            }

            return null;
        }

        // 씬 전체에서 이름으로 루트 오브젝트 검색
        private static GameObject FindActiveGameObject(string objectName)
        {
            GameObject[] objects = FindObjectsOfType<GameObject>();

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject target = objects[i];
                if (target != null && target.name == objectName)
                    return target;
            }

            return null;
        }
    }
}
