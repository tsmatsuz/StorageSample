using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WordSampleWebRole.Models;

namespace WordSampleWebRole.Controllers
{
    public class DeliverController : Controller
    {
        //
        // GET: /Deliver/

        public ActionResult Edit()
        {
            DeliverInfoModel model = Helper.GetCurrentDeliverInfo(Session);
            if (model == null)
                return View("Edit");
            else
                return View("Edit", model);
        }

        public ActionResult Confirm()
        {
            DeliverInfoModel model = Helper.GetCurrentDeliverInfo(Session);
            ViewBag.CostAmount = (from p in Helper.GetCurrentProductList(Session)
                                  select p.ProductUnitPrice * p.ProductCount).Sum();
            return View(model);
        }

        [HttpPost]
        public ActionResult Confirm(DeliverInfoModel model)
        {
            Helper.SetCurrentDeliverInfo(Session, model);
            ViewBag.CostAmount = (from p in Helper.GetCurrentProductList(Session)
                                  select p.ProductUnitPrice * p.ProductCount).Sum();
            if (ModelState.IsValid)
                return View(model);
            else
                return View("Edit", model);
        }
    }
}
