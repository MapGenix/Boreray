using DotSpatial.Symbology;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DotSpatial.Controls.Extensions
{
	public class Caches
	{
		private readonly Dictionary<string, Font> _symbFonts = new Dictionary<string, Font>();
		private readonly Dictionary<Color, Brush> _solidBrushes = new Dictionary<Color, Brush>();
		private readonly Dictionary<Color, Pen> _pens = new Dictionary<Color, Pen>();

		public Font GetFont(ILabelSymbolizer symb)
		{
			var fontDesc = String.Format("{0};{1};{2}", symb.FontFamily, symb.FontSize, symb.FontStyle);
			return _symbFonts.GetOrAdd(fontDesc, _ => symb.GetFont());
		}

		public Brush GetSolidBrush(Color color)
		{
			return _solidBrushes.GetOrAdd(color, _ => new SolidBrush(color));
		}

		public Pen GetPen(Color color)
		{
			return _pens.GetOrAdd(color, _ => new Pen(color));
		}
	}
}