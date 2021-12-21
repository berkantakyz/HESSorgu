using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using HESSorgu.Models;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Data.OleDb;
using System.Data;
using System.Web.UI.WebControls;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;
using ClosedXML.Excel;
using Newtonsoft.Json.Linq;
using System.Json;

namespace HESSorgu.Controllers
{
    public class HomeController : Controller
    {
        checkEmployee successResponse = new checkEmployee();
        fieldResponse fieldResponse = new fieldResponse();
        public ActionResult Anasayfa()
        {
            return View();
        }
        public ActionResult QRScan()
        {
            return View("QRScan");
        }

        public ActionResult hesKontrol(FormCollection frm)
        {
            TempData["errorMessage"] = "";
            TempData["successMessage"] = "";

            var hesCode = frm["hesCode"];

            if (hesCode != "")
            {
                hesCodeKontrol(hesCode);

                if (fieldResponse.errorKey == null)
                {
                    return RedirectToAction("SorguResult", successResponse);
                }

                else
                {
                    if (fieldResponse.errorKey == "hescodehasbeenexpired")
                    {
                        TempData["errorMessage"] = "Girilen HES Kodunun Kullanım Süresi Dolmuştur!";

                        return RedirectToAction("Anasayfa");
                    }

                    else if (fieldResponse.errorKey == "hescodenotfound")
                    {
                        TempData["errorMessage"] = "Girilen HES Kodu Geçerli Değildir!";

                        return RedirectToAction("Anasayfa");
                    }
                }
            }

            return RedirectToAction("Anasayfa");
        }

        public ActionResult hesKontrolQR(string hesCode)
        {
            JsonResult jsonResult = null;

            TempData["errorMessage"] = "";
            TempData["successMessage"] = "";

            hesCodeKontrol(hesCode);

            if (fieldResponse.errorKey == null)
            {
                return Json(new { msg = "success", model = successResponse }, JsonRequestBehavior.AllowGet);
            }

            else
            {
                return Json(new { msg = fieldResponse.errorKey.ToString(), model = successResponse }, JsonRequestBehavior.AllowGet);
            }
        }

        public String hesCodeKontrol(string hesCode)
        {
            if (hesCode != "")
            {
                //var client = new RestClient("your service url");
                //TEST ORTAMI
                var client = new RestClient("your service url");
                //CANLI ORTAM
                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                //string authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes("user_code" + ":" + "password"));
                //TEST LOGIN BİLGİLERİ
                string authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes("user_code" + ":" + "password"));
                request.AddHeader("authorization", "Basic " + authentication);
                request.RequestFormat = DataFormat.Json;
                string requestParams = "{\"hes_code\": \"" + hesCode + "\"} \r\n \r\n ";
                request.AddParameter("application/json", requestParams, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                try
                {
                    if (response.IsSuccessful)
                    {
                        string rs = response.Content.ToString();
                        successResponse = JsonConvert.DeserializeObject<Models.checkEmployee>(rs);

                        return successResponse.current_health_status;
                    }

                    else
                    {
                        string rs = response.Content.ToString();
                        fieldResponse = JsonConvert.DeserializeObject<Models.fieldResponse>(rs);

                        return fieldResponse.errorKey;
                    }
                }
                catch
                {
                    return "";
                }
            }

            return "İçeri Atılan Boş Alan";
        }

        public List<csvModel> hesCodeKontrolToplu(List<csvModel> csvList)
        {
            List<csvModel> resultList = new List<csvModel>();

            if (csvList.Count != 0)
            {
                //var client = new RestClient("your service url");
                //TEST ORTAMI
                var client = new RestClient("your service url");
                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                //string authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes("user_code" + ":" + "password"));
                //TEST LOGIN BİLGİLERİ
                string authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes("user_code" + ":" + "password"));
                request.AddHeader("authorization", "Basic " + authentication);
                request.RequestFormat = DataFormat.Json;

                string requestParams = "";

                foreach (var item in csvList)
                {
                    requestParams = requestParams + "{\"hes_code\": \"" + item.HES_KOD.Replace(" ","") + "\"}, \r\n \r\n ";
                }

                requestParams = requestParams.Trim();

                requestParams = "[" + requestParams.Substring(0, requestParams.Length - 1) + "]";

                request.AddParameter("application/json", requestParams, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                try
                {
                    if (response.IsSuccessful)
                    {
                        string rs = response.Content.ToString();

                        JObject obj = JObject.Parse(rs);

                        foreach (var item in csvList)
                        {
                            var success = obj.SelectTokens("$.." + item.HES_KOD + ".current_health_status").ToList();

                            var unSuccess = obj.SelectTokens("$..unsuccess_map." + item.HES_KOD).ToList();

                            try
                            {
                                if (success.Count == 1)
                                {
                                    string DURUM = success[0].ToString() == "RISKY" ? "RİSKLİ" : "RİSKSİZ";

                                    resultList.Add(new csvModel
                                    {
                                        DURUM = DURUM,
                                        HES_KOD = item.HES_KOD,
                                        PERSONEL_ADSOYAD = item.PERSONEL_ADSOYAD,
                                        PERSONEL_KOD = item.PERSONEL_KOD
                                    });
                                }

                           
                                else
                                {
                                    string DURUM = unSuccess[0].ToString() == "hescodehasbeenexpired" ? "KULLANIM SÜRESİ DOLMUŞTUR" : "GEÇERSİZ & TANIMSIZ HES KODU";

                                    resultList.Add(new csvModel
                                    {
                                        DURUM = DURUM,
                                        HES_KOD = item.HES_KOD,
                                        PERSONEL_ADSOYAD = item.PERSONEL_ADSOYAD,
                                        PERSONEL_KOD = item.PERSONEL_KOD
                                    });
                                }

                            }
                            catch (Exception ex)
                            {

                                throw;
                            }
                           

                        }

                        return resultList;
                    }

                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            return null;
        }

        public ActionResult SorguResult(checkEmployee datax)
        {
            if (datax != null)
            {
                if (datax.current_health_status == "RISKY")
                {
                    datax.current_health_status = "RİSKLİ";

                    TempData["errorMessage"] = "RİSK ! HES KODU RİSKLİ !";
                }

                else if (datax.current_health_status == "RISKLESS")
                {
                    datax.current_health_status = "GÜVENLİ";

                    TempData["successMessage"] = "HES KODU RİSKSİZ !";
                }
            }

            return View("SorguResult", datax);
        }

        public string excelOku(string dosya_adi, string dosya_path)
        {
            List<csvModel> _file = new List<csvModel>();
            List<csvModel> _fileResponse = new List<csvModel>();

            DataTable dt = new DataTable();

            try
            {
                dt = ProcessCSV(dosya_path);

                foreach (DataRow row in dt.Rows)
                {
                    csvModel csvList = new csvModel();

                    if (row[2].ToString() != "")
                    {
                        csvList.PERSONEL_KOD = row[0].ToString();
                        csvList.PERSONEL_ADSOYAD = row[1].ToString();
                        csvList.HES_KOD = row[2].ToString().Replace(" ", "").Replace("  ","").Trim();

                        _file.Add(csvList);
                    }
                }

                _fileResponse = hesCodeKontrolToplu(_file);

                string downloadPath = exportExcel(_fileResponse, dosya_adi, dosya_path);

                Helper.mailGonder mail = new Helper.mailGonder();

                List<Mails> mailList = getMailList();

                var result = mail.newMail("Sorguladığınız Hes Kodlarının Sonuçları hk.", "Merhaba, sorgulama sonuçları ektedir.", mailList, downloadPath);

                return downloadPath;
            }

            catch (Exception ex)
            {
                return "";
                dt.Dispose();
            }
        }

        public static DataTable ProcessCSV(string fileName)
        {
            try
            {
                string Feedback = string.Empty;
                string line = string.Empty;
                string[] strArray;
                DataTable dt = new DataTable();
                DataRow row;
                Regex r = new Regex(";(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("iso-8859-9"));
                line = sr.ReadLine();
                strArray = r.Split(line);
                Array.ForEach(strArray, s => dt.Columns.Add(new DataColumn()));

                while ((line = sr.ReadLine()) != null)
                {
                    row = dt.NewRow();
                    row.ItemArray = r.Split(line);
                    dt.Rows.Add(row);
                }

                sr.Dispose();

                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        [HttpPost]
        public ActionResult UploadFiles()
        {
            if (Request.Files.Count > 0)
            {
                try
                {
                    #region Upload && Download Temizle

                    var filesDownloads = Directory.GetFiles(Server.MapPath("~/Downloads/"), "*.*", SearchOption.AllDirectories);
                    foreach (var item in filesDownloads)
                        try
                        {
                            FileInfo fileInfo = new FileInfo(item);
                            fileInfo.Delete();
                        }
                        catch (Exception ex)
                        {
                        }

                    var filesUploads = Directory.GetFiles(Server.MapPath("~/Uploads/"), "*.*", SearchOption.AllDirectories);
                    foreach (var item in filesUploads)
                        try
                        {
                            FileInfo fileInfo = new FileInfo(item);
                            fileInfo.Delete();
                        }
                        catch (Exception ex)
                        {
                        }
                    #endregion

                    HttpFileCollectionBase files = Request.Files;


                    HttpPostedFileBase file = files[0];

                    string fname;

                    // Checking for Internet Explorer  
                    if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                    {
                        string[] testfiles = file.FileName.Split(new char[] { '\\' });
                        fname = testfiles[testfiles.Length - 1];
                    }
                    else
                    {
                        fname = file.FileName;
                    }

                    string strpath = System.IO.Path.GetExtension(file.FileName);

                    if (strpath == ".csv")
                    {
                        // Get the complete folder path and store the file inside it.  
                        fname = Path.Combine(Server.MapPath("~/Uploads/"), fname);
                        file.SaveAs(fname);

                        excelOku(file.FileName, fname);

                        return Json("");
                    }

                    else
                    {
                        return Json("Lütfen CSV Uzantılı Excel Dosyası Seçin!");
                    }
                }
                catch (Exception ex)
                {
                    return Json("Hata. Hata Detayı: " + ex.Message);
                }
            }
            else
            {
                return Json("Lütfen Dosya Seçin!");
            }

        }

        public string exportExcel(List<csvModel> exportList, string dosya_adi, string dosya_path)
        {
            string dateFileName = "";

            try
            {
                using (XLWorkbook workbook = new XLWorkbook())
                {
                    workbook.Worksheets.Add("Sayfa1", 1);

                    IXLWorksheet Worksheet = workbook.Worksheet("Sayfa1");

                    //try
                    //{
                    //    int NumberOfLastRow = Worksheet.LastRowUsed().RowNumber();
                    //    IXLCell CellForNewData = Worksheet.Cell(NumberOfLastRow + 1, 1);
                    //    CellForNewData.InsertTable(exportList);
                    //}
                    //catch//if this excel file is new created, select the first cell
                    //{
                    Worksheet.Cell(1, 1).InsertTable(exportList);
                    //}
                    
                    dateFileName = Server.MapPath("~/Downloads/") + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")+ dosya_adi.Replace("csv", "xlsx");

                    workbook.SaveAs(dateFileName);

                    workbook.Dispose();
                }

                return dateFileName;
            }
            catch (Exception ex)
            {
                return "";
                throw ex;
            }

        }

        public List<Mails> getMailList()
        {
            try
            {
                using (HESEntities db = new HESEntities())
                {
                    var data = db.Mails.ToList();

                    return data;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}