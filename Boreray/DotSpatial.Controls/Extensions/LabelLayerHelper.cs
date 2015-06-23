using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using DotSpatial.Data;
using DotSpatial.Symbology;

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
  }
}
