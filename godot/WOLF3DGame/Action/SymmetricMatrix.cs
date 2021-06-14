﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	/// <summary>
	/// A symmetric matrix of shorts whose diagonal elements are all equal to zero.
	/// This is enforced by only storing the lower half of the matrix and ignoring any attempts to set the diagonal entries.
	/// Wolfenstein 3-D uses this data structure to keep track of gun shot noise propagation between the different rooms (floor codes) of a level.
	/// </summary>
	public class SymmetricMatrix
	{
		private short[][] Data;

		public SymmetricMatrix(ushort size) => Size = size;

		public ushort Size
		{
			get => (ushort)(Data?.Length ?? 0);
			set
			{
				Data = new short[value][];
				Clear();
			}
		}

		public SymmetricMatrix Clear()
		{
			for (ushort i = 0; i < Size; i++)
				Data[i] = new short[i + 1];
			return this;
		}

		public short this[ushort x, ushort y]
		{
			get => x < y ?
				Data[y - 1][x]
				: x > y ?
				Data[x - 1][y]
				: (short)0;
			set
			{
				if (x < y)
					Data[y - 1][x] = value;
				else if (x > y)
					Data[x - 1][y] = value;
			}
		}

		public static ushort CalcSize(ushort n) => (ushort)(n * (n + 1) / 2);
		public static ushort CalcSizeReversed(ushort s) => (ushort)Math.Floor(Math.Sqrt(2u * s));

		public override string ToString() => ToString(",");

		public string ToString(string separator, string rowSeparator = null)
		{
			List<string> rows = new List<string>();
			foreach (short[] row in Data)
				rows.Add(string.Join(separator, row));
			return string.Join(rowSeparator ?? separator, rows);
		}

		public string ToXMLTag(string separator = ",") => "<" + GetType().Name + " Data=\"" + ToString(separator) + "\" />";

		public SymmetricMatrix(XElement e, char separator = ',') : this(e.Attribute("Data").Value, separator) { }

		public SymmetricMatrix(string @string, char separator = ',') : this(CalcSizeReversed((ushort)(@string.Count(x => x == separator) + 1)))
		{
			Queue<string> queue = new Queue<string>(@string.Split(separator));
			for (uint row = 0; row < Data.Length; row++)
				for (uint column = 0; column < Data[row].Length; column++)
					Data[row][column] = short.Parse(queue.Dequeue());
		}

		public SymmetricMatrix(SymmetricMatrix other) : this(other.Size)
		{
			for (ushort row = 0; row < Size; row++)
				Array.Copy(other.Data[row], Data[row], Data[row].Length);
		}

		public List<ushort> FloorCodes(params ushort[] floorCodes)
		{
			List<ushort> results = new List<ushort>();
			void DoFloor(ushort floor)
			{
				if (!results.Contains(floor))
				{
					results.Add(floor);
					for (ushort i = 0; i < Size; i++)
						if (this[floor, i] > 0)
							DoFloor(i);
				}
			}
			foreach (ushort floorCode in floorCodes)
				DoFloor(floorCode);
			return results;
		}

		public bool IsConnected(ushort a, ushort b) => this[a, b] > 0 || FloorCodes(a).Contains(b);
	}
}
