﻿using DotSpatial.Symbology;

namespace DotSpatial.Controls.Extensions
{
	public static class PointLayerHelper
	{
		public static IPointSymbolizer CreatePointSymbolizer(IPointCategory pc, IDrawnState ds)
		{
			IPointSymbolizer ps = pc.Symbolizer;
			if (ds.IsSelected)
			{
				ps = pc.SelectionSymbolizer;
			}
			return ps;
		}

		public static bool Validate(IPointCategory pc, IPointCategory category)
		{
			if (pc == null)
				return false;
			return pc == category;
		}

		public static bool Validate(IDrawnState ds)
		{
			if (ds == null)
				return false;
			if (!ds.IsVisible)
				return false;
			return ds.SchemeCategory != null;
		}

	}
}