// ********************************************************************************************************
// Product Name: DotSpatial.Controls.dll
// Description:  The Windows Forms user interface controls like the map, legend, toolbox, ribbon and others.
// ********************************************************************************************************
// The contents of this file are subject to the MIT License (MIT)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is from MapWindow.dll version 6.0
//
// The Initial Developer of this Original Code is Ted Dunsford. Created 4/18/2009 2:09:24 PM
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
//
// ********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Drawing;

namespace DotSpatial.Controls
{
	/// <summary>
	/// Contains methods to remove duplicates
	/// </summary>
	public static class DuplicationPreventer
	{
		private const int X = 0;
		private const int Y = 1;

		/// <summary>
		/// Cycles through the PointF points, where necessary and removes duplicate points
		/// that are found at the integer level.
		/// </summary>
		public static IEnumerable<Point> Clean(IEnumerable<PointF> points)
		{
			var previous = Point.Empty;
			var isFirst = true;
			foreach (var point in points)
			{
				if (float.IsNaN(point.X) || float.IsNaN(point.Y)) continue;
				var pt = new Point { X = Convert.ToInt32(point.X), Y = Convert.ToInt32(point.Y) };
				if (isFirst || pt.X != previous.X || pt.Y != previous.Y)
				{
					isFirst = false;
					previous = pt;
					yield return pt;
				}
			}
		}

		/// <summary>
		/// Cleans the enumerable of points by removing duplicates
		/// </summary>
		public static IEnumerable<Point> Clean(IEnumerable<double[]> points)
		{
			var previous = Point.Empty;
			var isFirst = true;
			foreach (var point in points)
			{
				if (double.IsNaN(point[X]) || double.IsNaN(point[Y])) continue;
				var pt = new Point { X = Convert.ToInt32(point[X]), Y = Convert.ToInt32(point[Y]) };
				if (isFirst || pt.X != previous.X || pt.Y != previous.Y)
				{
					isFirst = false;
					previous = pt;
					yield return pt;
				}
			}
		}
	}
}