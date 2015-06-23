using System;
using System.Collections.Generic;

namespace DotSpatial.Controls.Extensions
{
	public static class DictionaryExtensions
	{
		public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, Func<TKey, TValue> valueFactory)
		{
			TValue value;
			if (!dic.TryGetValue(key, out value))
			{
				value = valueFactory(key);
				dic.Add(key, value);
			}
			return value;
		}
	}
}