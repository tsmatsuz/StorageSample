using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WordSampleWebRole.Models;

namespace WordSampleWebRole
{
    public class Helper
    {
        // because of SharePoint restriction to compatible browser ...
        public const string UserAgentForSPO = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";

        public static void SetCurrentDeliverInfo(HttpSessionStateBase session, DeliverInfoModel item)
        {
            session["DeliverInfo"] = item;
        }

        public static DeliverInfoModel GetCurrentDeliverInfo(HttpSessionStateBase session)
        {
            return (DeliverInfoModel)session["DeliverInfo"];
        }

        public static List<ProductItemModel> GetCurrentProductList(HttpSessionStateBase session)
        {
            List<ProductItemModel> products;
            if (session["Products"] == null)
            {
                products = new List<ProductItemModel>();
                products.Add(new ProductItemModel() { ProductId = 1, ProductName = "ペン", ProductUnitPrice = 180, ProductCount = 3 });
                products.Add(new ProductItemModel() { ProductId = 2, ProductName = "はさみ", ProductUnitPrice = 300, ProductCount = 1 });
                products.Add(new ProductItemModel() { ProductId = 3, ProductName = "消しゴム", ProductUnitPrice = 50, ProductCount = 1 });
                session["Products"] = products;
            }
            else
            {
                products = (List<ProductItemModel>)session["Products"];
            }

            return products;
        }
    }
}