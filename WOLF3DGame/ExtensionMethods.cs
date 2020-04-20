using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3D.WOLF3DGame
{
    public static class ExtensionMethods
    {
        public static T Random<T>(this IEnumerable<T> list) => list.ToArray().Random();
        public static T Random<T>(this T[] array) => array[Main.Random.Next(0, array.Length - 1)];
    }
}
