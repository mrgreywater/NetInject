namespace NetInject.Utils {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    public static class EnumerableExtensions {
        /// <summary>
        ///     Executes an action for each element in the enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            foreach (T o in source)
                action(o);
        }
        /// <summary>
        ///     Sorts an enumerable with a specific key selector
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        public static void Sort<TSource, TKey>(this ICollection<TSource> source, Func<TSource, TKey> keySelector) {
            List<TSource> sortedList = source.OrderBy(keySelector).ToList();
            source.Clear();
            foreach (TSource sortedItem in sortedList)
                source.Add(sortedItem);
        }
    }
}