using Mafi;
using Mafi.Base;
using Mafi.Core.Mods;
using Mafi.Core.Products;
using Mafi.Core.UnlockingTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TradesMaker.Extra
{
    internal class StoragePatch : IModData
    {
        private static readonly ProductProto.ID[] storableProductsIds = {
            Ids.Products.Exhaust
            , Ids.Products.CoreFuel
            , Ids.Products.CoreFuelDirty
            , Ids.Products.BlanketFuel
            , Ids.Products.BlanketFuelEnriched
        };

        private static bool setStorable(ref ProtoRegistrator registrator, ref ProductProto.ID productId, bool storable = true)
        {
            Log.Info($"TradesMaker: trying to set storable flag for product {productId}");

            try
            {
                ProductProto product;
                if (!registrator.PrototypesDb.TryGetProto(productId, out product))
                {
                    // It's an unexpected behaviour, but the rest of the Mod could live
                    // even without this part.
                    // TODO: add a config option to choose if this must throw an Exception and [perhaps] die horribly.
                    Log.Error($"TradesMaker: cannot get Prototype for ID {productId}");
                    return false;
                }

                product
                    .GetType()
                    .GetField(nameof(product.IsStorable), BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.GetField
                        | BindingFlags.SetField
                        | BindingFlags.GetProperty
                        | BindingFlags.SetProperty)
                    .SetValue(product, storable);

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);

                return false;
            }
        }

        public void RegisterData(ProtoRegistrator registrator)
        {
            if (!storableProductsIds.All(id => setStorable(ref registrator, ref id)))
            {
                // TODO: implement painful rollbacks - there's a dirty, uncertain and maybe half-baked situation here!
                Log.Error($"TradesMaker: data registration failed for the storable products prototypes.");
            }
        }
    }
}
