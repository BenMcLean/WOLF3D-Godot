using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame
{
	public static class ExtensionMethods
	{
		public static T Random<T>(this List<T> list) =>
			list[Main.RNG.Next(0, list?.Count ?? 0)];
		public static T Random<T>(this IEnumerable<T> list) =>
			list.ToArray().Random();
		public static T Random<T>(this T[] array) =>
		array[Main.RNG.Next(0, array?.Length ?? 0)];
		public static bool IsTrue(this XElement xElement, string attribute) =>
			bool.TryParse(xElement?.Attribute(attribute)?.Value, out bool @bool) && @bool;
		public static bool IsFalse(this XElement xElement, string attribute) =>
			bool.TryParse(xElement?.Attribute(attribute)?.Value, out bool @bool) && !@bool;

		/// <summary>
		/// Returns first line in a string or entire string if no linebreaks are included
		/// </summary>
		/// <param name="str">String value</param>
		/// <returns>Returns first line in the string</returns>
		public static string FirstLine(this string @string) =>
			string.IsNullOrWhiteSpace(@string) ? null
			: @string.IndexOf(Environment.NewLine, StringComparison.CurrentCulture) is int index && index >= 0 ?
				@string.Substring(0, index)
				: @string;
	}
}
