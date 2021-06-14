﻿using System.Collections.Generic;
using System.IO;

namespace WOLF3DModel
{
	/// <summary>
	/// Parses and stores IMF format music data. http://www.shikadi.net/moddingwiki/IMF_Format
	/// </summary>
	public struct Imf
	{
		/// <summary>
		/// These songs play back at 700 Hz.
		/// </summary>
		public const float Hz = 1f / 700f;

		/// <summary>
		/// Sent to register port.
		/// </summary>
		public readonly byte Register;

		/// <summary>
		/// Sent to data port.
		/// </summary>
		public readonly byte Data;

		/// <summary>
		/// How much to wait.
		/// </summary>
		public readonly ushort Delay;

		public readonly float DelayFloat;

		public Imf(BinaryReader binaryReader)
		{
			Register = binaryReader.ReadByte();
			Data = binaryReader.ReadByte();
			DelayFloat = (Delay = binaryReader.ReadUInt16()) * Hz;
		}

		/// <summary>
		/// Parsing IMF files based on http://www.shikadi.net/moddingwiki/IMF_Format
		/// </summary>
		public static Imf[] ReadImf(Stream stream)
		{
			Imf[] imf;
			using (BinaryReader binaryReader = new BinaryReader(stream))
			{
				ushort length = (ushort)(binaryReader.ReadUInt16() / 4); // Length is provided in number of bytes. Divide by 4 to get the number of 4 byte packets.
				if (length == 0)
				{ // Type-0 format
					stream.Seek(0, 0);
					List<Imf> list = new List<Imf>();
					while (stream.Position < stream.Length)
						list.Add(new Imf(binaryReader));
					imf = list.ToArray();
				}
				else
				{ // Type-1 format
					imf = new Imf[length];
					for (uint i = 0; i < imf.Length; i++)
						imf[i] = new Imf(binaryReader);
				}
			}
			return imf;
		}
	}
}
