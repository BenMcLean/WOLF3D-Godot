using Godot;
using System;
using System.Linq;

namespace WOLF3DGame
{
    public class Direction8
    {
        private Direction8() { }

        public static readonly Direction8 SOUTH = new Direction8()
        {
            Value = 0,
            ShortName = "S",
            Name = "South",
            Vector2 = Vector2.Down,
            Vector3 = Vector3.Back,
        };
        public static readonly Direction8 SOUTHWEST = new Direction8()
        {
            Value = 1,
            ShortName = "SW",
            Name = "Southwest",
            Vector2 = new Vector2(-1, 1).Normalized(),
            Vector3 = new Vector3(-1, 0, 1).Normalized(),
        };
        public static readonly Direction8 WEST = new Direction8()
        {
            Value = 2,
            ShortName = "W",
            Name = "West",
            Vector2 = Vector2.Left,
            Vector3 = Vector3.Left,
        };
        public static readonly Direction8 NORTHWEST = new Direction8()
        {
            Value = 3,
            ShortName = "NW",
            Name = "Northwest",
            Vector2 = new Vector2(1, 1).Normalized(),
            Vector3 = new Vector3(1, 0, 1).Normalized(),
        };
        public static readonly Direction8 NORTH = new Direction8()
        {
            Value = 4,
            ShortName = "N",
            Name = "North",
            Vector2 = Vector2.Up,
            Vector3 = Vector3.Forward,
        };
        public static readonly Direction8 NORTHEAST = new Direction8()
        {
            Value = 5,
            ShortName = "NE",
            Name = "Northeast",
            Vector2 = new Vector2(1, -1).Normalized(),
            Vector3 = new Vector3(1, 0, -1).Normalized(),
        };
        public static readonly Direction8 EAST = new Direction8()
        {
            Value = 6,
            ShortName = "E",
            Name = "East",
            Vector2 = Vector2.Right,
            Vector3 = Vector3.Right,
        };
        public static readonly Direction8 SOUTHEAST = new Direction8()
        {
            Value = 7,
            ShortName = "SE",
            Name = "Southeast",
            Vector2 = new Vector2(-1, -1).Normalized(),
            Vector3 = new Vector3(-1, 0, -1).Normalized(),
        };
        public static readonly Direction8[] Values = new Direction8[] { SOUTH, SOUTHWEST, WEST, NORTHWEST, NORTH, NORTHEAST, EAST, SOUTHEAST };

        public uint Value { get; private set; }
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

        public Direction8 Clock => Add(1);
        public Direction8 Counter => Add(-1);
        public Direction8 Clock90 => Add(2);
        public Direction8 Counter90 => Add(-2);
        public Direction8 Clock135 => Add(3);
        public Direction8 Counter135 => Add(-3);
        public Direction8 Opposite => Add(4);
        private Direction8 Add(int @int) => Values[Modulus((((int)Value) + @int), Values.Length)];
        public static int Modulus(int lhs, int rhs) => (lhs % rhs + rhs) % rhs;

        public static Direction8 From(string @string)
            => uint.TryParse(@string, out uint result) && result < Values.Length ?
                Values[result]
                : (from v in Values
                   where string.Equals(v.ShortName, @string, StringComparison.CurrentCultureIgnoreCase)
                   || string.Equals(v.Name, @string, StringComparison.CurrentCultureIgnoreCase)
                   select v).FirstOrDefault();

        public static Direction8 From(Vector3 vector3) => From(new Vector2(vector3.x, vector3.z));

        public static Direction8 From(Vector2 vector2)
        {
            return null; // TODO: FIX THIS!
        }
    }
}
