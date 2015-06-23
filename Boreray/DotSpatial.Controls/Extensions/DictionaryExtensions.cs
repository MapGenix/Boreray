using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using DotSpatial.Data;
using DotSpatial.Symbology;
using DotSpatial.Topology;

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
