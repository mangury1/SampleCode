using System;
using System.Collections.Generic;
using UnityEngine;

// UnityEngine.EventSystems.EventSystem(UGUI 입력 시스템)과 이름이 겹치므로
// 별도 네임스페이스로 분리해 둔다. 사용할 때는 GameCore.EventSystem으로 명시할 것.
namespace GameCore
{

public abstract class GameEvent
{
    public abstract void Reset();
    public bool IsSuccess;
} // 기본 게임 이벤트 클래스

public static class EventSystem
{
    // s_Events: 타입으로 액션 관리. 특정 타입의 이벤트가 발생했을 때 실행할 액션 관리
    // s_EventLookups: 델리게이트로 액션 관리. 특정 델리게이트(리스너)의 추가, 제거 및 호출할 액션 관리
    private static readonly Dictionary<Type, Action<GameEvent>> s_Events = new Dictionary<Type, Action<GameEvent>>();
    private static readonly Dictionary<Delegate, Action<GameEvent>> s_EventLookups = new Dictionary<Delegate, Action<GameEvent>>();

    // 현재 Broadcast가 진행 중인 이벤트 타입 집합. 같은 타입이 디스패치 중에 재진입(재귀 Broadcast)하는 것을 감지하기 위함.
    // (이벤트 인스턴스를 싱글턴으로 재사용하는 구조에서는, 디스패치 중에 같은 타입을 다시 Broadcast하면
    //  아직 처리 중인 호출의 필드가 덮어써져 데이터가 손상되기 때문에 이를 막아야 한다.)
    private static readonly HashSet<Type> s_Dispatching = new HashSet<Type>();

    public static void AddListener<T>(Action<T> evt) where T : GameEvent
    {
        if (null == evt)
        {
            throw new ArgumentNullException(nameof(evt), "Listener cannot be null");
        }

        if (!s_EventLookups.ContainsKey(evt))
        {
            Action<GameEvent> newAction = (e) => evt((T)e); // 새로운 액션 생성
            s_EventLookups[evt] = newAction; // 델리게이트와 액션을 딕셔너리에 추가

            if (s_Events.TryGetValue(typeof(T), out Action<GameEvent> internalAction))
            {
                s_Events[typeof(T)] = internalAction += newAction; // 기존 액션에 새로운 액션 추가
            }
            else
            {
                s_Events[typeof(T)] = newAction; // 새로운 타입과 액션 추가
            }
        }
        else
        {
            Debug.LogWarning("Listener is already registered for this event.");
        }
    }

    public static void RemoveListener<T>(Action<T> evt) where T : GameEvent
    {
        if (null == evt)
        {
            throw new ArgumentNullException(nameof(evt), "Listener cannot be null");
        }

        if (s_EventLookups.TryGetValue(evt, out Action<GameEvent> action))
        {
            if (s_Events.TryGetValue(typeof(T), out Action<GameEvent> tempAction))
            {
                tempAction -= action; // 액션 제거
                if (null == tempAction)
                {
                    s_Events.Remove(typeof(T)); // 액션이 없으면 타입 제거
                }
                else
                {
                    s_Events[typeof(T)] = tempAction; // 업데이트된 액션 저장
                }
            }

            s_EventLookups.Remove(evt); // 델리게이트 제거
        }
        else
        {
            Debug.LogWarning("Listener does not exist.");
        }
    }

    public static void Broadcast(GameEvent evt)
    {
        Type type = evt.GetType();
        if (!s_Events.TryGetValue(type, out Action<GameEvent> action) || null == action)
        {
            return;
        }

        // 재진입 감지: 같은 타입이 이미 디스패치 중이면 공유 이벤트 인스턴스 손상을 막기 위해 거부한다.
        if (!s_Dispatching.Add(type))
        {
            Debug.LogError($"[EventSystem] {type} 이벤트가 자신의 리스너 안에서 재귀적으로 Broadcast 되었습니다. " +
                           "공유 이벤트 인스턴스가 손상되므로 이번 호출은 무시합니다. 호출부를 확인하세요.");
            return;
        }

        try
        {
            // 리스너를 개별적으로 호출해, 하나가 예외를 던져도 나머지 리스너는 영향받지 않게 한다.
            foreach (Action<GameEvent> listener in action.GetInvocationList())
            {
                try
                {
                    listener.Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception during {type} broadcast: {ex.Message} StackTrace: {ex.StackTrace}");
                }
            }

            evt.Reset();
        }
        finally
        {
            s_Dispatching.Remove(type);
        }
    }

    public static void Broadcast(GameEvent evt, Action<GameEvent> callback)
    {
        Type type = evt.GetType();
        if (!s_Events.TryGetValue(type, out Action<GameEvent> action) || null == action)
        {
            return;
        }

        if (!s_Dispatching.Add(type))
        {
            Debug.LogError($"[EventSystem] {type} 이벤트가 자신의 리스너 안에서 재귀적으로 Broadcast 되었습니다. " +
                           "공유 이벤트 인스턴스가 손상되므로 이번 호출은 무시합니다. 호출부를 확인하세요.");
            return;
        }

        try
        {
            foreach (Action<GameEvent> listener in action.GetInvocationList())
            {
                try
                {
                    listener.Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception during {type} broadcast: {ex.Message} StackTrace: {ex.StackTrace}");
                }
            }

            try
            {
                callback?.Invoke(evt); // 추가 콜백 호출
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during {type} broadcast callback: {ex.Message} StackTrace: {ex.StackTrace}");
            }

            evt.Reset();
        }
        finally
        {
            s_Dispatching.Remove(type);
        }
    }

    public static void Clear()
    {
        s_Events.Clear();
        s_EventLookups.Clear();
        s_Dispatching.Clear();
    }
}

} // namespace GameCore
