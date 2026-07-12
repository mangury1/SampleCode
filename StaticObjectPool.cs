using System;
using System.Collections.Generic;

namespace Utilities
{
    // 타입별로 인스턴스를 재사용하는 정적 오브젝트 풀.
    // List/Dictionary 등 GC 부담이 큰 참조형 객체를 매번 새로 생성하지 않고 재사용하기 위해 사용한다.
    static class StaticObjectPool
    {
        // 타입 E 전용 스택. 제네릭 인스턴스마다 별도의 static 필드가 생성되어 타입별로 분리 보관된다.
        private static class Pool<E> where E : class
        {
            private static readonly Stack<E> _pool = new();

            public static int Lent { get; private set; }
            public static int Return { get; private set; }

            // 인터페이스/추상 클래스는 인스턴스화할 수 없으므로 타입당 최초 1회만 검사한다.
            static Pool()
            {
                if (typeof(E).IsInterface)
                {
                    throw new Exception("Interface!!");
                }

                if (typeof(E).IsAbstract)
                {
                    throw new Exception("Abstract!!");
                }
            }

            public static void Push(E obj)
            {
                if (null == obj)
                {
                    return;
                }

                lock (_pool)
                {
                    ++Return;
                    _pool.Push(obj);
                }
            }

            public static E Pop()
            {
                lock (_pool)
                {
                    ++Lent;
                    return 0 < _pool.Count ? _pool.Pop() : Activator.CreateInstance<E>();
                }
            }
        }

        // 지금까지 풀링에 사용된 타입 목록. 디버그/통계 용도.
        private static readonly HashSet<Type> _usedTypes = new();
        public static ICollection<Type> UsedTypes => _usedTypes;

        public static void Push<E>(E obj) where E : class
        {
            _usedTypes.Add(typeof(E));
            Pool<E>.Push(obj);
        }

        public static E Pop<E>() where E : class
        {
            _usedTypes.Add(typeof(E));
            return Pool<E>.Pop();
        }

        // using 블록을 벗어날 때 Pop한 객체를 자동으로 Push해 반납을 보장한다.
        // 주의: ref struct이므로 async 메서드의 await 구간을 넘겨서는 사용할 수 없다.
        public ref struct RAII<E> where E : class
        {
            private E _data;

            public RAII(out E outData)
            {
                _data = Pop<E>();
                outData = _data;
            }

            public void Dispose()
            {
                Push(_data);
                _data = null;
            }
        }
    }
}
