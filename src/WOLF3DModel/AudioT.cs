﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3DModel
{
	public struct AudioT
	{
		public static AudioT Load(string folder, XElement xml)
		{
			using (FileStream audioHead = new FileStream(System.IO.Path.Combine(folder, xml.Element("Audio").Attribute("AudioHead").Value), FileMode.Open))
			using (FileStream audioTStream = new FileStream(System.IO.Path.Combine(folder, xml.Element("Audio").Attribute("AudioT").Value), FileMode.Open))
				return new AudioT(audioHead, audioTStream, xml.Element("Audio"));
		}

		public Adl[] Sounds;
		public readonly Dictionary<string, Song> Songs;

		public static uint[] ParseHead(Stream stream)
		{
			List<uint> list = new List<uint>();
			using (BinaryReader binaryReader = new BinaryReader(stream))
				while (stream.Position < stream.Length)
					list.Add(binaryReader.ReadUInt32());
			return list.ToArray();
		}

		public static byte[][] SplitFile(Stream head, Stream file) =>
			SplitFile(ParseHead(head), file);

		public static byte[][] SplitFile(uint[] head, Stream file)
		{
			byte[][] split = new byte[head.Length - 1][];
			for (uint chunk = 0; chunk < split.Length; chunk++)
			{
				uint size = head[chunk + 1] - head[chunk];
				if (size > 0)
				{
					split[chunk] = new byte[size];
					file.Seek(head[chunk], 0);
					file.Read(split[chunk], 0, split[chunk].Length);
				}
			}
			return split;
		}

		public AudioT(Stream audioHedStream, Stream audioTStream, XElement xml) : this(SplitFile(audioHedStream, audioTStream), xml)
		{ }

		public class Song
		{
			public string Name { get; set; }
			public byte[] Bytes { get; set; }
			public Imf[] Imf { get; set; }
			public bool IsImf => Imf != null;

			public override bool Equals(object obj) => obj is Song song && (Name?.Equals(song.Name) ?? false);

			public override int GetHashCode() => base.GetHashCode();

			public override string ToString() => Name;
		}

		public AudioT(byte[][] file, XElement xml)
		{
			uint startAdlibSounds = (uint)xml.Attribute("StartAdlibSounds");
			Sounds = new Adl[(uint)xml.Attribute("NumSounds")];
			for (uint i = 0; i < Sounds.Length; i++)
				if (file[startAdlibSounds + i] != null)
					using (MemoryStream sound = new MemoryStream(file[startAdlibSounds + i]))
						Sounds[i] = new Adl(sound);
			uint startMusic = (uint)xml.Attribute("StartMusic"),
				endMusic = (uint)file.Length - startMusic;
			Songs = new Dictionary<string, Song>();
			bool midi = xml?.Elements("MIDI")?.Any() ?? false;
			for (uint i = 0; i < endMusic; i++)
				if (file[startMusic + i] != null)
					using (MemoryStream song = new MemoryStream(file[startMusic + i]))
					{
						if (midi)
						{
							Song newSong = new Song()
							{
								Name = (xml.Elements("MIDI").Where(
								e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == i
								)?.Select(e => e.Attribute("Name")?.Value)
								?.FirstOrDefault() is string name
								&& !string.IsNullOrWhiteSpace(name)) ?
								name
								: i.ToString(),
								Bytes = new byte[file[startMusic + i].Length - 2],
							};
							// Super 3D Noah's Ark adds two bytes of junk data to the start of all its MIDI songs and I don't know why.
							Array.Copy(
								//sourceArray
								file[startMusic + i],
								//sourceIndex
								2,
								//destinationArray
								newSong.Bytes,
								//destinationIndex
								0,
								//length
								newSong.Bytes.Length
								);
							Songs.Add(newSong.Name, newSong);
						}
						else
						{
							Imf[] imf = Imf.ReadImf(song);
							Song newSong = new Song()
							{
								Name = (xml.Elements("Imf")?.Where(
								e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == i
								)?.Select(e => e.Attribute("Name")?.Value)
								?.FirstOrDefault() is string name
								&& !string.IsNullOrWhiteSpace(name)) ?
								name
								: i.ToString(),
								Bytes = file[startMusic + i],
								Imf = imf,
							};
							Songs.Add(newSong.Name, newSong);
						}
					}
		}
	}
}
