using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    internal static class NyangQuariumQuestBoardTween
    {
        private const float QuestBoardMoveDuration = 0.3f;
        private const float QuestBoardPunchDuration = 0.2f;
        private const float QuestBoardPunchScale = 0.08f;
        private const float QuestBoardRenewEnterPadding = 40f;

        public static Sequence PlayOrderChange(
            Sequence currentSequence,
            Transform movedTransform,
            Action applyOrderChange,
            bool punchMovedBoard,
            Action onComplete)
        {
            if (movedTransform == null || applyOrderChange == null)
                return currentSequence;

            RectTransform movedRect = movedTransform as RectTransform;
            RectTransform parentRect = movedTransform.parent as RectTransform;

            if (movedRect == null || parentRect == null)
            {
                applyOrderChange.Invoke();
                return currentSequence;
            }

            currentSequence?.Complete();
            currentSequence?.Kill();

            List<RectTransform> rects = CollectDirectQuestBoardRects(parentRect);
            Dictionary<RectTransform, Vector2> startPositions = CaptureAnchoredPositions(rects);
            LayoutGroup layoutGroup = parentRect.GetComponent<LayoutGroup>();

            // 정렬 후 목표 위치 계산
            applyOrderChange.Invoke();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            Canvas.ForceUpdateCanvases();

            Dictionary<RectTransform, Vector2> targetPositions = CaptureAnchoredPositions(rects);
            int finalSiblingIndex = movedTransform.GetSiblingIndex();

            // 시작 위치로 복귀 후 Tween 시작
            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rect = rects[i];
                if (rect != null && startPositions.TryGetValue(rect, out Vector2 position))
                    rect.anchoredPosition = position;
            }

            if (layoutGroup != null)
                layoutGroup.enabled = false;

            movedTransform.SetAsLastSibling();

            // 보드 이동 Tween 생성
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null || !targetPositions.TryGetValue(rect, out Vector2 targetPosition))
                    continue;

                sequence.Join(
                    rect.DOAnchorPos(targetPosition, QuestBoardMoveDuration)
                        .SetEase(Ease.OutCubic));
            }

            if (punchMovedBoard)
            {
                // 완료 보드 강조 연출
                sequence.Join(
                    movedRect.DOPunchScale(
                        Vector3.one * QuestBoardPunchScale,
                        QuestBoardPunchDuration,
                        8,
                        0.5f));
            }

            sequence.OnComplete(() =>
            {
                // 레이아웃 복구 완료
                movedTransform.SetSiblingIndex(finalSiblingIndex);

                if (layoutGroup != null)
                    layoutGroup.enabled = true;

                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                onComplete?.Invoke();
            });

            return sequence;
        }

        public static Sequence PlayRefreshFromRight(
            Sequence currentSequence,
            Transform renewedTransform,
            Action applyRefresh,
            Action onComplete)
        {
            if (renewedTransform == null || applyRefresh == null)
                return currentSequence;

            RectTransform renewedRect = renewedTransform as RectTransform;
            RectTransform parentRect = renewedTransform.parent as RectTransform;

            if (renewedRect == null || parentRect == null)
            {
                applyRefresh.Invoke();
                return currentSequence;
            }

            currentSequence?.Complete();
            currentSequence?.Kill();

            List<RectTransform> rects = CollectDirectQuestBoardRects(parentRect);
            Dictionary<RectTransform, Vector2> startPositions = CaptureAnchoredPositions(rects);
            LayoutGroup layoutGroup = parentRect.GetComponent<LayoutGroup>();

            applyRefresh.Invoke();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            Canvas.ForceUpdateCanvases();

            rects = CollectDirectQuestBoardRects(parentRect);
            Dictionary<RectTransform, Vector2> targetPositions = CaptureAnchoredPositions(rects);

            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null)
                    continue;

                if (rect == renewedRect &&
                    targetPositions.TryGetValue(rect, out Vector2 targetPosition))
                {
                    rect.anchoredPosition = GetRightEnterPosition(renewedRect, targetPosition);
                    continue;
                }

                if (startPositions.TryGetValue(rect, out Vector2 startPosition))
                    rect.anchoredPosition = startPosition;
            }

            if (layoutGroup != null)
                layoutGroup.enabled = false;

            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null || !targetPositions.TryGetValue(rect, out Vector2 targetPosition))
                    continue;

                sequence.Join(
                    rect.DOAnchorPos(targetPosition, QuestBoardMoveDuration)
                        .SetEase(Ease.OutCubic));
            }

            sequence.OnComplete(() =>
            {
                if (layoutGroup != null)
                    layoutGroup.enabled = true;

                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                onComplete?.Invoke();
            });

            return sequence;
        }

        private static List<RectTransform> CollectDirectQuestBoardRects(RectTransform parentRect)
        {
            List<RectTransform> rects = new();

            if (parentRect == null)
                return rects;

            for (int i = 0; i < parentRect.childCount; i++)
            {
                RectTransform child = parentRect.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.activeSelf)
                    rects.Add(child);
            }

            return rects;
        }

        private static Dictionary<RectTransform, Vector2> CaptureAnchoredPositions(
            List<RectTransform> rects)
        {
            Dictionary<RectTransform, Vector2> positions = new();

            if (rects == null)
                return positions;

            for (int i = 0; i < rects.Count; i++)
            {
                RectTransform rect = rects[i];
                if (rect != null)
                    positions[rect] = rect.anchoredPosition;
            }

            return positions;
        }

        private static Vector2 GetRightEnterPosition(RectTransform rect, Vector2 targetPosition)
        {
            float width = rect != null ? rect.rect.width : 0f;
            return targetPosition + Vector2.right * (width + QuestBoardRenewEnterPadding);
        }
    }
}
