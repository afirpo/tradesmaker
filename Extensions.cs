using Mafi.Core.Products;
using Mafi;
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

  internal static class UtilityExtensions
  {
    public static ProductProto.ID getProductOrThrow(this IDictionary<string, ProductProto.ID> productsDictionary, string productName)
    {
      KeyValuePair<string, ProductProto.ID> entry = productsDictionary
        .SingleOrDefault(x => x.Key.Equals(productName, StringComparison.InvariantCultureIgnoreCase));

      if (entry.Key == null)
      {
        string message = $"TradesMaker: the product {productName} seems NOT to be a tradeable product.";

        Log.Warning(message);

        throw new ApplicationException(message);
      }

      return entry.Value;
    }
  }
}
