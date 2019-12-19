using Godot;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace WOLF3DGame.Model
{
    public class Direction8
    {
        private Direction8() { }

        public static readonly Direction8 SOUTH = new Direction8()
        {
            Value = 0,
            ShortName = "S",
            Name = "South",
            X = 0,
            Z = 1,
            Vector2 = Vector2.Down,
            Vector3 = Vector3.Back,
        };
        public static readonly Direction8 SOUTHWEST = new Direction8()
        {
            Value = 1,
            ShortName = "SW",
            Name = "Southwest",
            X = -1,
            Z = 1,
            Vector2 = new Vector2(-1, 1).Normalized(),
            Vector3 = new Vector3(-1, 0, 1).Normalized(),
        };
        public static readonly Direction8 WEST = new Direction8()
        {
            Value = 2,
            ShortName = "W",
            Name = "West",
            X = -1,
            Z = 0,
            Vector2 = Vector2.Left,
            Vector3 = Vector3.Left,
        };
        public static readonly Direction8 NORTHWEST = new Direction8()
        {
            Value = 3,
            ShortName = "NW",
            Name = "Northwest",
            X = -1,
            Z = -1,
            Vector2 = new Vector2(-1, -1).Normalized(),
            Vector3 = new Vector3(-1, 0, -1).Normalized(),
        };
        public static readonly Direction8 NORTH = new Direction8()
        {
            Value = 4,
            ShortName = "N",
            Name = "North",
            X = 0,
            Z = -1,
            Vector2 = Vector2.Up,
            Vector3 = Vector3.Forward,
        };
        public static readonly Direction8 NORTHEAST = new Direction8()
        {
            Value = 5,
            ShortName = "NE",
            Name = "Northeast",
            X = 1,
            Z = -1,
            Vector2 = new Vector2(1, -1).Normalized(),
            Vector3 = new Vector3(1, 0, -1).Normalized(),
        };
        public static readonly Direction8 EAST = new Direction8()
        {
            Value = 6,
            ShortName = "E",
            Name = "East",
            X = 1,
            Z = 0,
            Vector2 = Vector2.Right,
            Vector3 = Vector3.Right,
        };
        public static readonly Direction8 SOUTHEAST = new Direction8()
        {
            Value = 7,
            ShortName = "SE",
            Name = "Southeast",
            X = 1,
            Z = 1,
            Vector2 = new Vector2(1, 1).Normalized(),
            Vector3 = new Vector3(1, 0, 1).Normalized(),
        };
        public static readonly ReadOnlyCollection<Direction8> Values = Array.AsReadOnly(new Direction8[] { SOUTH, SOUTHWEST, WEST, NORTHWEST, NORTH, NORTHEAST, EAST, SOUTHEAST });

        public uint Value { get; private set; }
        public int X { get; private set; }
        public const int Y = 0;
        public int Z { get; private set; }
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public Vector2 Vector2 { get; private set; }
        public Vector3 Vector3 { get; private set; }

        public static implicit operator ulong(Direction8 d) => d.Value;
        public static implicit operator long(Direction8 d) => d.Value;
        public static implicit operator uint(Direction8 d) => d.Value;
        public static implicit operator int(Direction8 d) => (int)d.Value;
        public static implicit operator ushort(Direction8 d) => (ushort)d.Value;
        public static implicit operator short(Direction8 d) => (short)d.Value;
        public static implicit operator byte(Direction8 d) => (byte)d.Value;
        public static implicit operator string(Direction8 d) => d.Name;
        public static implicit operator Vector2(Direction8 d) => d.Vector2;
        public static implicit operator Vector3(Direction8 d) => d.Vector3;
        public static Direction8 operator +(Direction8 a, Direction8 b) => a + (int)b.Value;
        public static Direction8 operator +(Direction8 direction8, int @int) => From((int)direction8.Value + @int);
        public static Direction8 operator +(Direction8 direction8, uint @uint) => direction8 + (int)@uint;
        public static Direction8 operator +(int @int, Direction8 direction8) => direction8 + @int;
        public static Direction8 operator +(uint @uint, Direction8 direction8) => direction8 + (int)@uint;
        public static Direction8 operator -(Direction8 a, Direction8 b) => a - (int)b.Value;
        public static Direction8 operator -(Direction8 direction8, int @int) => From((int)direction8.Value - @int);
        public static Direction8 operator -(Direction8 direction8, uint @uint) => direction8 - (int)@uint;
        public static Direction8 operator -(int @int, Direction8 direction8) => From(@int - (int)direction8.Value);
        public static Direction8 operator -(uint @uint, Direction8 direction8) => (int)@uint - direction8;
        public static Direction8 operator ++(Direction8 direction8) => direction8 += 1;
        public static Direction8 operator --(Direction8 direction8) => direction8 -= 1;
        public Direction8 Clock => this + 1;
        public Direction8 Counter => this - 1;
        public Direction8 Clock90 => this + 2;
        public Direction8 Counter90 => this - 2;
        public Direction8 Clock135 => this + 3;
        public Direction8 Counter135 => this - 3;
        public Direction8 Opposite => this + 4;
        public Direction8 MirrorX => From(Values.Count - (int)Value);
        public Direction8 MirrorZ => MirrorX.Opposite;

        public static Direction8 From(int @int) => Values[Modulus(@int, Values.Count)];
        public static Direction8 From(uint @uint) => From((int)@uint);
        public static int Modulus(int lhs, int rhs) => (lhs % rhs + rhs) % rhs;

        public static Direction8 From(Vector3 vector3) => Angle(Mathf.Atan2(vector3.x, vector3.z));

        public static Direction8 From(Vector2 vector2) => Angle(vector2.Angle());

        public static Direction8 Angle(float angle) => PositiveAngle(angle + Mathf.Pi);
        public static Direction8 PositiveAngle(float angle) =>
            angle < Mathf.Tau / 16f ? EAST
            : angle < Mathf.Tau * 3f / 16f ? SOUTHEAST
            : angle < Mathf.Tau * 5f / 16f ? SOUTH
            : angle < Mathf.Tau * 7f / 16f ? SOUTHWEST
            : angle < Mathf.Tau * 9f / 16f ? WEST
            : angle < Mathf.Tau * 11f / 16f ? NORTHWEST
            : angle < Mathf.Tau * 13f / 16f ? NORTH
            : angle < Mathf.Tau * 15f / 16f ? NORTHEAST
            : EAST;

        public static Direction8 From(string @string) =>
            uint.TryParse(@string, out uint result) ?
                From(result)
                : (from v in Values
                   where string.Equals(v.ShortName, @string, StringComparison.CurrentCultureIgnoreCase)
                   || string.Equals(v.Name, @string, StringComparison.CurrentCultureIgnoreCase)
                   select v).FirstOrDefault();
    }
}
