using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WOLF3DModel
{
	/// <summary>
	/// Methods that start with "Draw" modify the original array. Other methods return a copy.
	/// x is width, y is height
	/// x+ is right, y+ is down
	/// (i << 2 == i * 4)
	/// (i >> 2 == i / 4) when i is a positive integer
	/// </summary>
	public static class TextureMethods
	{
		//TODO: DrawTriangle
		//TODO: DrawLine
		//TODO: DrawCircle
		//TODO: DrawEllipse
		#region Drawing
		/// <summary>
		/// Draws one pixel of the specified color
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data</param>
		/// <param name="color">rgba color to draw</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with pixel drawn</returns>
		public static byte[] DrawPixel(this byte[] texture, int color, int x, int y, int width = 0) => DrawPixel(texture, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, width);
		/// <summary>
		/// Draws one pixel of the specified color
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data to be modified</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with pixel drawn</returns>
		public static byte[] DrawPixel(this byte[] texture, byte red, byte green, byte blue, byte alpha, int x, int y, int width = 0)
		{
			if (x < 0 || y < 0) return texture;
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			x <<= 2; //x *= 4;
			if (x >= xSide || y >= ySide) return texture;
			int offset = y * xSide + x;
			texture[offset] = red;
			texture[offset + 1] = green;
			texture[offset + 2] = blue;
			texture[offset + 3] = alpha;
			return texture;
		}
		/// <summary>
		/// Draws a rectangle of the specified color
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data to be modified</param>
		/// <param name="color">rgba color to draw</param>
		/// <param name="x">upper left corner of rectangle</param>
		/// <param name="y">upper left corner of rectangle</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with rectangle drawn</returns>
		public static byte[] DrawRectangle(this byte[] texture, int color, int x, int y, int rectWidth, int rectHeight, int width = 0) => DrawRectangle(texture, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, rectWidth, rectHeight, width);
		/// <summary>
		/// Draws a rectangle of the specified color
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data to be modified</param>
		/// <param name="x">upper left corner of rectangle</param>
		/// <param name="y">upper left corner of rectangle</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with rectangle drawn</returns>
		public static byte[] DrawRectangle(this byte[] texture, byte red, byte green, byte blue, byte alpha, int x, int y, int rectWidth, int rectHeight, int width = 0)
		{
			if (rectHeight < 1) rectHeight = rectWidth;
			if (x < 0)
			{
				rectWidth += x;
				x = 0;
			}
			if (y < 0)
			{
				rectHeight += y;
				y = 0;
			}
			width = width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width;
			int height = (texture.Length / width) >> 2;
			if (rectWidth < 1 || rectHeight < 1 || x >= width || y >= height) return texture;
			rectWidth = Math.Min(rectWidth, width - x);
			rectHeight = Math.Min(rectHeight, height - y);
			int xSide = width << 2,
				x4 = x << 2,
				offset = y * xSide + x4,
				rectWidth4 = rectWidth << 2,
				yStop = offset + xSide * rectHeight;
			texture[offset] = red;
			texture[offset + 1] = green;
			texture[offset + 2] = blue;
			texture[offset + 3] = alpha;
			for (int x2 = offset + 4; x2 < offset + rectWidth4; x2 += 4)
				Array.Copy(texture, offset, texture, x2, 4);
			for (int y2 = offset + xSide; y2 < yStop; y2 += xSide)
				Array.Copy(texture, offset, texture, y2, rectWidth4);
			return texture;
		}
		/// <summary>
		/// Draws a texture onto a different texture
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data to be modified</param>
		/// <param name="x">upper left corner of where to insert</param>
		/// <param name="y">upper left corner of where to insert</param>
		/// <param name="insert">raw rgba888 pixel data to insert</param>
		/// <param name="insertWidth">width of insert or 0 to assume square texture</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with insert drawn</returns>
		public static byte[] DrawInsert(this byte[] texture, int x, int y, byte[] insert, int insertWidth = 0, int width = 0)
		{
			int insertX = 0, insertY = 0;
			if (x < 0)
			{
				insertX = -x;
				insertX <<= 2;
				x = 0;
			}
			if (y < 0)
			{
				insertY = -y;
				y = 0;
			}
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			x <<= 2; // x *= 4;
			if (x > xSide) return texture;
			int insertXside = (insertWidth < 1 ? (int)Math.Sqrt(insert.Length >> 2) : insertWidth) << 2,
				actualInsertXside = (x + insertXside > xSide ? xSide - x : insertXside) - insertX,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			if (y > ySide) return texture;
			if (xSide == insertXside && x == 0 && insertX == 0)
				Array.Copy(insert, insertY * insertXside, texture, y * xSide, Math.Min(insert.Length - insertY * insertXside + insertX, texture.Length - y * xSide));
			else
				for (int y1 = y * xSide + x, y2 = insertY * insertXside + insertX; y1 < texture.Length && y2 < insert.Length; y1 += xSide, y2 += insertXside)
					Array.Copy(insert, y2, texture, y1, actualInsertXside);
			return texture;
		}
		/// <summary>
		/// Draws a texture onto a different texture with simple transparency
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data to be modified</param>
		/// <param name="x">upper left corner of where to insert</param>
		/// <param name="y">upper left corner of where to insert</param>
		/// <param name="insert">raw rgba888 pixel data to insert</param>
		/// <param name="insertWidth">width of insert or 0 to assume square texture</param>
		/// <param name="threshold">only draws pixel if alpha is higher than or equal to threshold</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with insert drawn</returns>
		public static byte[] DrawTransparentInsert(this byte[] texture, int x, int y, byte[] insert, int insertWidth = 0, byte threshold = 128, int width = 0)
		{
			int insertX = 0, insertY = 0;
			if (x < 0)
			{
				insertX = -x;
				insertX <<= 2;
				x = 0;
			}
			if (y < 0)
			{
				insertY = -y;
				y = 0;
			}
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			x <<= 2; // x *= 4;
			if (x > xSide) return texture;
			int insertXside = (insertWidth < 1 ? (int)Math.Sqrt(insert.Length >> 2) : insertWidth) << 2,
				actualInsertXside = (x + insertXside > xSide ? xSide - x : insertXside) - insertX,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			if (y > ySide) return texture;
			for (int y1 = y * xSide + x, y2 = insertY * insertXside + insertX; y1 < texture.Length && y2 < insert.Length; y1 += xSide, y2 += insertXside)
				for (int x1 = 0; x1 < actualInsertXside; x1 += 4)
					if (insert[y2 + x1 + 3] >= threshold)
						Array.Copy(insert, y2 + x1, texture, y1 + x1, 4);
			return texture;
		}
		/// <summary>
		/// Draws 1 pixel wide padding around the outside of a rectangular area by copying pixels from the edges of the area
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data to be modified</param>
		/// <param name="x">upper left corner of area</param>
		/// <param name="y">upper left corner of area</param>
		/// <param name="areaWidth">width of area</param>
		/// <param name="areaHeight">height of area or 0 to assume square area</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>same texture with padding drawn</returns>
		public static byte[] DrawPadding(this byte[] texture, int x, int y, int areaWidth, int areaHeight, int width = 0)
		{
			if (areaHeight < 1) areaHeight = areaWidth;
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				x4 = x * 4,
				offset = y > 0 ? (y - 1) * xSide + x4 : x4,
				stop = Math.Min((y + areaHeight) * xSide + x4, texture.Length),
				areaWidth4 = areaWidth << 2;
			if (x4 > xSide || offset > texture.Length)
				return texture;
			if (y > 0)
				Array.Copy(texture, offset + xSide, texture, offset, Math.Min(areaWidth4, xSide - x4));
			if (y + areaHeight < ySide)
				Array.Copy(texture, stop - xSide, texture, stop, Math.Min(areaWidth4, xSide - x4));
			for (int y1 = Math.Max(x4, offset); y1 <= stop; y1 += xSide)
			{
				if (x > 0)
					Array.Copy(texture, y1, texture, y1 - 4, 4);
				if (x4 + areaWidth4 < xSide)
					Array.Copy(texture, y1 + areaWidth4 - 4, texture, y1 + areaWidth4, 4);
			}
			return texture;
		}
		public static byte[] DrawTriangle(this byte[] texture, int color, int x, int y, int triangleWidth, int triangleHeight, int width = 0) => DrawTriangle(texture, (byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color, x, y, triangleWidth, triangleHeight, width);
		public static byte[] DrawTriangle(this byte[] texture, byte red, byte green, byte blue, byte alpha, int x, int y, int triangleWidth, int triangleHeight, int width = 0)
		{
			int textureWidth = width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width,
				xSide = textureWidth << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			if ((x < 0 && x + triangleWidth < 0)
				|| (x > textureWidth && x + triangleWidth > textureWidth)
				|| (y < 0 && y + triangleHeight < 0)
				|| (y > ySide && y + triangleHeight > ySide))
				return texture; // Triangle is completely outside the texture bounds.
			int realX = x < 1 ? 0 : Math.Min(xSide, x << 2),
				realY = y < 1 ? 0 : (Math.Min(y, ySide) * xSide);
			bool isWide = triangleWidth > 0,
				isTall = triangleHeight > 0;
			triangleWidth = Math.Abs(triangleWidth);
			triangleHeight = Math.Abs(triangleHeight);
			int triangleWidth4 = triangleWidth << 2;
			//if (/*(x + triangleWidth) >> 2 > xSide ||*/ y > ySide) throw new NotImplementedException();
			int offset = realY * xSide + (realX << 2);
			texture[offset] = red;
			texture[offset + 1] = green;
			texture[offset + 2] = blue;
			texture[offset + 3] = alpha;
			int xStop, yStop, longest;
			if (isWide)
			{
				xStop = Math.Min(Math.Min((realY + 1) * xSide, offset + triangleWidth4), texture.Length - 4);
				longest = xStop - offset;
				for (int x1 = offset + 4; x1 < xStop; x1 += 4)
					Array.Copy(texture, offset, texture, x1, 4);
			}
			else
			{
				xStop = Math.Max(Math.Max(realY * xSide, offset - triangleWidth4), 0);
				longest = offset - xStop;
				for (int x1 = offset - 4; x1 > xStop; x1 -= 4)
					Array.Copy(texture, offset, texture, x1, 4);
			}
			//int yStop = offset - triangleHeight * xSide;
			//float @float = (float)(triangleWidth - 1) / triangleHeight;
			//for (int y1 = offset - xSide, y2 = triangleHeight - 1; y1 > 0 && y1 > yStop; y1 -= xSide, y2--)
			//	Array.Copy(texture, offset, texture, y1, Math.Min(longest, ((int)(@float * y2) + 1) << 2));
			return texture;
		}
		#endregion Drawing
		#region Rotation
		/// <summary>
		/// Flips an image on the X axis
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
		public static byte[] FlipX(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			byte[] flipped = new byte[texture.Length];
			for (int y = 0; y < flipped.Length; y += xSide)
				Array.Copy(texture, y, flipped, flipped.Length - xSide - y, xSide);
			return flipped;
		}
		/// <summary>
		/// Flips an image on the Y axis
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
		public static byte[] FlipY(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			byte[] flipped = new byte[texture.Length];
			for (int y = 0; y < flipped.Length; y += xSide)
				for (int x = 0; x < xSide; x += 4)
					Array.Copy(texture, y + x, flipped, y + xSide - 4 - x, 4);
			return flipped;
		}
		/// <summary>
		/// Rotates image clockwise by 45 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
		public static byte[] RotateClockwise45Thin(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = (xSide + (ySide << 2)) - 4;
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
			for (int y1 = texture.Length - xSide, y2 = newXside * ySide - newXside; y1 < texture.Length; y1 += 4, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 >= 0; x1 -= xSide, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
				}
			return rotated;
		}
		/// <summary>
		/// Rotates image clockwise by 45 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
		public static byte[] RotateClockwise45(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide + (ySide << 2);
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
			for (int y1 = texture.Length - xSide, y2 = newXside * ySide - newXside; y1 < texture.Length; y1 += 4, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 >= 0; x1 -= xSide, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
				}
			return rotated;
		}
		/// <summary>
		/// Rotates image clockwise by 90 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data where its width is the height of the source texture</returns>
		public static byte[] RotateClockwise90(this byte[] texture, int width = 0)
		{
			int ySide2 = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width),
				xSide1 = ySide2 << 2,
				xSide2 = (width < 1 ? ySide2 : texture.Length / width);
			byte[] rotated = new byte[texture.Length];
			for (int y1 = 0, y2 = xSide2 - 4; y1 < texture.Length; y1 += xSide1, y2 -= 4)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide1; x1 += 4, x2 += xSide2)
					Array.Copy(texture, x1, rotated, x2, 4);
			return rotated;
		}
		/// <summary>
		/// Rotates image clockwise by 135 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
		public static byte[] RotateClockwise135Thin(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = (xSide + (ySide << 2)) - 4;
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
			for (int y1 = texture.Length - 4, y2 = newXside * (xSide >> 2) - newXside; y1 > 0; y1 -= xSide, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 > y1 - xSide; x1 -= 4, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
				}
			return rotated;
		}
		/// <summary>
		/// Rotates image clockwise by 135 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
		public static byte[] RotateClockwise135(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide + (ySide << 2);
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
			for (int y1 = texture.Length - 4, y2 = newXside * (xSide >> 2) - newXside; y1 > 0; y1 -= xSide, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 > y1 - xSide; x1 -= 4, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
				}
			return rotated;
		}
		/// <summary>
		/// Rotates an image 180 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of identical size to source texture</returns>
		public static byte[] Rotate180(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			byte[] rotated = new byte[texture.Length];
			for (int y1 = 0, y2 = texture.Length - xSide; y1 < texture.Length; y1 += xSide, y2 -= xSide)
				for (int x1 = y1, x2 = y2 + xSide - 4; x1 < y1 + xSide; x1 += 4, x2 -= 4)
					Array.Copy(texture, x1, rotated, x2, 4);
			return rotated;
		}
		/// <summary>
		/// Rotates image counter-clockwise by 135 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
		public static byte[] RotateCounter135Thin(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide + (ySide << 2) - 4;
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
			for (int y1 = xSide - 4, y2 = newXside * ySide - newXside; y1 >= 0; y1 -= 4, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 < texture.Length; x1 += xSide, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
				}
			return rotated;
		}
		/// <summary>
		/// Rotates image counter-clockwise by 135 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
		public static byte[] RotateCounter135(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide + (ySide << 2);
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
			for (int y1 = xSide - 4, y2 = newXside * ySide - newXside; y1 >= 0; y1 -= 4, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 < texture.Length; x1 += xSide, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
				}
			return rotated;
		}
		/// <summary>
		/// Rotates image counter-clockwise by 90 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data where its width is the height of the source texture</returns>
		public static byte[] RotateCounter90(this byte[] texture, int width = 0)
		{
			int ySide2 = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width),
				xSide1 = ySide2 << 2,
				xSide2 = (width < 1 ? ySide2 : texture.Length / width);
			byte[] rotated = new byte[texture.Length];
			for (int y1 = 0, y2 = texture.Length - xSide2; y1 < texture.Length; y1 += xSide1, y2 += 4)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide1; x1 += 4, x2 -= xSide2)
					Array.Copy(texture, x1, rotated, x2, 4);
			return rotated;
		}
		/// <summary>
		/// Rotates image counter-clockwise by 45 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height - 1</returns>
		public static byte[] RotateCounter45Thin(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide + (ySide << 2) - 4;
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide)];
			for (int y1 = 0, y2 = newXside * (xSide >> 2) - newXside; y1 < texture.Length; y1 += xSide, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + newXside, 4);
				}
			return rotated;
		}
		/// <summary>
		/// Rotates image counter-clockwise by 45 degrees
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width + height</returns>
		public static byte[] RotateCounter45(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide + (ySide << 2);
			byte[] rotated = new byte[newXside * ((xSide >> 2) + ySide - 1)];
			for (int y1 = 0, y2 = newXside * (xSide >> 2) - newXside; y1 < texture.Length; y1 += xSide, y2 += newXside + 4)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 4)
				{
					Array.Copy(texture, x1, rotated, x2, 4);
					Array.Copy(texture, x1, rotated, x2 + 4, 4);
				}
			return rotated;
		}
		#endregion Rotation
		#region Isometric
		/// <summary>
		/// Skews an image for use as an isometric wall tile sloping down
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
		public static byte[] IsoSlantDown(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide * 2,
				newXside2 = xSide << 2;
			byte[] slanted = new byte[newXside * (ySide * 2 + (xSide >> 2) + 1)];
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside2)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += newXside + 8)
				{
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside + 4, 4);
					Array.Copy(slanted, x2 + newXside, slanted, x2 + newXside2, 8);
					Array.Copy(texture, x1, slanted, x2 + newXside2 + newXside + 4, 4);
				}
			return slanted;
		}
		/// <summary>
		/// Skews an image for use as a short isometric wall tile sloping down
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
		public static byte[] IsoSlantDownShort(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide * 2;
			byte[] slanted = new byte[newXside * ((xSide >> 2) + 1 + ySide)];
			for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += newXside + 8)
				{
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside + 4, 4);
					Array.Copy(texture, x1, slanted, x2 + newXside + newXside + 4, 4);
				}
			return slanted;
		}
		/// <summary>
		/// Skews an image for use as an isometric wall tile sloping up
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
		public static byte[] IsoSlantUp(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide * 2,
				newXside2 = xSide << 2;
			byte[] slanted = new byte[newXside * (ySide * 2 + (xSide >> 2) + 1)];
			for (int y1 = 0, y2 = newXside * ((xSide >> 2) + 2); y1 < texture.Length; y1 += xSide, y2 += newXside2)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 8)
				{
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside + 4, 4);
					Array.Copy(slanted, x2 - newXside, slanted, x2 - newXside2, 8);
					Array.Copy(texture, x1, slanted, x2 - newXside2 - newXside + 4, 4);
				}
			return slanted;
		}
		/// <summary>
		/// Skews an image for use as a short isometric wall tile sloping up
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of double the source texture width</returns>
		public static byte[] IsoSlantUpShort(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = xSide * 2;
			byte[] slanted = new byte[newXside * ((xSide >> 2) + 1 + ySide)];
			for (int y1 = 0, y2 = newXside * ((xSide >> 2) + 1); y1 < texture.Length; y1 += xSide, y2 += newXside)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 8)
				{
					Array.Copy(texture, x1, slanted, x2, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside + 4, 4);
					Array.Copy(texture, x1, slanted, x2 - newXside - newXside + 4, 4);
				}
			return slanted;
		}
		/// <summary>
		/// Rotates and stretches an image for use as an isometric floor tile
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data where the width is derived from the source image size by the formula "newWidth = (width + height - 1) * 2"</returns>
		public static byte[] IsoTile(this byte[] texture, int width = 0)
		{
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				ySide = (width < 1 ? xSide : texture.Length / width) >> 2,
				newXside = (xSide + (ySide << 2)) * 2 - 8;
			byte[] tile = new byte[newXside * ((xSide >> 2) + ySide)];
			for (int y1 = 0, y2 = newXside * (xSide >> 2) - newXside; y1 < texture.Length; y1 += xSide, y2 += newXside + 8)
				for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += -newXside + 8)
				{
					Array.Copy(texture, x1, tile, x2, 4);
					Array.Copy(texture, x1, tile, x2 + 4, 4);
					Array.Copy(tile, x2, tile, x2 + newXside, 8);
				}
			return tile;
		}
		#endregion Isometric
		#region Image manipulation
		/// <summary>
		/// Extracts a rectangular piece of a texture
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="x">upper left corner of selection</param>
		/// <param name="y">upper left corner of selection</param>
		/// <param name="croppedWidth">width of selection</param>
		/// <param name="croppedHeight">height of selection</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of width croppedWidth or smaller if x is smaller than zero or if x + croppedWidth extends outside the source texture</returns>
		public static byte[] Crop(this byte[] texture, int x, int y, int croppedWidth, int croppedHeight, int width = 0)
		{
			if (x < 0)
			{
				croppedWidth += x;
				x = 0;
			}
			if (croppedWidth < 1) throw new InvalidDataException("croppedWidth < 1. Was: \"" + croppedWidth + "\"");
			if (y < 0)
			{
				croppedHeight += y;
				y = 0;
			}
			if (croppedHeight < 1) throw new InvalidDataException("croppedHeight < 1. Was: \"" + croppedHeight + "\"");
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			x <<= 2; // x *= 4;
			if (x > xSide) throw new InvalidDataException("x > xSide. x: \"" + x + "\", xSide: \"" + xSide + "\"");
			int ySide = (width < 1 ? xSide : texture.Length / width) >> 2;
			if (y > ySide) throw new InvalidDataException("y > ySide. y: \"" + y + "\", ySide: \"" + ySide + "\"");
			if (y + croppedHeight > ySide)
				croppedHeight = ySide - y;
			croppedWidth <<= 2; // croppedWidth *= 4;
			if (x + croppedWidth > xSide)
				croppedWidth = xSide - x;
			byte[] cropped = new byte[croppedWidth * croppedHeight];
			for (int y1 = y * xSide + x, y2 = 0; y2 < cropped.Length; y1 += xSide, y2 += croppedWidth)
				Array.Copy(texture, y1, cropped, y2, croppedWidth);
			return cropped;
		}
		/// <summary>
		/// Makes a new texture and copies the old texture to its upper left corner
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="newWidth">width of newly resized texture</param>
		/// <param name="newHeight">height of newly resized texture</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of width newWidth</returns>
		public static byte[] Resize(this byte[] texture, int newWidth, int newHeight, int width = 0)
		{
			if (newWidth < 1) throw new InvalidDataException("newWidth cannot be smaller than 1. Was: \"" + newWidth + "\"");
			if (newHeight < 1) throw new InvalidDataException("newHeight cannot be smaller than 1. Was: \"" + newHeight + "\"");
			newWidth <<= 2; // newWidth *= 4;
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2;
			byte[] resized = new byte[newWidth * newHeight];
			if (newWidth == xSide)
				Array.Copy(texture, resized, Math.Min(texture.Length, resized.Length));
			else
			{
				int newXside = Math.Min(xSide, newWidth);
				for (int y1 = 0, y2 = 0; y1 < texture.Length && y2 < resized.Length; y1 += xSide, y2 += newWidth)
					Array.Copy(texture, y1, resized, y2, newXside);
			}
			return resized;
		}
		/// <summary>
		/// Tile an image
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="xFactor">number of times to tile horizontally</param>
		/// <param name="yFactor">number of times to tile vertically</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width * xFactor</returns>
		public static byte[] Tile(this byte[] texture, int xFactor = 2, int yFactor = 2, int width = 0)
		{
			if (xFactor < 1 || yFactor < 1 || (xFactor < 2 && yFactor < 2)) return (byte[])texture.Clone();
			byte[] tiled = new byte[texture.Length * xFactor * yFactor];
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				newXside = xSide * xFactor;
			if (xFactor > 1)
				for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXside)
					for (int x = 0; x < newXside; x += xSide)
						Array.Copy(texture, y1, tiled, y2 + x, xSide);
			else
				Array.Copy(texture, tiled, texture.Length);
			if (yFactor > 1)
			{
				int xScaledLength = texture.Length * xFactor;
				for (int y = xScaledLength; y < tiled.Length; y += xScaledLength)
					Array.Copy(tiled, 0, tiled, y, xScaledLength);
			}
			return tiled;
		}
		/// <summary>
		/// Simple nearest-neighbor upscaling by integer multipliers
		/// </summary>
		/// <param name="texture">raw rgba8888 pixel data of source image</param>
		/// <param name="xFactor">horizontal scaling factor</param>
		/// <param name="yFactor">vertical scaling factor</param>
		/// <param name="width">width of texture or 0 to assume square texture</param>
		/// <returns>new raw rgba8888 pixel data of newWidth = width * xFactor</returns>
		public static byte[] Upscale(this byte[] texture, int xFactor, int yFactor, int width = 0)
		{
			if (xFactor < 1 || yFactor < 1 || (xFactor < 2 && yFactor < 2)) return (byte[])texture.Clone();
			int xSide = (width < 1 ? (int)Math.Sqrt(texture.Length >> 2) : width) << 2,
				newXside = xSide * xFactor,
				newXsideYfactor = newXside * yFactor;
			byte[] scaled = new byte[texture.Length * yFactor * xFactor];
			if (xFactor < 2)
				for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXsideYfactor)
					for (int z = y2; z < y2 + newXsideYfactor; z += newXside)
						Array.Copy(texture, y1, scaled, z, xSide);
			else
			{
				int xFactor4 = xFactor << 2;
				for (int y1 = 0, y2 = 0; y1 < texture.Length; y1 += xSide, y2 += newXsideYfactor)
				{
					for (int x1 = y1, x2 = y2; x1 < y1 + xSide; x1 += 4, x2 += xFactor4)
						for (int z = 0; z < xFactor4; z += 4)
							Array.Copy(texture, x1, scaled, x2 + z, 4);
					for (int z = y2 + newXside; z < y2 + newXsideYfactor; z += newXside)
						Array.Copy(scaled, y2, scaled, z, newXside);
				}
			}
			return scaled;
		}
		#endregion Image manipulation
		#region Utilities
		/// <summary>
		/// Compute power of two greater than or equal to `n`
		/// </summary>
		public static uint NextPowerOf2(this uint n)
		{
			n--; // decrement `n` (to handle the case when `n` itself is a power of 2)
				 // set all bits after the last set bit
			n |= n >> 1;
			n |= n >> 2;
			n |= n >> 4;
			n |= n >> 8;
			n |= n >> 16;
			return ++n; // increment `n` and return
		}
		public static byte R(this int color) => (byte)(color >> 24);
		public static byte G(this int color) => (byte)(color >> 16);
		public static byte B(this int color) => (byte)(color >> 8);
		public static byte A(this int color) => (byte)color;
		public static int Color(byte r, byte g, byte b, byte a) => r << 24 | g << 16 | b << 8 | a;
		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Index2ByteArray(this byte[] index, int[] palette)
		{
			byte[] bytes = new byte[index.Length << 2];
			for (int i = 0, j = 0; i < index.Length; i++)
			{
				bytes[j++] = (byte)(palette[index[i]] >> 24);
				bytes[j++] = (byte)(palette[index[i]] >> 16);
				bytes[j++] = (byte)(palette[index[i]] >> 8);
				bytes[j++] = (byte)palette[index[i]];
			}
			return bytes;
		}
		/// <param name="index">Palette indexes (one byte per pixel)</param>
		/// <param name="palette">256 rgba8888 color values</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static int[] Index2IntArray(this byte[] index, int[] palette)
		{
			int[] ints = new int[index.Length];
			for (int i = 0; i < index.Length; i++)
				ints[i] = palette[index[i]];
			return ints;
		}
		/// <param name="ints">rgba8888 color values (one int per pixel)</param>
		/// <returns>rgba8888 texture (four bytes per pixel)</returns>
		public static byte[] Int2ByteArray(this int[] ints)
		{
			byte[] bytes = new byte[ints.Length << 2];
			for (int i = 0, j = 0; i < ints.Length; i++)
			{
				bytes[j++] = (byte)(ints[i] >> 24);
				bytes[j++] = (byte)(ints[i] >> 16);
				bytes[j++] = (byte)(ints[i] >> 8);
				bytes[j++] = (byte)ints[i];
			}
			return bytes;
		}
		/// <param name="bytes">rgba8888 color values (four bytes per pixel)</param>
		/// <returns>rgba8888 texture (one int per pixel)</returns>
		public static int[] Byte2IntArray(this byte[] bytes)
		{
			int[] ints = new int[bytes.Length >> 2];
			for (int i = 0, j = 0; i < bytes.Length; i += 4)
				ints[j++] = (bytes[i] << 24)
					| (bytes[i + 1] << 16)
					| (bytes[i + 2] << 8)
					| bytes[i + 3];
			return ints;
		}
		public static T[] ConcatArrays<T>(params T[][] list)
		{
			T[] result = new T[list.Sum(a => a.Length)];
			for (int i = 0, offset = 0; i < list.Length; i++)
			{
				list[i].CopyTo(result, offset);
				offset += list[i].Length;
			}
			return result;
		}
		public static int[] LoadPalette(string @string) => LoadPalette(new MemoryStream(Encoding.UTF8.GetBytes(@string)));
		public static int[] LoadPalette(Stream stream)
		{
			int[] result;
			using (StreamReader streamReader = new StreamReader(stream))
			{
				string line;
				while (string.IsNullOrWhiteSpace(line = streamReader.ReadLine().Trim())) { }
				if (!line.Equals("JASC-PAL") || !streamReader.ReadLine().Trim().Equals("0100"))
					throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
				if (!int.TryParse(streamReader.ReadLine()?.Trim(), out int numColors)
				 || numColors != 256)
					throw new InvalidDataException("Palette stream does not contain exactly 256 colors.");
				result = new int[numColors];
				for (int i = 0; i < numColors; i++)
				{
					string[] tokens = streamReader.ReadLine()?.Trim().Split(' ');
					if (tokens == null || tokens.Length != 3
						|| !byte.TryParse(tokens[0], out byte r)
						|| !byte.TryParse(tokens[1], out byte g)
						|| !byte.TryParse(tokens[2], out byte b))
						throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
					result[i] = (r << 24)
						| (g << 16)
						| (b << 8)
						| (i == 255 ? 0 : 255);
				}
			}
			return result;
		}
		#endregion Utilities
	}
}
