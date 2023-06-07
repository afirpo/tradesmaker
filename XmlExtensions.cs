using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TradesMaker
{
  internal static class XmlExtensions
  {
    public static IEnumerable<XmlNode> Descendants(this XmlNode node, string name)
    {
      return node.ChildNodes
        .OfType<XmlNode>()
        .Where(x => name.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));
    }

    public static int? IntValue(this XmlNode node, string attribute)
    {
      XmlAttribute attr = node.Attributes[attribute];

      if (attr == null)
        return null;

      return string.IsNullOrWhiteSpace(attr.Value) ? null : (int?)int.Parse(attr.Value, NumberFormatInfo.InvariantInfo);
    }

    public static double? DblValue(this XmlNode node, string attribute)
    {
      XmlAttribute attr = node.Attributes[attribute];

      if (attr == null)
        return null;

      return string.IsNullOrWhiteSpace(attr.Value) ? null : (double?)double.Parse(attr.Value, NumberFormatInfo.InvariantInfo);
    }

    public static string StrValue(this XmlNode node, string attribute)
    {
      XmlAttribute attr = node.Attributes[attribute];

      return attr != null ? attr.Value : null;
    }
  }

}
