﻿using System.Drawing;
using System.Drawing.Drawing2D;

namespace DotSpatial.Controls.Extensions
{
	public static class GraphicsHelper
	{
		public static void RotateAt(Graphics gr, float cx, float cy, float angle)
		{
			gr.ResetTransform();
			gr.TranslateTransform(-cx, -cy, MatrixOrder.Append);
			gr.RotateTransform(angle, MatrixOrder.Append);
			gr.TranslateTransform(cx, cy, MatrixOrder.Append);
		}
	}
}