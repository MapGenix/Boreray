using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotSpatial.Controls.Extensions.Map
{
	public static class StringFactory
	{
		public static string PrefixWithEllipsis(string text, int length)
		{
			if (text.Length <= length) return text;

			return "..." + text.Substring(Math.Max(2, text.Length - length - 3));
		}
	}
}
