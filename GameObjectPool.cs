using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Utilities
{
    // 프리팹 기반 GameObject 재사용 풀. StaticObjectPool과 달리 GameObject/MonoBehaviour는
    // Activator.CreateInstance로 만들 수 없으므로, 프리팹 원본을 키로 Instantiate/SetActive를
    // 재사용하는 방식으로 별도 관리한다. 맵, 유닛/좀비 등 3D 오브젝트 생성 전반에 공용으로 사용한다.
    public static class GameObjectPool
    {
        private static readonly Dictionary<GameObject, Stack<GameObject>> _prefabPools = new Dictionary<GameObject, Stack<GameObject>>();
        private static readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

        /// <summary>풀에 재사용 가능한 인스턴스가 있으면 꺼내 재활성화하고, 없으면 새로 Instantiate한다.</summary>
        public static GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            if (null == prefab)
            {
                return null;
            }

            GameObject instance = null;

            if (_prefabPools.TryGetValue(prefab, out Stack<GameObject> stack) && 0 < stack.Count)
            {
                instance = stack.Pop();
                instance.transform.SetParent(parent, false);
                instance.SetActive(true);
            }
            else
            {
                instance = Object.Instantiate(prefab, parent);
                _instanceToPrefab[instance] = prefab;
            }

            return instance;
        }

        /// <summary>인스턴스를 파괴하지 않고 비활성화한 뒤 풀에 반납한다.</summary>
        public static void Despawn(GameObject instance)
        {
            if (null == instance)
            {
                return;
            }

            if (!_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
            {
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);

            if (!_prefabPools.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                _prefabPools.Add(prefab, stack);
            }

            stack.Push(instance);
        }

        /// <summary>
        /// prefab을 position에 스폰하고, ParticleSystem 재생이 끝나면(main.duration + 파티클 최대 수명) 자동으로
        /// 풀에 반납한다. 피격 이펙트(DamageEffect)처럼 한 번 재생되고 알아서 사라져야 하는 연출용.
        /// prefab에 ParticleSystem이 없으면 기본 1초 뒤 반납한다.
        /// scale: 인스턴스의 localScale 배율(예: UnitModel/EnemyModel.EffectScale). 1이면 프리팹 원본 크기.
        /// 반환값: 스폰된 인스턴스(추가로 손댈 일 없으면 무시해도 됨). prefab이 null이면 null을 반환한다.
        /// </summary>
        public static GameObject SpawnOneShotEffect(GameObject prefab, Vector3 position, float scale = 1f)
        {
            if (null == prefab)
            {
                return null;
            }

            GameObject instance = Spawn(prefab);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * scale;

            // 풀에서 재사용되는 인스턴스는 이전 회전값(예: 발사체 방향)이 남아있을 수 있다.
            // Y축 회전만 0으로 초기화한다(X/Z는 프리팹이 의도한 기울기일 수 있어 그대로 둔다).
            Vector3 euler = instance.transform.eulerAngles;
            instance.transform.eulerAngles = new Vector3(euler.x, 0f, euler.z);

            ParticleSystem particleSystem = instance.GetComponentInChildren<ParticleSystem>();
            float lifetime = null != particleSystem ? particleSystem.main.duration + particleSystem.main.startLifetime.constantMax : 1f;

            DespawnAfterSecondsAsync(instance, lifetime).Forget();

            return instance;
        }

        private static async UniTaskVoid DespawnAfterSecondsAsync(GameObject instance, float seconds)
        {
            await UniTask.Delay(Mathf.RoundToInt(seconds * 1000f), cancellationToken: instance.GetCancellationTokenOnDestroy());
            Despawn(instance);
        }

        /// <summary>
        /// 해당 프리팹으로 생성된 풀 인스턴스를 전부 파괴하고 풀 자체를 제거한다.
        /// 아직 반납되지 않고 활성 상태로 남아있는(예: 씬 전환 시점에 날아다니던 총알 등) 인스턴스도
        /// 함께 찾아 파괴한다 — 그렇지 않으면 _instanceToPrefab에 파괴된 오브젝트를 가리키는 키가
        /// 영구히 남아 static 딕셔너리가 계속 누적된다.
        /// Addressables 프리팹은 핸들 Release 전에 반드시 호출해, 언로드된 에셋을 참조하는
        /// 풀 인스턴스가 남지 않도록 한다.
        /// </summary>
        public static void ClearPool(GameObject prefab)
        {
            if (null == prefab)
            {
                return;
            }

            if (_prefabPools.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                while (0 < stack.Count)
                {
                    GameObject instance = stack.Pop();
                    _instanceToPrefab.Remove(instance);
                    Object.Destroy(instance);
                }

                _prefabPools.Remove(prefab);
            }

            List<GameObject> activeInstances = null;
            foreach (KeyValuePair<GameObject, GameObject> pair in _instanceToPrefab)
            {
                if (prefab != pair.Value)
                {
                    continue;
                }
                if (null == activeInstances)
                {
                    activeInstances = new List<GameObject>();
                }
                activeInstances.Add(pair.Key);
            }

            if (null != activeInstances)
            {
                for (int i = 0; i < activeInstances.Count; ++i)
                {
                    GameObject instance = activeInstances[i];
                    _instanceToPrefab.Remove(instance);
                    if (null != instance)
                    {
                        Object.Destroy(instance);
                    }
                }
            }
        }
    }
}
