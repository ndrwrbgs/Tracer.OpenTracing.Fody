
namespace Tracer.OpenTracing.Util
{
    using System;

    using JetBrains.Annotations;

    /// <summary>
    /// Performance optimizations for Array (borrowed from FastLinq but without importing the dependency)
    /// </summary>
    internal static class ArrayExtensions
    {
        [CanBeNull]
        public static T FirstOrDefault<T>(
            [ItemNotNull] this T[] source,
            Func<T, bool> predicate)
            where T : class
        {
            var len = source.Length;
            for (int index = 0; index < len; index++)
            {
                var itemAtIndex = source[index];
                if (predicate(itemAtIndex))
                {
                    return itemAtIndex;
                }
            }

            return null;
        }
    }
}