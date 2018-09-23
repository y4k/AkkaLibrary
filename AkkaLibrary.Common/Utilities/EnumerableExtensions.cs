using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AkkaLibrary.Common.Utilities
{
    /// <summary>
    /// Extensions to <see cref="IEnumerable"/>
    /// </summary>
    public static class EnumerableExtension
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static T SelectAtRandom<T>(this IEnumerable<T> enumerable)
            => enumerable.ElementAt(_random.Next(0, enumerable.Count()));

        /// <summary>
        /// Removes an item from the collection at random and returns both the
        /// updated collection and the removed item
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <param name="removed">The removed item</param>
        /// <returns>The updated collection</returns>
        public static T RemoveAtRandom<T, TData>(this T collection, out TData removed) where T : ICollection<TData>
        {
            removed = collection.ElementAt(_random.Next(0, collection.Count));
            collection.Remove(removed);
            return collection;
        }

        /// <summary>
        /// Removes an item from the collection at random and return only the
        /// updated collection
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <returns>The updated collection</returns>
        public static T RemoveAtRandom<T, TData>(this T collection) where T : ICollection<TData>
        {
            return collection.RemoveAtRandom(out TData _);
        }
    }
}