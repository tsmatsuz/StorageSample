using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WordSampleWebRole.Models;

namespace WordSampleWebRole.Controllers
{
    public class ProductController : Controller
    {
        //
        // GET: /Product/

        public ActionResult Confirm(int id)
        {
            var product = (from p in Helper.GetCurrentProductList(Session)
                          where p.ProductId == id
                          select p).FirstOrDefault<ProductItemModel>();
            return View(product);
        }

        [HttpPost]
        public ActionResult UpdateCount(ProductItemModel model)
        {
            var product = (from p in Helper.GetCurrentProductList(Session)
                           where p.ProductId == model.ProductId
                           select p).FirstOrDefault<ProductItemModel>();
            product.ProductCount = model.ProductCount;

            if (ModelState.IsValid)
                return RedirectToAction("Edit", "Deliver");
            else
                return View("Confirm", product);
        }

        public ActionResult Delete(int id)
        {
            var productList = Helper.GetCurrentProductList(Session);
            var product = (from p in Helper.GetCurrentProductList(Session)
                           where p.ProductId == id
                           select p).FirstOrDefault<ProductItemModel>();
            productList.Remove(product);

            return RedirectToAction("Edit", "Deliver");
        }
    }
}
