using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame
{
	/// <summary>
	/// +x is east, +y is up, +z is south
	/// </summary>
	public class Direction8
	{
		#region Instances
		private Direction8() { }
		public static readonly Direction8 NORTH = new Direction8()
		{
			Value = 0,
			ShortName = "N",
			Name = "North",
			X = 0,
			Z = -1,
		};
		public static readonly Direction8 NORTHWEST = new Direction8()
		{
			Value = 1,
			ShortName = "NW",
			Name = "Northwest",
			X = -1,
			Z = -1,
		};
		public static readonly Direction8 WEST = new Direction8()
		{
			Value = 2,
			ShortName = "W",
			Name = "West",
			X = -1,
			Z = 0,
		};
		public static readonly Direction8 SOUTHWEST = new Direction8()
		{
			Value = 3,
			ShortName = "SW",
			Name = "Southwest",
			X = -1,
			Z = 1,
		};
		public static readonly Direction8 SOUTH = new Direction8()
		{
			Value = 4,
			ShortName = "S",
			Name = "South",
			X = 0,
			Z = 1,
		};
		public static readonly Direction8 SOUTHEAST = new Direction8()
		{
			Value = 5,
			ShortName = "SE",
			Name = "Southeast",
			X = 1,
			Z = 1,
		};
		public static readonly Direction8 EAST = new Direction8()
		{
			Value = 6,
			ShortName = "E",
			Name = "East",
			X = 1,
			Z = 0,
		};
		public static readonly Direction8 NORTHEAST = new Direction8()
		{
			Value = 7,
			ShortName = "NE",
			Name = "Northeast",
			X = 1,
			Z = -1,
		};
		public static readonly ReadOnlyCollection<Direction8> Values = Array.AsReadOnly(new Direction8[] { NORTH, NORTHWEST, WEST, SOUTHWEST, SOUTH, SOUTHEAST, EAST, NORTHEAST });
		public static readonly ReadOnlyCollection<Direction8> Cardinals = Array.AsReadOnly(new Direction8[] { NORTH, WEST, SOUTH, EAST });
		public static readonly ReadOnlyCollection<Direction8> Diagonals = Array.AsReadOnly(new Direction8[] { NORTHWEST, SOUTHWEST, SOUTHEAST, NORTHEAST });
		static Direction8()
		{
			foreach (Direction8 direction in Values)
			{
				direction.Vector2 = new Vector2(direction.X, direction.Z).Normalized();
				direction.Vector3 = new Vector3(direction.X, 0f, direction.Z).Normalized();
				direction.Angle = ToPositiveAngle(Mathf.Atan2(-direction.X, -direction.Z));
			}
		}
		#endregion Instances
		#region Data Members
		public uint Value { get; private set; }
		public int X { get; private set; }
		public const int Y = 0;
		public int Z { get; private set; }
		public string Name { get; private set; }
		public string ShortName { get; private set; }
		public Vector2 Vector2 { get; private set; }
		public Vector3 Vector3 { get; private set; }
		public float Angle { get; private set; }
		#endregion Data members
		#region Operators
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
		public static Vector2 operator +(Direction8 direction8, float @float) => new Vector2(direction8.X * @float, direction8.Z * @float);
		public static Vector2 operator +(float @float, Direction8 direction8) => direction8 + @float;
		public static Vector2 operator -(Direction8 direction8, float @float) => new Vector2(direction8.X * -1f * @float, direction8.Z * -1f * @float);
		public static Vector2 operator -(float @float, Direction8 direction8) => direction8 - @float;
		public static Vector2 operator *(Direction8 direction8, float @float) => direction8.Vector2 * @float;
		public static Vector2 operator *(float @float, Direction8 direction8) => direction8 * @float;
		public static Vector2 operator /(Direction8 direction8, float @float) => direction8.Vector2 / @float;
		public static Vector2 operator +(Direction8 direction8, Vector2 vector2) => direction8.Vector2 + vector2;
		public static Vector2 operator +(Vector2 vector2, Direction8 direction8) => vector2 + direction8;
		public static Vector3 operator +(Direction8 direction8, Vector3 vector3) => direction8.Vector3 + vector3;
		public static Vector3 operator +(Vector3 vector3, Direction8 direction8) => direction8 + vector3;
		public static Vector2 operator -(Direction8 direction8, Vector2 vector2) => direction8.Vector2 - vector2;
		public static Vector2 operator -(Vector2 vector2, Direction8 direction8) => vector2 - direction8.Vector2;
		public static Vector3 operator -(Direction8 direction8, Vector3 vector3) => direction8.Vector3 - vector3;
		public static Vector3 operator -(Vector3 vector3, Direction8 direction8) => vector3 - direction8.Vector3;
		public static Vector2 operator *(Direction8 direction8, Vector2 vector2) => direction8.Vector2 * vector2;
		public static Vector2 operator *(Vector2 vector2, Direction8 direction8) => direction8 * vector2;
		public static Vector3 operator *(Direction8 direction8, Vector3 vector3) => direction8.Vector3 * vector3;
		public static Vector3 operator *(Vector3 vector3, Direction8 direction8) => direction8 * vector3;
		public static Vector2 operator /(Direction8 direction8, Vector2 vector2) => direction8.Vector2 / vector2;
		public static Vector2 operator /(Vector2 vector2, Direction8 direction8) => vector2 / direction8.Vector2;
		public static Vector3 operator /(Direction8 direction8, Vector3 vector3) => direction8.Vector3 / vector3;
		public static Vector3 operator /(Vector3 vector3, Direction8 direction8) => vector3 / direction8.Vector3;
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
		#endregion Operators
		public Direction8 Clock => this - 1;
		public Direction8 Counter => this + 1;
		public Direction8 Clock90 => this - 2;
		public Direction8 Counter90 => this + 2;
		public Direction8 Clock135 => this - 3;
		public Direction8 Counter135 => this + 3;
		public Direction8 Opposite => this - 4;
		public Direction8 MirrorX => MirrorZ.Opposite;
		public Direction8 MirrorZ => From(Values.Count - (int)Value);
		public bool IsCardinal => X == 0 || Z == 0; // Value % 2 == 0;
		public bool IsDiagonal => !IsCardinal;
		public static Direction8 From(int @int) => Values[Modulus(@int, Values.Count)];
		public static Direction8 From(uint @uint) => From((int)@uint);
		public static int Modulus(int lhs, int rhs) => (lhs % rhs + rhs) % rhs;
		public static Direction8 From(Vector3 vector3) => From(Assets.Vector2(vector3));
		public static Direction8 From(Vector2 vector2) => FromAngle(vector2.Angle());
		public static Direction8 CardinalFrom(Vector3 vector3) => CardinalFrom(Assets.Vector2(vector3));
		public static Direction8 CardinalFrom(Vector2 vector2) => CardinalFromAngle(Mathf.Atan2(-vector2.x, -vector2.y));
		public static Direction8 AngleToPoint(Vector3 vector3) => AngleToPoint(Vector3.Zero, vector3);
		public static Direction8 CardinalToPoint(Vector3 vector3) => CardinalToPoint(Vector3.Zero, vector3);
		public static Direction8 AngleToPoint(Vector3 a, Vector3 b) => AngleToPoint(a.x, a.z, b.x, b.z);
		public static Direction8 AngleToPoint(Vector2 a, Vector2 b) => AngleToPoint(a.x, a.y, b.x, b.y);
		public static Direction8 CardinalToPoint(Vector3 a, Vector3 b) => CardinalToPoint(a.x, a.z, b.x, b.z);
		public static Direction8 CardinalToPoint(Vector2 a, Vector2 b) => CardinalToPoint(a.x, a.y, b.x, b.y);
		public static Direction8 AngleToPoint(float x, float y) => AngleToPoint(0f, 0f, x, y);
		public static Direction8 CardinalToPoint(float x, float y) => CardinalToPoint(0f, 0f, x, y);
		public static Direction8 AngleToPoint(float x1, float y1, float x2, float y2) => FromAngle(Mathf.Atan2(x1 - x2, y1 - y2));
		public static Direction8 CardinalToPoint(float x1, float y1, float x2, float y2) => CardinalFromAngle(Mathf.Atan2(x1 - x2, y1 - y2));
		public Basis Basis => new Basis(Vector3.Up, Angle).Orthonormalized();
		public bool InSight(Vector3 a, Vector3 b, float halfFOV = Assets.QuarterPi) => InSight(a.x, a.z, b.x, b.z, halfFOV);
		public bool InSight(Vector2 a, Vector2 b, float halfFOV = Assets.QuarterPi) => InSight(a.x, a.y, b.x, b.y, halfFOV);
		public bool InSight(float x1, float y1, float x2, float y2, float halfFOV = Assets.QuarterPi) => InSight(Mathf.Atan2(x1 - x2, y1 - y2), halfFOV);
		public bool InSight(float angle, float halfFOV = Assets.QuarterPi) => ToPositiveAngle(angle - Angle) <= halfFOV || ToPositiveAngle(Angle - angle) <= halfFOV;
		public static Direction8 FromAngle(Transform transform) => FromAngle(transform.basis);
		public static Direction8 FromAngle(Basis basis) => FromAngle(basis.GetEuler().y);
		public static Direction8 FromAngle(float angle) => FromPositiveAngle(ToPositiveAngle(angle));
		public static float ToPositiveAngle(float angle) => Mathf.PosMod(angle, Mathf.Tau);
		private static readonly ReadOnlyCollection<float> positiveAngles = Array.AsReadOnly(new float[8] { Mathf.Tau / 16f, Mathf.Tau * 3f / 16f, Mathf.Tau * 5f / 16f, Mathf.Tau * 7f / 16f, Mathf.Tau * 9f / 16f, Mathf.Tau * 11f / 16f, Mathf.Tau * 13f / 16f, Mathf.Tau * 15f / 16f });
		public static Direction8 FromPositiveAngle(float angle) =>
			angle < positiveAngles[0] ? NORTH
			: angle < positiveAngles[1] ? NORTHWEST
			: angle < positiveAngles[2] ? WEST
			: angle < positiveAngles[3] ? SOUTHWEST
			: angle < positiveAngles[4] ? SOUTH
			: angle < positiveAngles[5] ? SOUTHEAST
			: angle < positiveAngles[6] ? EAST
			: angle < positiveAngles[7] ? NORTHEAST
			: NORTH;
		public static Direction8 CardinalFromAngle(Transform transform) => CardinalFromAngle(transform.basis);
		public static Direction8 CardinalFromAngle(Basis basis) => CardinalFromAngle(basis.GetEuler().y);
		public static Direction8 CardinalFromAngle(float angle) => CardinalFromPositiveAngle(ToPositiveAngle(angle));
		private static readonly ReadOnlyCollection<float> cardinalPositiveAngles = Array.AsReadOnly(new float[4] { Mathf.Tau / 8f, Mathf.Tau * 3f / 8f, Mathf.Tau * 5f / 8f, Mathf.Tau * 7f / 8f });
		public static Direction8 CardinalFromPositiveAngle(float angle) =>
			angle < cardinalPositiveAngles[0] ? NORTH
			: angle < cardinalPositiveAngles[1] ? WEST
			: angle < cardinalPositiveAngles[2] ? SOUTH
			: angle < cardinalPositiveAngles[3] ? EAST
			: NORTH;
		public static Direction8 From(XAttribute xAttribute) => From(xAttribute?.Value);
		public static Direction8 From(string @string) =>
			int.TryParse(@string, out int result) ?
				From(result)
				: Values.Where(v =>
					string.Equals(v.ShortName, @string, StringComparison.InvariantCultureIgnoreCase)
					|| string.Equals(v.Name, @string, StringComparison.InvariantCultureIgnoreCase)
				).FirstOrDefault();
		public static Direction8 Combine(params Direction8[] directions)
		{
			if (directions == null || directions.Length < 1)
				return null;
			int x = 0, z = 0;
			foreach (Direction8 direction in directions)
			{
				x += direction.X;
				z += direction.Z;
			}
			x = Math.Sign(x);
			z = Math.Sign(z);
			return Values.Where(direction => direction.X == x && direction.Z == z).FirstOrDefault();
		}
		public static IEnumerable<Direction8> RandomOrder(params Direction8[] excluded) => RandomOrder(Main.RNG, excluded);
		public static IEnumerable<Direction8> RandomOrder(RNG rng, params Direction8[] excluded)
		{
			List<Direction8> directions = new List<Direction8>(Values);
			foreach (Direction8 exclude in excluded)
				directions.Remove(exclude);
			while (directions.Count > 0)
			{
				Direction8 direction = directions[rng.Next(0, directions.Count)];
				directions.Remove(direction);
				yield return direction;
			}
		}
		public int SpriteNumber(Vector3 from, Vector3 to) => SpriteNumber(Direction8.AngleToPoint(to, from));
		public int SpriteNumber(Direction8 angle) => Modulus((int)Value + (int)angle.Value, 8); // TODO: Fix this
		public override string ToString() => ShortName;
	}
}
