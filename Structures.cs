using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesMaker
{
  [Serializable]
  internal struct QuickTradeDefaults
  {
    public double costMultiplierPerStep, unityMultiplierPerStep;

    public int cooldownPerStep, maxSteps, minReputationRequired, tradesPerStep;

    public bool ignoreTradeMultipliers;
  }

  [Serializable]
  internal struct XmlQuickTradeElement
  {
    public int village;

    public string productToBuy, productToPayWith;

    public int buyQty, payQty;

    public double upointsPerTrade;

    public double? costMultiplierPerStep, unityMultiplierPerStep;

    public int? cooldownPerStep, maxSteps, minReputationRequired, tradesPerStep;

    public bool? ignoreTradeMultipliers;
  }

}
