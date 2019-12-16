using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3DGame
{
    public class Direction8
    {
        private Direction8() { }

        public static readonly Direction8 SOUTH = new Direction8()
        {
            Uint = 0,
            ShortName = "S",
            Name = "South",
            Vector2 = Vector2.Down,
            Vector3 = Vector3.Back,
        };
        public static readonly Direction8 SOUTHWEST = new Direction8()
        {
            Uint = 1,
            ShortName = "SW",
            Name = "Southwest",
            Vector2 = new Vector2(-1, 1).Normalized(),
            Vector3 = new Vector3(-1, 0, 1).Normalized(),
        };
        public static readonly Direction8 WEST = new Direction8()
        {
            Uint = 2,
            ShortName = "W",
            Name = "West",
            Vector2 = Vector2.Left,
            Vector3 = Vector3.Left,
        };
        public static readonly Direction8 NORTHWEST = new Direction8()
        {
            Uint = 3,
            ShortName = "NW",
            Name = "Northwest",
            Vector2 = new Vector2(1, 1).Normalized(),
            Vector3 = new Vector3(1, 0, 1).Normalized(),
        };
        public static readonly Direction8 NORTH = new Direction8()
        {
            Uint = 4,
            ShortName = "N",
            Name = "North",
            Vector2 = Vector2.Up,
            Vector3 = Vector3.Forward,
        };
        public static readonly Direction8 NORTHEAST = new Direction8()
        {
            Uint = 5,
            ShortName = "NE",
            Name = "Northeast",
            Vector2 = new Vector2(1, -1).Normalized(),
            Vector3 = new Vector3(1, 0, -1).Normalized(),
        };
        public static readonly Direction8 EAST = new Direction8()
        {
            Uint = 6,
            ShortName = "E",
            Name = "East",
            Vector2 = Vector2.Right,
            Vector3 = Vector3.Right,
        };
        public static readonly Direction8 SOUTHEAST = new Direction8()
        {
            Uint = 7,
            ShortName = "SE",
            Name = "Southeast",
            Vector2 = new Vector2(-1, -1).Normalized(),
            Vector3 = new Vector3(-1, 0, -1).Normalized(),
        };
        public static readonly Direction8[] Values = new Direction8[] { SOUTH, SOUTHWEST, WEST, NORTHWEST, NORTH, NORTHEAST, EAST, SOUTHEAST };

        public uint Uint { get; private set; }
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public Vector2 Vector2 { get; private set; }
        public Vector3 Vector3 { get; private set; }

        public Direction8 Clock()
        {
            return add(1);
        }

        public Direction8 Counter()
        {
            return add(-1);
        }

        public Direction8 Clock90()
        {
            return add(2);
        }

        public Direction8 Counter90()
        {
            return add(-2);
        }

        private Direction8 add(int @int) => Values[Modulus((((int)Uint) + @int), Values.Length)];

        public static int Modulus(int lhs, int rhs) => (lhs % rhs + rhs) % rhs;
    }
}
