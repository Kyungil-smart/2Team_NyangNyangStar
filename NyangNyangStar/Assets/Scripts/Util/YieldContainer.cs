using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 코루틴에서 자주 사용하는 YieldInstruction을 캐싱해서 재사용하는 클래스
/// </summary>
public static class YieldContainer
{
    private static readonly Dictionary<float, WaitForSeconds> _seconds = new();
    private static readonly WaitForEndOfFrame _endOfFrame = new();

    /// <summary>
    /// 지정한 시간만큼 대기하는 WaitForSeconds를 반환한다.
    /// 이미 생성된 시간이면 캐시에서 가져오고, 없으면 새로 생성한다.
    /// </summary>
    /// <param name="seconds">대기 시간</param>
    /// <returns>지정한 시간의 WaitForSeconds</returns>
    public static WaitForSeconds Seconds(float seconds)
    {
        if (!_seconds.TryGetValue(seconds, out WaitForSeconds waitForSeconds))
        {
            waitForSeconds = new WaitForSeconds(seconds);
            _seconds.Add(seconds, waitForSeconds);
        }

        return waitForSeconds;
    }

    /// <summary>
    /// 0초부터 지정한 시간 사이의 랜덤 대기 시간을 반환한다.
    /// 캐시가 과도하게 늘어나지 않도록 0.01초 단위로 반올림한다.
    /// </summary>
    /// <param name="seconds">최대 대기 시간</param>
    /// <returns>랜덤 대기 시간의 WaitForSeconds</returns>
    public static WaitForSeconds RandSeconds(float seconds)
    {
        float random = Random.Range(0f, seconds);
        float value = Mathf.Round(random * 100f) / 100f;

        return Seconds(value);
    }

    /// <summary>
    /// 현재 프레임의 렌더링이 끝난 뒤까지 대기하는 WaitForEndOfFrame을 반환한다.
    /// </summary>
    /// <returns>캐싱된 WaitForEndOfFrame</returns>
    public static WaitForEndOfFrame EndOfFrame()
    {
        return _endOfFrame;
    }
}