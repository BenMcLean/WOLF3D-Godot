using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
		public static readonly Regex NewLineRegex = new Regex(@"\r\n|\n|\r", RegexOptions.Singleline);
		public static string[] Lines(this string @string) => NewLineRegex.Split(@string);
		public static int CountLines(this string @string) => NewLineRegex.Matches(@string).Count + 1;
		public static IEnumerable<Tuple<int, int>> IntPairs(this XAttribute input) => IntPairs(input?.Value);
		public static IEnumerable<Tuple<int, int>> IntPairs(this string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new InvalidDataException("Can't get pairs from \"" + input + "\".");
			string[] inputs = input.Split(',');
			for (int i = 0; i < inputs.Length; i += 2)
				yield return new Tuple<int, int>(int.Parse(inputs[i]), int.Parse(inputs[i + 1]));
		}
		public static IEnumerable<Tuple<float, float>> FloatPairs(this XAttribute input) => FloatPairs(input?.Value);
		public static IEnumerable<Tuple<float, float>> FloatPairs(this string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new InvalidDataException("Can't get pairs from \"" + input + "\".");
			string[] inputs = input.Split(',');
			for (int i = 0; i < inputs.Length; i += 2)
				yield return new Tuple<float, float>(float.Parse(inputs[i]), float.Parse(inputs[i + 1]));
		}
		public static float Width(this Godot.Font font, string @string) => @string?.Lines().Max(line => line.Sum(c => font.GetCharSize(c).x)) ?? 0f;
		public static float Height(this Godot.Font font, string @string) => @string.CountLines() * font.GetHeight();
	}
}
