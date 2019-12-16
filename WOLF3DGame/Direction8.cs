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
        public static readonly Direction8 SOUTH = new Direction8(0, "S", "South", Vector2.Down, Vector3.Back);
        public static readonly Direction8 SOUTHWEST = new Direction8(1, "SW", "Southwest", new Vector2(-1, 1).Normalized(), new Vector3(-1, 0, 1).Normalized());
        public static readonly Direction8 WEST = new Direction8(2, "W", "West", Vector2.Left, Vector3.Left);
        public static readonly Direction8 NORTHWEST = new Direction8(3, "NW", "Northwest", new Vector2(1, 1).Normalized(), new Vector3(1, 0, 1).Normalized());
        public static readonly Direction8 NORTH = new Direction8(4, "N", "North", Vector2.Up, Vector3.Forward);
        public static readonly Direction8 NORTHEAST = new Direction8(5, "NE", "Northeast", new Vector2(1, -1).Normalized(), new Vector3(1, 0, -1).Normalized());
        public static readonly Direction8 EAST = new Direction8(6, "E", "East", Vector2.Right, Vector3.Right);
        public static readonly Direction8 SOUTHEAST = new Direction8(7, "SE", "Southeast", new Vector2(-1, -1).Normalized(), new Vector3(-1, 0, -1).Normalized());
        public static readonly Direction8[] Values = new Direction8[] { SOUTH, SOUTHWEST, WEST, NORTHWEST, NORTH, NORTHEAST, EAST, SOUTHEAST };

        private Direction8(uint @uint, string shortName, string name, Vector2 vector2, Vector3 vector3)
        {
            Uint = @uint;
            ShortName = shortName;
            Name = name;
            Vector2 = vector2;
            Vector3 = vector3;
        }

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
