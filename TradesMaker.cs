using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Mods;
using Mafi.Core.Products;
using Mafi.Core.World.Entities;
using Mafi.Core.World.QuickTrade;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace TradesMaker
{
  public sealed class TradesMaker : DataOnlyMod
  {
    public override string Name => "Trades Maker";

    public override int Version => 2;

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

      foreach (XmlNode trade in xDoc.Descendants("tradesconfig").Single().Descendants("trade"))
      {
        foreach (XmlNode quick in trade.Descendants("quick"))
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

      Dictionary<int, Lyst<QuickTradePairProto>> tradesByVillages = new Dictionary<int, Lyst<QuickTradePairProto>>();

      try
      {
        foreach (IGrouping<int, XmlQuickTradeElement> groupedTrades in quickTrades.GroupBy(x => x.village))
        {
          WorldMapVillageProto tradeVillage = villages[groupedTrades.Key];

          IEnumerable<QuickTradePairProto> existingTrades = tradeVillage.QuickTrades.AsEnumerable();

          tradesByVillages.Add(groupedTrades.Key, new Lyst<QuickTradePairProto>(existingTrades));

          Lyst<QuickTradePairProto> trades = tradesByVillages[groupedTrades.Key];

          foreach (XmlQuickTradeElement trade in groupedTrades)
          {
            ProductProto.ID productInDict = allProducts.getProductOrThrow(trade.productToBuy);

            ProductProto productToBuy = registrator.PrototypesDb.GetOrThrow<ProductProto>(productInDict);

            productInDict = allProducts.getProductOrThrow(trade.productToPayWith);

            ProductProto productToPayWith = registrator.PrototypesDb.GetOrThrow<ProductProto>(productInDict);

            // I prefer to use 'StringBuilder' even for this simple task
            // instead of allocate and concatenate new strings one at a time.
            // See more at:
            // https://learn.microsoft.com/en-us/dotnet/standard/base-types/stringbuilder
            StringBuilder quickTradeId = new StringBuilder("TradesMaker");

            quickTradeId
              .Append(trade.village)
              .Append(productToBuy.Id.Value)
              .Append(productToPayWith.Id.Value);

            QuickTradePairProto quickTradePair = new QuickTradePairProto(new EntityProto.ID(quickTradeId.ToString())
              , new ProductQuantity(productToBuy, trade.buyQty.Quantity())
              , new ProductQuantity(productToPayWith, trade.payQty.Quantity())
              , trade.upointsPerTrade.Upoints()
              , trade.maxSteps.Value
              , trade.minReputationRequired.Value
              , trade.tradesPerStep.Value
              , trade.cooldownPerStep.Value.Ticks()
              , trade.costMultiplierPerStep.Value.Percent()
              , trade.unityMultiplierPerStep.Value.Percent()
              , trade.ignoreTradeMultipliers.Value);

            trades.Add(quickTradePair);

            registrator.PrototypesDb.Add(quickTradePair);
          }
        }

        foreach (KeyValuePair<int, Lyst<QuickTradePairProto>> trade in tradesByVillages)
        {
          WorldMapVillageProto tradeVillage = villages[trade.Key];

          tradeVillage.QuickTrades = ImmutableArray.CreateRange(trade.Value);
        }
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

    private static void exportProductsIds(out IDictionary<string, ProductProto.ID> productsDictionary)
    {
      using (StreamWriter outFile = new StreamWriter(Path.Combine(ModDirectory, "ProductsIds.txt")))
      {
        productsDictionary = new Dictionary<string, ProductProto.ID>();

        Type products = typeof(Ids.Products);

        foreach (FieldInfo field in products.GetFields().OrderBy(x => x.Name))
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
