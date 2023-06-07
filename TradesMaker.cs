using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Mods;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.World.Entities;
using Mafi.Core.World.QuickTrade;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace TradesMaker
{
  public sealed class TradesMaker : DataOnlyMod
  {
    public override string Name => "Trades Maker";

    public override int Version => 1;

    private static readonly string ModDirectory;

    private static QuickTradeDefaults loadDefaultsFromXml(string filepathname)
    {
      QuickTradeDefaults defaults = new QuickTradeDefaults();

      XmlDocument xDoc = new XmlDocument();
      xDoc.Load(filepathname);

      XmlNode tradesconfig = xDoc.Descendants("tradesconfig").Single();

      defaults.cooldownPerStep = tradesconfig.IntValue("defaultCooldownPerStep").Value;
      defaults.costMultiplierPerStep = tradesconfig.DblValue("defaultCostMultiplierPerStep").Value;
      defaults.unityMultiplierPerStep = tradesconfig.DblValue("defaultUnityMultiplierPerStep").Value;
      defaults.maxSteps = tradesconfig.IntValue("defaultMaxSteps").Value;
      defaults.minReputationRequired = tradesconfig.IntValue("defaultMinReputationRequired").Value;
      defaults.tradesPerStep = tradesconfig.IntValue("defaultTradesPerStep").Value;
      defaults.ignoreTradeMultipliers = tradesconfig.IntValue("defaultIgnoreTradeMultipliers").Value == 1;

      return defaults;
    }

    private static IEnumerable<XmlQuickTradeElement> loadXml(string filepathname, QuickTradeDefaults tradeDefaults)
    {
      List<XmlQuickTradeElement> quickTrades = new List<XmlQuickTradeElement>();

      XmlDocument xDoc = new XmlDocument();
      xDoc.Load(filepathname);

      foreach (XmlNode trade in xDoc.Descendants("tradesconfig").Single().Descendants("trade").AsParallel())
      {
        foreach (XmlNode quick in trade.Descendants("quick").AsParallel())
        {
          XmlQuickTradeElement element = new XmlQuickTradeElement();

          element.village = trade.IntValue("village").Value;
          element.productToBuy = quick.StrValue("productToBuy");
          element.productToPayWith = quick.StrValue("productToPayWith");
          element.buyQty = quick.IntValue("buyQty").Value;
          element.payQty = quick.IntValue("payQty").Value;
          element.upointsPerTrade = quick.DblValue("upointsPerTrade").Value;
          element.cooldownPerStep = quick.IntValue("cooldownPerStep").GetValueOrDefault(tradeDefaults.cooldownPerStep);
          element.costMultiplierPerStep = quick.DblValue("costMultiplierPerStep").GetValueOrDefault(tradeDefaults.costMultiplierPerStep);
          element.unityMultiplierPerStep = quick.DblValue("unityMultiplierPerStep").GetValueOrDefault(tradeDefaults.unityMultiplierPerStep);
          element.maxSteps = quick.IntValue("maxSteps").GetValueOrDefault(tradeDefaults.maxSteps);
          element.minReputationRequired = quick.IntValue("minReputationRequired").GetValueOrDefault(tradeDefaults.minReputationRequired);
          element.tradesPerStep = quick.IntValue("tradesPerStep").GetValueOrDefault(tradeDefaults.tradesPerStep);

          int? ignoreTradeMultipliers = quick.IntValue("ignoreTradeMultipliers");

          if (ignoreTradeMultipliers.HasValue)
            element.ignoreTradeMultipliers = ignoreTradeMultipliers.Value == 1;
          else
            element.ignoreTradeMultipliers = tradeDefaults.ignoreTradeMultipliers;

          quickTrades.Add(element);
        }
      }

      return quickTrades;
    }

    private static ProductProto.ID getProductOrThrow(IDictionary<string, ProductProto.ID> productsDictionary, string productName)
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

    static TradesMaker()
    {
      ModDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Captain of Industry/Mods/TradesMaker");
    }

    public TradesMaker(CoreMod coreMod, BaseMod baseMod)
    {
      // No dependencies, really.
    }

    public override void RegisterPrototypes(ProtoRegistrator registrator)
    {
      IDictionary<string, ProductProto.ID> allProducts;

      exportProductsIds(out allProducts);

      Dictionary<int, WorldMapVillageProto> villages = new Dictionary<int, WorldMapVillageProto>
      {
        { 1, registrator.PrototypesDb.GetOrThrow<WorldMapVillageProto>(Ids.World.Settlement1) },
        { 2, registrator.PrototypesDb.GetOrThrow<WorldMapVillageProto>(Ids.World.Settlement2) },
        { 3, registrator.PrototypesDb.GetOrThrow<WorldMapVillageProto>(Ids.World.Settlement3) },
        { 4, registrator.PrototypesDb.GetOrThrow<WorldMapVillageProto>(Ids.World.Settlement4) },
        { 5, registrator.PrototypesDb.GetOrThrow<WorldMapVillageProto>(Ids.World.Settlement5) }
      };

      string xmlConfigFile = Directory.GetFiles(ModDirectory, "*.xml").First();

      QuickTradeDefaults tradeDefaults = loadDefaultsFromXml(xmlConfigFile);

      IEnumerable<XmlQuickTradeElement> quickTrades = loadXml(xmlConfigFile, tradeDefaults);

      foreach (var trade in quickTrades)
      {
        try
        {
          ProductProto.ID productInDict = getProductOrThrow(allProducts, trade.productToBuy);

          ProductProto productToBuy = registrator.PrototypesDb.GetOrThrow<ProductProto>(productInDict);

          productInDict = getProductOrThrow(allProducts, trade.productToPayWith);

          ProductProto productToPayWith = registrator.PrototypesDb.GetOrThrow<ProductProto>(productInDict);

          WorldMapVillageProto tradeVillage = villages[trade.village];

          Lyst<QuickTradePairProto> trades = new Lyst<QuickTradePairProto>(tradeVillage.QuickTrades.AsEnumerable())
          {
            new QuickTradePairProto(new EntityProto.ID("TradesMaker_" + DateTime.Now.Ticks)
                  , new ProductQuantity(productToBuy, trade.buyQty.Quantity())
                  , new ProductQuantity(productToPayWith, trade.payQty.Quantity())
                  , trade.upointsPerTrade.Upoints()
                  , trade.maxSteps.Value
                  , trade.minReputationRequired.Value
                  , trade.tradesPerStep.Value
                  , trade.cooldownPerStep.Value.Ticks()
                  , trade.costMultiplierPerStep.Value.Percent()
                  , trade.unityMultiplierPerStep.Value.Percent()
                  , trade.ignoreTradeMultipliers.Value
            )
          };

          tradeVillage.QuickTrades = ImmutableArray.CreateRange(trades);
        }
        catch (ApplicationException ex)
        {
          Log.Error(ex.Message);
        }
        catch (Exception ex)
        {
          Log.Error($"TradesMaker: unexpected error!" + Environment.NewLine + ex.ToString());
        }
      }
    }

    private static void exportProductsIds(out IDictionary<string, ProductProto.ID> productsDictionary)
    {
      using (StreamWriter outFile = new StreamWriter(Path.Combine(ModDirectory, "ProductsIds.txt")))
      {
        productsDictionary = new Dictionary<string, ProductProto.ID>();

        Type products = typeof(Ids.Products);

        foreach (var field in products.GetFields().OrderBy(x => x.Name).AsParallel())
        {
          object productId = field.GetValue(null);

          if (productId is ProductProto.ID)
          {
            productsDictionary.Add(field.Name, (ProductProto.ID)productId);

            outFile.WriteLine(field.Name);
          }
        }
      }
    }
  }
}
