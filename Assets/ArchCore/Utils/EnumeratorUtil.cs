using System.Collections;
using System.Collections.Generic;

namespace ArchCore.Utils
{
    public static class EnumeratorUtil
    {
        public static IEnumerator<T> Single<T>(T value)
        {
            while (true)
            {
                yield return value;
            }
        }
    }
}