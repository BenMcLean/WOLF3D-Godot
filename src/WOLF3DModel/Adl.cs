﻿using System;
using System.Collections.ObjectModel;
using System.IO;

namespace WOLF3DModel
{
	/// <summary>
	/// Parses and stores the Adlib sound effect format. http://www.shikadi.net/moddingwiki/Adlib_sound_effect
	/// This format is extracted from Adam Biser's Wolfenstein Data Compiler (WDC) with the file extension ".ADL"
	/// However, it is not related to the ADL format by Westwood, despite doing the exact same job on the exact same hardware with the exact same file extension.
	/// </summary>
	public class Adl
	{
		/// <summary>
		/// These sound effects play back at 140 Hz.
		/// </summary>
		public const float Hz = 1f / 140f;

		/// <summary>
		/// The OPL register settings for the instrument
		/// </summary>
		public readonly byte[] Instrument = new byte[16];

		/// <summary>
		/// Octave to play notes at
		/// </summary>
		public readonly byte Octave;

		/// <summary>
		/// Pitch data
		/// </summary>
		public readonly byte[] Notes;
		public readonly ushort Priority;
		public Adl(Stream stream) : this(new BinaryReader(stream))
		{ }
		public Adl(BinaryReader binaryReader)
		{
			uint length = binaryReader.ReadUInt32();
			Priority = binaryReader.ReadUInt16();
			binaryReader.Read(Instrument, 0, Instrument.Length);
			Octave = binaryReader.ReadByte();
			Notes = new byte[length];
			binaryReader.Read(Notes, 0, Notes.Length);
		}
		public byte Block => (byte)((Octave & 7) << 2);
		public const byte KeyFlag = 0x20;
		public const byte NotePort = 0xA0;
		public const byte OctavePort = 0xB0;
		public static readonly ReadOnlyCollection<byte> InstrumentPorts = Array.AsReadOnly(new byte[]
		{
			0x20, // mChar 	0x20 	Modulator characteristics
			0x23, // cChar 	0x23 	Carrier characteristics
			0x40, // mScale 	0x40 	Modulator scale
			0x43, // cScale 	0x43 	Carrier scale
			0x60, // mAttack 	0x60 	Modulator attack/decay rate
			0x63, // cAttack 	0x63 	Carrier attack/decay rate
			0x80, // mSust 	0x80 	Modulator sustain
			0x83, // cSust 	0x83 	Carrier sustain
			0xE0, // mWave 	0xE0 	Modulator waveform
			0xE3, // cWave 	0xE3 	Carrier waveform
			//0xC0, // nConn 	0xC0 	Feedback/connection (usually ignored and set to 0)
				  // voice 	- 	unknown (Muse-only)
				  // mode 	- 	unknown (Muse-only)
				  //UINT8[3] 	padding 	- 	Pad instrument definition up to 16 bytes
		});
	}
}
