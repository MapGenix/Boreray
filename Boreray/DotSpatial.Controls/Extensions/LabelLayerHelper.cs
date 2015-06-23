using DotSpatial.Data;
using DotSpatial.Symbology;
using DotSpatial.Topology;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DotSpatial.Controls.Extensions
{
	public static class LabelLayerHelper
	{
		public static float GetAngleToRotate(ILabelSymbolizer symb, IFeature feature)
		{
			if (symb.UseAngle)
			{
				try
				{
					return Convert.ToSingle(symb.Angle);
				}
				catch (Exception)
				{
					return 0;
				}
			}

			if (symb.UseLabelAngleField)
			{
				var angleField = symb.LabelAngleField;
				if (String.IsNullOrEmpty(angleField))
					return 0;

				try
				{
					return Convert.ToSingle(feature.DataRow[angleField]);
				}
				catch (Exception)
				{
					return 0;
				}
			}

			return 0;
		}

		public static void RotateAt(Graphics gr, float cx, float cy, float angle)
		{
			gr.ResetTransform();
			gr.TranslateTransform(-cx, -cy, MatrixOrder.Append);
			gr.RotateTransform(angle, MatrixOrder.Append);
			gr.TranslateTransform(cx, cy, MatrixOrder.Append);
		}

		public static string GetLabelText(IFeature feature, ILabelCategory category, ILabelSymbolizer symb)
		{
			var useFloatingFormat = !string.IsNullOrWhiteSpace(symb.FloatingFormat);
			var result = category.Expression;
			if (feature != null && ContainsExpression(result))
			{
				foreach (DataColumn dc in feature.DataRow.Table.Columns)
				{
					var curColumnReplacement = "[" + dc.ColumnName + "]";

					// Check that this column used in expression
					if (!result.Contains(curColumnReplacement))
						continue;

					var currValue = feature.DataRow[dc.ColumnName];
					if (useFloatingFormat &&
						(dc.DataType == typeof(double) ||
						dc.DataType == typeof(float)))
					{
						try
						{
							var dv = Convert.ToDouble(currValue);
							currValue = dv.ToString(symb.FloatingFormat);
						}
						catch (Exception)
						{
							currValue = SafeToString(currValue);
						}
					}
					else
					{
						currValue = SafeToString(currValue);
					}

					result = result.Replace(curColumnReplacement, (string)currValue);
					if (!ContainsExpression(result))
						break;
				}
			}
			return result;
		}

		public static bool ContainsExpression(string inStr)
		{
			if (String.IsNullOrEmpty(inStr))
				return false;
			const char symb1 = ']';
			const char symb2 = '[';
			bool s1 = false, s2 = false;
			foreach (var t in inStr)
			{
				if (t == symb1)
				{
					s1 = true;
					if (s1 && s2) return true;
				}
				else if (t == symb2)
				{
					s2 = true;
					if (s1 && s2) return true;
				}
			}

			return false;
		}

		public static string SafeToString(object value)
		{
			if (value == null || value == DBNull.Value)
			{
				return string.Empty;
			}
			return value.ToString();
		}

		public static PointF Position(ILabelSymbolizer symb, SizeF size)
		{
			ContentAlignment orientation = symb.Orientation;
			float x = symb.OffsetX;
			float y = -symb.OffsetY;
			switch (orientation)
			{
				case ContentAlignment.TopLeft:
					return new PointF(-size.Width + x, -size.Height + y);

				case ContentAlignment.TopCenter:
					return new PointF(-size.Width / 2 + x, -size.Height + y);

				case ContentAlignment.TopRight:
					return new PointF(0 + x, -size.Height + y);

				case ContentAlignment.MiddleLeft:
					return new PointF(-size.Width + x, -size.Height / 2 + y);

				case ContentAlignment.MiddleCenter:
					return new PointF(-size.Width / 2 + x, -size.Height / 2 + y);

				case ContentAlignment.MiddleRight:
					return new PointF(0 + x, -size.Height / 2 + y);

				case ContentAlignment.BottomLeft:
					return new PointF(-size.Width + x, 0 + y);

				case ContentAlignment.BottomCenter:
					return new PointF(-size.Width / 2 + x, 0 + y);

				case ContentAlignment.BottomRight:
					return new PointF(0 + x, 0 + y);
			}
			return new PointF(0, 0);
		}

		public static RectangleF PlaceLineLabel(IBasicGeometry lineString, Func<SizeF> labelSize, MapArgs e, ILabelSymbolizer symb)
		{
			ILineString ls = Geometry.FromBasicGeometry(lineString) as ILineString;
			if (ls == null) return RectangleF.Empty;
			Coordinate c;
			if (symb.LabelPlacementMethod == LabelPlacementMethod.Centroid)
				c = ls.Centroid.Coordinate;
			else if (symb.LabelPlacementMethod == LabelPlacementMethod.InteriorPoint)
				c = ls.InteriorPoint.Coordinate;
			else
				c = ls.Envelope.Center();

			var lz = labelSize();
			PointF adjustment = LabelLayerHelper.Position(symb, lz);
			float x = Convert.ToSingle((c.X - e.MinX) * e.Dx) + adjustment.X;
			float y = Convert.ToSingle((e.MaxY - c.Y) * e.Dy) + adjustment.Y;
			return new RectangleF(x, y, lz.Width, lz.Height);
		}

		public static RectangleF PlacePointLabel(IBasicGeometry f, MapArgs e, Func<SizeF> labelSize, ILabelSymbolizer symb)
		{
			Coordinate c = f.GetBasicGeometryN(1).Coordinates[0];
			if (e.GeographicExtents.Intersects(c) == false) return RectangleF.Empty;
			var lz = labelSize();
			PointF adjustment = LabelLayerHelper.Position(symb, lz);
			float x = Convert.ToSingle((c.X - e.MinX) * e.Dx) + adjustment.X;
			float y = Convert.ToSingle((e.MaxY - c.Y) * e.Dy) + adjustment.Y;
			return new RectangleF(x, y, lz.Width, lz.Height);
		}


	}
}