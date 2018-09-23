using System.Collections.Generic;
using NETCoreAsio.DataStructures;

namespace AkkaLibrary.Common.Utilities
{
    public static class CollectionExtensions
    {
        public static T Head<T>(this CircularQueue<T> queue)
        {
            return queue[0];
        }

        public static T Second<T>(this CircularQueue<T> queue)
        {
            return queue[1];
        }

        public static bool TryDequeue<T>(this Queue<T> queue, out T value)
        {
            value = default(T);
            if(queue.Count == 0)
            {
                return false;
            }
            value = queue.Dequeue();
            return true;
        }

        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> kvp, out T1 out1, out T2 out2)
        {
            out1 = kvp.Key;
            out2 = kvp.Value;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
            => new HashSet<T>(source, comparer);
    }
}