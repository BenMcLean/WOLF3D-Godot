Everything in this repo is still a work in progress: **the game is not playable yet.**

# Goals

This project aims to re-create the experience of the classic 1992 first person shooter game Wolfenstein 3-D and other games running on the same engine (Spear of Destiny and [Super 3-D Noah's Ark](https://wisdomtree.itch.io/s3dna)) for virtual reality, ultimately to make these old games playable on the Oculus Quest in true stereoscopic 3D. Also supporting PC VR.

There has already been [an admirable effort at recreating Wolfenstein 3-D in VR](https://further-beyond.itch.io/wolf3dvr) so anyone who just wants to play the game in VR right now on their PC should check that version out. However, in my view, that version leaves some gaps which this project aims to fill in:

1. This is on the Godot-Mono engine, so the entire technology stack is as open source as possible. This remains true despite the fact that almost none of the original Wolfenstein 3-D code is directly involved in this project. It's a port, but it's not a source port: it is a brand new high-level emulator of the game's rules re-interpreted for VR.
2. This version is going to load in all the assets from the original 1992 game files at runtime with no intermediary formats. (beyond adding some XML for things normally compiled into the EXE) The shareware is included but unless you obtain the original MS-DOS game files for the other episodes and games then you can't play them. Files from other platforms are unsupported.
3. Apart from rendering in HD, this version is going to keep the aesthetics strictly matching the original 1992 MS-DOS PC version to a ridiculously autistic and even slightly creepy degree. This means emulated Adlib / Sound Blaster sound, no dynamic lighting and no high resolution textures. If you don't like the original pixel art by Adrian Carmack and the original Adlib soundtrack by Robert Prince then this is not the version of Wolfenstein 3-D for you.
4. This version is going to have extensive mod support, including directly supporting classic mods and user-made map packs from the original game and/or made with existing modding tools from the community. In fact, the whole thing is being constructed from the beginning such that everything (even the full registered WOLF3D) is treated during development as a mod of the shareware version. Mods that require patching actual new code features into WOLF3D.EXE will probably not run, but most mods will probably run.
5. The goal is to make this program work on the Oculus Quest. Other platforms, like PC VR, may also be supported, but the Oculus Quest is the main goal.

At this time, I do NOT plan to work on support for Blake Stone, Corridor 7 or Rise of the Triad.

Oh, and also, my project is going to go out of its way to intentionally not change the name of its master branch from continuing to be called "master" because changing things that we all know are already not racist in the name of "stopping racism" is fundamentally dishonest and contemptible.

Not to mention that I, of course, will not be censoring any of the swatztikas in the game, nor the pictures of Hitler, nor the German eagles, nor the SS uniforms, nor the Nazi music, nor any other World War II related content from the original game. Nazis are bad and we should have genuinely bad Nazis in our World War II games, not fake ones.

# Building

Currently works with Godot Engine v3.3.stable.mono.official.

In your Android export preset for Oculus Quest, you need these settings:
* `Options > Xr Features > XR Mode`: `Oculus Mobile VR`
* `Options > Xr Features > Degrees of Freedom`: `3DOF and 6DOF` (3DOF mode might not actually work but allowing it doesn't hurt anything)
* `Options > Xr Features > Hand tracking`: `None` (I have no plans to support hand tracking)
* `Options > Permissions`: select both `Read External Storage` and `Write External Storage`. (required to read the game files and write the shareware files)
* `Resources > Filters to export non-resource files/folders`: add `*.zip, *.xml, *.wlf, *.sf2` to include the Wolfenstein 3-D shareware, the settings XML, custom music and a default soundfont. (game might crash without this!)

# Useless Legal Crap

Only small exerpts from the Wolfenstein 3-D source code were used in the creation of this program. The bulk of the code is original.

[The Wolfenstein 3-D source code](https://github.com/id-Software/wolf3d) is dual-licensed as the ["Limited Use Software License Agreement"](https://github.com/id-Software/wolf3d) as well as [GNU GPL](https://github.com/id-Software/Wolf3D-iOS/blob/master/wolf3d/COPYING.txt). We know that not only the iOS source code but also the original 16-bit C source code is available under the GNU GPL because John Carmack's notes in id Software's official Wolfenstein 3-D iOS source code release from March 20th, 2009 stated, ["I released the original source for Wolfenstein 3D many years ago, originally under a not-for-commercial purposes license, then later under the GPL."](https://github.com/id-Software/Wolf3D-iOS/blob/master/wolf3d/readme_iWolf.txt) I would be able to omit this complicated explanation if only someone at Zenimax (or Microsoft?) would take five minutes to actually add a GNU GPL license file to the official Wolfenstein 3-D source code release.

The [Wolfenstein 3-D Shareware v1.4](https://archive.org/download/Wolfenstein3d/Wolfenstein3dV14sw.ZIP) game data created by id Software and published by Apogee is included under its original shareware redistribution permission from 1992. No other game data is included in official builds. Users supply their own game data to play anything else.

[Godot](http://godotengine.org/) is under the [MIT licsense](https://github.com/godotengine/godot/blob/master/LICENSE.txt).

[godot_openvr](https://github.com/GodotVR/godot_openvr) by Bastiaan Olij a.k.a. Mux213 is [MIT licensed](https://github.com/GodotVR/godot_openvr/blob/master/LICENSE). It uses the [OpenVR SDK](https://github.com/ValveSoftware/openvr) by Valve Software under the [BSD 3-Clause "New" or "Revised" License](https://github.com/ValveSoftware/openvr/blob/master/LICENSE).

[godot_oculus_mobile](https://github.com/GodotVR/godot_oculus_mobile) is [MIT licensed](https://github.com/GodotVR/godot_oculus_mobile/blob/master/LICENSE). It uses the [Oculus Mobile SDK](https://developer.oculus.com/downloads/package/oculus-mobile-sdk/) which is available under the [Oculus SDK License Agreement](https://developer.oculus.com/licenses/oculussdk/) which no human has ever actually read.

[NScumm.Audio](https://github.com/scemino/NScumm.Audio) by scemino is a C# port of [AdPlug](http://adplug.github.io/) by Simon Peter which is licensed under [LGPL v2.1](https://github.com/adplug/adplug/blob/master/COPYING). Its [DosBox OPL3 emulator is licensed under GPL v2+ and its WoodyOPL emulator from the DOSBox team is licensed under LGPL v2.1.](https://www.dosbox.com/). Its [Mono.Options](https://github.com/xamarin/XamarinComponents/tree/master/XPlat/Mono.Options) is under the [MIT license](https://github.com/xamarin/XamarinComponents/blob/master/XPlat/Mono.Options/License.md).

[Godot MIDI Player](https://bitbucket.org/arlez80/godot-midi-player/src/master/) by Yui Kinomoto is [MIT licensed](https://bitbucket.org/arlez80/godot-midi-player/src/master/LICENSE.txt).

"RNG.cs" contributed by [Tommy Ettinger](https://github.com/tommyettinger) was explicitly dedicated to the public domain.

The "Bm437_IBM_VGA9" bitmap font is converted from [The Ultimate Oldschool PC Font Pack by VileR](https://int10h.org/oldschool-pc-fonts) under the [CC BY-SA 4.0 license](https://int10h.org/oldschool-pc-fonts/readme/#legal_stuff).

"1mgm.sf2" (1.03 MB) is a soundfont from the Sound Blaster AWE32. It is "Copyright (c) E-mu Systems, Inc., 1993." E-mu Systems, Inc. was acquired by Creative Technology Ltd. in 1993. This file is not used by this program to play any music from Wolfenstein 3-D. Instead, it is only used as the built-in default soundfont for user-supplied game data that needs MIDI playback such as Super 3D Noah's Ark. I will replace this file with a different, non-commercial soundfont if anyone suggests a more appropriate one or if Creative ever complains, but I doubt they have any further interest in prohibiting non-commercial redistribution of such an old, tiny and obscure computer file.

# Thanks

This is my (Ben McLean's) pet project, but I have received much help that I need to thank people for.

First of all, [this is mind-blowing](https://bitbucket.org/NY00123/gamesrc-ver-recreation/). I still have a hard time believing tbat anything so awesome could ever exist.

Second, Valéry Sablonnière has been a huge help with his [C# port of the DOSBOX OPL (Adlib / Sound Blaster) emulator](https://github.com/scemino/NScumm.Audio). I might not have been able to get the sound working in Godot without his code, which is GPL by the way.

Third, [Adam Biser](https://adambiser.itch.io/wdc), [Fabien Sanglard](http://fabiensanglard.net/gebbwolf3d/), [Blzut3](http://maniacsvault.net/ecwolf/) and even John Carmack himsself have been quite helpful, not only by having made significant contributions to the Wolfenstein 3-D fan community but also by directly answering some of my emailed technical questions. Also, the [Game Modding wiki](http://www.shikadi.net/moddingwiki/Wolfenstein_3-D) has been quite helpful as a resource.

[kalbert312](https://github.com/kalbert312) helped me figure the sprite graphics format out by sending me some of his Java and TypeScript code to translate into C#.

[Tommy Ettinger](https://github.com/tommyettinger) contributed the random number generator.

Finally, gotta thank the original id Software team from back in the day for making such awesome games!
