using System;
using System.Collections.Generic;
using System.Text;

namespace AndroidBot
{
    public static class Utils
    {
        private static Random random = new Random();

        public static T PickRandom<T>(this IList<T> collection) where T : class
        {
            if (collection == null)
                return null;
            if (collection.Count == 0)
                return null;
            return collection[random.Next(0, collection.Count)];
        }
    }
}