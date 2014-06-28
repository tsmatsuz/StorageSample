using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WordSampleWebRole.Models;
using System.Reflection;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Net;
using System.Text;

namespace WordSampleWebRole.Controllers
{
    public class DocumentController : Controller
    {
        //
        // GET: /Document/

        #region Simple Display

        public ActionResult Display()
        {
            MemoryStream memStream = new MemoryStream();
            CreateDocument(memStream);
            memStream.Position = 0;
            return new FileStreamResult(memStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        }

        #endregion

        #region Office 365 SharePoint Online

        public ActionResult SaveToOffice365()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SaveToOffice365(O365SaveInfoModel model)
        {
            SPOUtil.ProcessSPOSecurity(Session, model.SiteUrl, model.UserId, model.Password);

            using (MemoryStream memStream = new MemoryStream())
            {
                // HtpWebRequest の stream の file mode では OpenXML SDK が使えないため、
                // いったん MemoryStream に書きます。。。
                CreateDocument(memStream);
                memStream.Position = 0;

                string folderName = SPOUtil.GetSPListRootFolder(Session, model.SiteUrl, model.DocLibName);
                string fileName = "PO_" + TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Tokyo Standard Time").ToString("yyyyMMddHHmmss") + ".docx";
                string targetUrl = (new Uri(model.SiteUrl)).GetLeftPart(UriPartial.Authority) + folderName + "/" + fileName;

                SPOUtil.UploadSPFile(Session, memStream, targetUrl);

                memStream.Close();
            }

            SPOUtil.ClearSPOCookieContainer(Session); // ログイン情報を破棄 !

            return RedirectToAction("Confirm", "Deliver");
        }

        [AjaxErrorHandler]
        public ActionResult SPOAvailableLists(string url, string userid, string password)
        {
            SPOUtil.ProcessSPOSecurity(Session, url, userid, password);
            List<string> resultList = SPOUtil.GetSPListCollection(Session, url);
            return Json(resultList, JsonRequestBehavior.AllowGet);
        }

        [AjaxErrorHandler]
        public ActionResult ClearLogin()
        {
            SPOUtil.ClearSPOCookieContainer(Session);
            return Json(new { status = "success" }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region SkyDrive

        // Windows Live callback action using authentication code
        public ActionResult SaveToSkydrive(string code)
        {
            // if you retrieve refresh token too, please add wl.offline_access to scope !
            if (string.IsNullOrEmpty(code))
                return Redirect(@"https://login.live.com/oauth20_authorize.srf?client_id=" + HttpUtility.UrlEncode(SkyDriveUtil.CLIENT_ID) + "&scope=wl.signin%20wl.skydrive_update&response_type=code&redirect_uri=" + HttpUtility.UrlEncode(SkyDriveUtil.REDIRECT_URL));

            OAuthToken token;
            SkyDriveUtil.RequestAccessTokenByVerifier(code, out token);

            ViewBag.Folders = SkyDriveUtil.GetFolders(token);

            return View(new SkydriveSaveInfoModel() { FolderLocation = null, AccessToken = token.AccessToken });
        }

        [HttpPost]
        public ActionResult SaveToSkydrive(SkydriveSaveInfoModel model)
        {
            string fileName = "PO_" + TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Tokyo Standard Time").ToString("yyyyMMddHHmmss") + ".docx";

            using (MemoryStream memStream = new MemoryStream())
            {
                // HtpWebRequest の stream の file mode では OpenXML SDK が使えないため、
                // いったん MemoryStream に書きます。。。
                CreateDocument(memStream);
                memStream.Position = 0;

                SkyDriveUtil.UploadFile(fileName, memStream, model.FolderLocation, model.AccessToken);

                memStream.Close();
            }

            return RedirectToAction("Confirm", "Deliver");
        }

        #endregion

        #region Windows Azure Blob Storage

        public ActionResult SaveToBlobstorage()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SaveToBlobstorage(BlobSaveInfoModel model)
        {
            if (ModelState.IsValid)
            {
                string fileName = "PO_" + TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Tokyo Standard Time").ToString("yyyyMMddHHmmss") + ".docx";
                using (MemoryStream memStream = new MemoryStream())
                {
                    CreateDocument(memStream);
                    memStream.Position = 0;
                    BlobUtil.SaveBlock(model.Account, model.AccessKey, model.Container, fileName, memStream);
                    memStream.Close();
                }

                return RedirectToAction("Confirm", "Deliver");
            }
            else
                return View(model);
        }

        [AjaxErrorHandler]
        public ActionResult BlobContainers(string account, string accesskey)
        {
            List<string> resultList = BlobUtil.GetAccountContainers(account, accesskey);
            return Json(resultList, JsonRequestBehavior.AllowGet);
        }

        #endregion

        //
        // create Word document in memory ! (using OpenXML SDK)
        //
        private void CreateDocument(Stream writeStream)
        {
            // OrderModel オブジェクトの作成
            OrderModel order = new OrderModel()
            {
                Deliver = Helper.GetCurrentDeliverInfo(Session),
                Products = Helper.GetCurrentProductList(Session)
            };
            decimal CostAmount = (from p in Helper.GetCurrentProductList(Session)
                                  select p.ProductUnitPrice * p.ProductCount).Sum();

            // Word ファイルの作成
            System.Globalization.CultureInfo cinf = new System.Globalization.CultureInfo("ja-JP");
            System.Globalization.NumberFormatInfo nfi = (System.Globalization.NumberFormatInfo)cinf.NumberFormat.Clone();

            Assembly asm = Assembly.GetExecutingAssembly();
            string resourceName = String.Empty;
            foreach (string s in asm.GetManifestResourceNames())
            {
                if (s.EndsWith("template.docx"))
                {
                    resourceName = s;
                    break;
                }
            }
            Stream tempStream = asm.GetManifestResourceStream(resourceName);
            byte[] tempData = new byte[tempStream.Length];
            tempStream.Read(tempData, 0, tempData.Length);
            writeStream.Write(tempData, 0, tempData.Length);
            using (WordprocessingDocument docx = WordprocessingDocument.Open(writeStream, true))
            {
                MainDocumentPart mainPart = docx.MainDocumentPart;

                // Name
                SdtElement nameElem = mainPart.Document.Body.Descendants<SdtElement>().Where(r => r.SdtProperties.GetFirstChild<SdtAlias>().Val == "Name").Single();
                SdtContentRun nameContent = nameElem.Descendants<SdtContentRun>().FirstOrDefault();
                Text nameText = nameContent.Descendants<Text>().FirstOrDefault();
                // Caution : if the error occures here, please use "Start Without Debugging" mode. (Because ASP.NET debugger clears cookie.)
                nameText.Text = order.Deliver.Name;

                // Zip Code
                SdtElement zipElem = mainPart.Document.Body.Descendants<SdtElement>().Where(r => r.SdtProperties.GetFirstChild<SdtAlias>().Val == "ZipCode").Single();
                SdtContentCell zipContent = zipElem.Descendants<SdtContentCell>().FirstOrDefault();
                Text zipText = zipContent.Descendants<Text>().FirstOrDefault();
                zipText.Text = order.Deliver.ZipCode;

                // Address
                SdtElement addressElem = mainPart.Document.Body.Descendants<SdtElement>().Where(r => r.SdtProperties.GetFirstChild<SdtAlias>().Val == "AddressLine").Single();
                SdtContentCell addressContent = addressElem.Descendants<SdtContentCell>().FirstOrDefault();
                Text addressText = addressContent.Descendants<Text>().FirstOrDefault();
                addressText.Text = order.Deliver.Address;

                // Telephone
                SdtElement telElem = mainPart.Document.Body.Descendants<SdtElement>().Where(r => r.SdtProperties.GetFirstChild<SdtAlias>().Val == "Telephone").Single();
                SdtContentCell telContent = telElem.Descendants<SdtContentCell>().FirstOrDefault();
                Text telText = telContent.Descendants<Text>().FirstOrDefault();
                telText.Text = order.Deliver.Telephone;

                // Total Amount
                SdtElement totalElem = mainPart.Document.Body.Descendants<SdtElement>().Where(r => r.SdtProperties.GetFirstChild<SdtAlias>().Val == "TotalAmount").Single();
                SdtContentRun totalContent = totalElem.Descendants<SdtContentRun>().FirstOrDefault();
                Text totalText = totalContent.Descendants<Text>().FirstOrDefault();
                totalText.Text = String.Format(nfi, "{0:c}", CostAmount);

                // Product List
                Table productTbl = mainPart.Document.Body.Descendants<Table>().ElementAt<Table>(1);
                for (int i = 0; i < order.Products.Count<ProductItemModel>(); i++)
                {
                    if (i != 0)
                    {
                        TableRow tr = new TableRow();
                        TableCell tc1 = new TableCell();
                        TableCell tc2 = new TableCell();
                        TableCell tc3 = new TableCell();
                        TableCell tc4 = new TableCell();
                        tr.Append(tc1);
                        tr.Append(tc2);
                        tr.Append(tc3);
                        tr.Append(tc4);
                        productTbl.Append(tr);
                    }

                    TableRow row = productTbl.Descendants<TableRow>().ElementAt<TableRow>(i + 1);

                    row.Descendants<TableCell>().ElementAt<TableCell>(0).RemoveAllChildren();
                    row.Descendants<TableCell>().ElementAt<TableCell>(0).Append(new Paragraph(new Run(new Text(order.Products[i].ProductName))));

                    row.Descendants<TableCell>().ElementAt<TableCell>(1).RemoveAllChildren();
                    row.Descendants<TableCell>().ElementAt<TableCell>(1).Append(new Paragraph(new Run(new Text(order.Products[i].ProductCount.ToString()))));

                    row.Descendants<TableCell>().ElementAt<TableCell>(2).RemoveAllChildren();
                    row.Descendants<TableCell>().ElementAt<TableCell>(2).Append(new Paragraph(new Run(new Text(String.Format(nfi, "{0:c}", order.Products[i].ProductUnitPrice)))));

                    row.Descendants<TableCell>().ElementAt<TableCell>(3).RemoveAllChildren();
                    row.Descendants<TableCell>().ElementAt<TableCell>(3).Append(new Paragraph(new Run(new Text(String.Format(nfi, "{0:c}", order.Products[i].ProductCount * order.Products[i].ProductUnitPrice)))));
                }
            }
        }

    }

    public class AjaxErrorHandlerAttribute : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            string errorMsg = filterContext.Exception.Message;
            if (filterContext.Exception.InnerException != null)
                errorMsg += " (InnerException : " + filterContext.Exception.InnerException.Message + ")";

            filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
            var responce = filterContext.RequestContext.HttpContext.Response;
            responce.Write(errorMsg);
            responce.ContentType = "text/plain";

            filterContext.ExceptionHandled = true;
        }
    }
}
