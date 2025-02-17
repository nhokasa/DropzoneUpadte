using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using API._Services.Interfaces;
using API.Dtos;
using API.Helpers.Params;
using API.Hubs;
using Aspose.Cells;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
namespace API.Controllers
{
    public class ProductCategoryController : ApiController
    {
        private readonly IProductCategoryService _productCategoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<HubClient, IHubClient> _hubContext;

        public ProductCategoryController(
            IProductCategoryService productCategoryService,
            IWebHostEnvironment webHostEnvironment,
            IHubContext<HubClient, IHubClient> hubContext)
        {
            _productCategoryService = productCategoryService;
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductCategory_Dto model)
        {
            model.Update_By = User.FindFirst(ClaimTypes.Name).Value;
            model.Update_Time = DateTime.Now;
            var result = await _productCategoryService.Create(model);
            if (result.Success)
                await _hubContext.Clients.All.LoadDataProductCate();
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductCategoryByID(string productCateID)
        {
            return Ok(await _productCategoryService.GetProductCategoryByID(productCateID));
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAsync()
        {
            return Ok(await _productCategoryService.GetAllAsync());
        }

        [HttpGet("name")]
        public async Task<IActionResult> GetIDAndName()
        {
            return Ok(await _productCategoryService.GetIDAndName());
        }

        [HttpGet("pagination")]
        public async Task<IActionResult> GetProductCategoryWithPaginations([FromQuery] PaginationParams param, string text)
        {
            return Ok(await _productCategoryService.GetProductCategoryWithPaginations(param, text, false));
        }

        [HttpPut]
        public async Task<IActionResult> Update(ProductCategory_Dto model)
        {
            model.Update_By = User.FindFirst(ClaimTypes.Name).Value;
            model.Update_Time = DateTime.Now;
            var result = await _productCategoryService.Update(model);
            if (result.Success)
                await _hubContext.Clients.All.LoadDataProductCate();
            return Ok(result);
        }

        [HttpPut("changeStatus")]
        public async Task<IActionResult> ChangeStatus(ProductCategory_Dto model)
        {
            model.Update_By = User.FindFirst(ClaimTypes.Name).Value;
            model.Update_Time = DateTime.Now;
            model.Status = !model.Status;
            var result = await _productCategoryService.Update(model);
            if (result.Success)
                await _hubContext.Clients.All.LoadDataProductCate();
            return Ok(result);
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Remove(List<ProductCategory_Dto> model)
        {
            var result = await _productCategoryService.Remove(model);
            if (result.Success)
                await _hubContext.Clients.All.LoadDataProductCate();
            return Ok(result);
        }

        // Import Data Excel
        [HttpPost("import")]
        public async Task<ActionResult> ImportExcel(IFormFile files)
        {
            if (files != null)
            {
                string fileNameExtension = (files.FileName.Split("."))[(files.FileName.Split(".")).Length - 1];
                string fileName = "Upload_Excel_ProductCate_" + DateTime.Now.ToString().Replace(":", "").Replace("/", "").Replace(" ", "") + "." + fileNameExtension;

                string folder = _webHostEnvironment.WebRootPath + $@"\uploaded\excels\ProcutCategory";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                string filePath = Path.Combine(folder, fileName);
                using (FileStream fs = System.IO.File.Create(filePath))
                {
                    files.CopyTo(fs);
                    fs.Flush();
                }
                var result = await _productCategoryService.ImportExcel(filePath, User.FindFirst(ClaimTypes.Name).Value);
                if (result.Success)
                    await _hubContext.Clients.All.LoadDataProductCate();
                return Ok(result);
            }
            throw new Exception("Import Excel Product Category failed on save");
        }

        // Export Excel Epplus
        [HttpGet("exportExcelEpplus")]
        public async Task<ActionResult> ExportExcelEpplus([FromQuery] PaginationParams param, string text)
        {
            var data = await _productCategoryService.GetProductCategoryWithPaginations(param, text, false);

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                // Define excel file
                var workSheet = package.Workbook.Worksheets.Add("Sheet1");

                // Add header
                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Row(1).Height = 40;
                workSheet.Column(7).Style.Numberformat.Format = "yyyy/MM/dd";
                workSheet.Cells[1, 1, 1, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workSheet.Cells[1, 1, 1, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#20a8d8"));
                workSheet.Cells[1, 1, 1, 7].Style.Font.Color.SetColor(Color.White);
                workSheet.Cells[1, 1, 1, 7].Style.Font.Size = 14;
                workSheet.Cells[1, 1].Value = "#";
                workSheet.Cells[1, 2].Value = "Product Cate ID";
                workSheet.Cells[1, 3].Value = "Product Cate Name";
                workSheet.Cells[1, 4].Value = "Status";
                workSheet.Cells[1, 5].Value = "Position";
                workSheet.Cells[1, 6].Value = "Update By";
                workSheet.Cells[1, 7].Value = "Update Time";

                // Add body
                int index = 1;
                foreach (var item in data.Result)
                {
                    workSheet.Cells[index + 1, 1].Value = (index);
                    workSheet.Cells[index + 1, 2].Value = item.Product_Cate_ID;
                    workSheet.Cells[index + 1, 3].Value = item.Product_Cate_Name;
                    workSheet.Cells[index + 1, 5].Value = item.Position;
                    workSheet.Cells[index + 1, 6].Value = item.Update_By;
                    workSheet.Cells[index + 1, 7].Value = item.Update_Time;
                    workSheet.Cells[index + 1, 7].Style.Numberformat.Format = "yyyy/MM/dd hh:mm:ss";
                    workSheet.Row(index + 1).Height = 30;

                    string file = "";
                    if (item.Status == true)
                        file = _webHostEnvironment.WebRootPath + "\\icons\\ok-512.png";
                    else
                        file = _webHostEnvironment.WebRootPath + "\\icons\\circle-outline-512.png";
                    Image img = Image.FromFile(file);
                    ExcelPicture pic = workSheet.Drawings.AddPicture(index.ToString(), img);
                    // position image custom (row, top, col, left) : px
                    pic.SetPosition(index, 10, 3, 45);
                    pic.SetSize(20, 20);

                    index++;
                }
                workSheet.Cells[1, 1, index, 7].Style.WrapText = true;
                workSheet.Cells[1, 1, index, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Cells[1, 1, index, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                workSheet.Cells[1, 1, index, 7].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                workSheet.Cells[1, 1, index, 7].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                workSheet.Cells[1, 1, index, 7].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                workSheet.Cells[1, 1, index, 7].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                workSheet.Column(1).AutoFit(10);
                workSheet.Column(2).AutoFit(20);
                workSheet.Column(3).AutoFit(40);
                workSheet.Column(4).AutoFit(15);
                workSheet.Column(5).AutoFit(15);
                workSheet.Column(6).AutoFit(20);
                workSheet.Column(7).AutoFit(20);
                package.Save();
            }

            // Export
            stream.Position = 0;
            return File(stream, "application/xlsx", "Product_Category_" + Guid.NewGuid().ToString() + ".xlsx");
        }

        //Export Excel Aspose.Cell
        [HttpGet("exportExcelAspose")]
        public async Task<ActionResult> ExportExcelAspose([FromQuery] PaginationParams param, string text, int checkExport)
        {
            var data = await _productCategoryService.GetProductCategoryWithPaginations(param, text, false);
            var path = Path.Combine(_webHostEnvironment.ContentRootPath, "Resources\\Template\\ProductCategory\\ProductCategoryListTemplate.xlsx");
            WorkbookDesigner designer = new WorkbookDesigner();
            designer.Workbook = new Workbook(path);

            Cell cell = designer.Workbook.Worksheets[0].Cells["A1"];

            Worksheet ws = designer.Workbook.Worksheets[0];

            designer.SetDataSource("result", data.Result);
            designer.Process();

            int index = 2;
            for (int i = 0; i < data.Result.Count; i++)
            {
                ws.Cells["A" + index].PutValue(i + 1);
                index++;
            }

            var index2 = 1;
            foreach (var item in data.Result)
            {
                string file = "";
                if (item.Status == true)
                    file = _webHostEnvironment.WebRootPath + "\\icons\\ok-512.png";
                else
                    file = _webHostEnvironment.WebRootPath + "\\icons\\circle-outline-512.png";
                Aspose.Cells.Drawing.Picture pic = ws.Pictures[ws.Pictures.Add(index2, 3, file)];
                pic.Height = 20;
                pic.Width = 20;
                pic.Top = 5;
                pic.Left = 40;

                index2++;
            }

            MemoryStream stream = new MemoryStream();

            string fileKind = "";
            string fileExtension = "";

            if (checkExport == 1)
            {
                designer.Workbook.Save(stream, SaveFormat.Xlsx);
                fileKind = "application/xlsx";
                fileExtension = ".xlsx";
            }
            if (checkExport == 2)
            {
                // custom size ( width: in, height: in )
                //ws.PageSetup.CustomPaperSize(12.5, 8);
                ws.PageSetup.FitToPagesTall = 0;
                ws.PageSetup.SetHeader(0, "&D &T");
                ws.PageSetup.SetHeader(1, "&B Product Category");
                ws.PageSetup.SetFooter(0, "&B SYSTEM BY MINH HIEU");
                ws.PageSetup.SetFooter(2, "&P/&N");
                ws.PageSetup.PrintQuality = 1200;
                designer.Workbook.Save(stream, SaveFormat.Pdf);
                fileKind = "application/pdf";
                fileExtension = ".pdf";
            }

            byte[] result = stream.ToArray();

            return File(result, fileKind, "Product_Category_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + fileExtension);
        }
    }
}