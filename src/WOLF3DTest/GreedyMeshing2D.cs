﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WOLF3DTest
{
	public class GreedyMeshing2D
	{
		/*
static func is_range_equal(row: Array, xmin: int, xmax: int, val) -> bool:
	for x in range(xmin, xmax):
		if row[x] != val:
			return false
	return true


static func greedy_2d(grid: Array) -> Array:
	var quads := []
	grid = grid.duplicate(true)

	for y in len(grid):
		var row = grid[y]

		for x in len(row):
			var c = row[x]

			if c == 0:
				continue

			var rx = x + 1
			while rx < len(row) and row[rx] == c:
				rx += 1
			
			var ry = y + 1
			while ry < len(grid) and is_range_equal(grid[ry], x, rx, c):
				ry += 1

			quads.append(Rect2(x, y, rx - x, ry - y))

			for j in range(y, ry):
				var row2 = grid[j]
				for i in range(x, rx):
					row2[i] = 0

	return quads
		*/
	}
}
