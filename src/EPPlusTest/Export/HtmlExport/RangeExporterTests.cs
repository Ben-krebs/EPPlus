﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.Export.HtmlExport;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using OfficeOpenXml.Style;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;

namespace EPPlusTest.Export.HtmlExport
{
    [TestClass]
    public class RangeExporterTests : TestBase
    {
        [TestMethod]
        public void ShouldExportHtmlWithHeadersNoAccessibilityAttributes()
        {
            using(var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Test");
                sheet.Cells["A1"].Value = "Name";
                sheet.Cells["B1"].Value = "Age";
                sheet.Cells["A2"].Value = "John Doe";
                sheet.Cells["B2"].Value = 23;
                var range = sheet.Cells["A1:B2"];
                using(var ms = new MemoryStream())
                {
                    var exporter = range.CreateHtmlExporter();
                    exporter.Settings.Accessibility.TableSettings.AddAccessibilityAttributes=false;
                    exporter.RenderHtml(ms);                    
                    var sr = new StreamReader(ms);
                    ms.Position = 0;
                    var result = sr.ReadToEnd();
                    Assert.AreEqual(
                        "<table class=\"epplus-table\"><thead><tr><th data-datatype=\"string\" class=\"epp-al\">Name</th><th data-datatype=\"number\" class=\"epp-al\">Age</th></tr></thead><tbody><tr><td>John Doe</td><td data-value=\"23\" class=\"epp-ar\">23</td></tr></tbody></table>",
                        result);
                }
            }
        }
        [TestMethod]
        public void ShouldSetWidthAndDefaultRowAndWidthClasses()
        {
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Test");
                sheet.Cells["A1"].Value = "Name";
                sheet.Cells["B1"].Value = "Age";
                sheet.Cells["A2"].Value = "John Doe";
                sheet.Cells["B2"].Value = 23;
                sheet.Cells["A1:A2"].AutoFitColumns();
                var range = sheet.Cells["A1:C3"];

                var exporter = range.CreateHtmlExporter();
                exporter.Settings.Accessibility.TableSettings.AddAccessibilityAttributes = false;
                exporter.Settings.SetColumnWidth = true;
                exporter.Settings.SetRowHeight = true;
                var result = exporter.GetSinglePage();
                File.WriteAllText("c:\\temp\\" + sheet.Name + ".html", result);
                Assert.AreEqual(
                    "<html><head><style type=\"text/css\">table.epplus-table{font-family:Calibri;font-size:11pt;border-spacing:0;border-collapse:collapse;word-wrap:break-word;white-space:nowrap;}.epp-hidden {display:none;}.epp-al {text-align:left;}.epp-ar {text-align:right;}.epp-dcw {width:64px;}.epp-drh {height:20px;}</style></head><body><table class=\"epplus-table\"><colgroup><col class=\"epp-dcw\" span=\"1\"/><col class=\"epp-dcw\" span=\"1\"/><col class=\"epp-dcw\" span=\"1\"/></colgroup><thead><tr class=\"epp-drh\"><th data-datatype=\"string\" class=\"epp-al\">Name</th><th data-datatype=\"number\" class=\"epp-al\">Age</th><th data-datatype=\"string\" class=\"epp-al\"></th></tr></thead><tbody><tr class=\"epp-drh\"><td>John Doe</td><td data-value=\"23\" class=\"epp-ar\">23</td><td></td></tr><tr class=\"epp-drh\"><td></td><td></td><td></td></tr></tbody></table></body></html>",
                    result);
            }
        }

        [TestMethod]
        public async Task ShouldExportHtmlWithHeadersWithStyles()
        {
            using (var package = OpenPackage("HtmlPatternStylesCells.xlsx", true))
            {
                var sheet = package.Workbook.Worksheets.Add("PatternStyle");
                sheet.Cells["A1"].Value = "Name";
                sheet.Cells["B1"].Value = "Age";
                sheet.Cells["A2"].Value = "John Doe";
                sheet.Cells["B2"].Value = 23;
                var range = sheet.Cells["A1:B2"];
                sheet.Cells["A1:B1"].Style.Font.Bold = true;
                sheet.Cells["A1:B1"].Style.Font.Color.SetColor(Color.Blue);
                sheet.Cells["A1:B1"].Style.Border.Bottom.Style=ExcelBorderStyle.Thin;
                sheet.Cells["A1:B1"].Style.Border.Bottom.Color.SetColor(Color.Red);
                sheet.Cells["A1:B1"].Style.Fill.PatternType = ExcelFillStyle.LightGray;
                sheet.Cells["A1:B1"].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                sheet.Cells["A1:B1"].Style.Fill.PatternColor.SetColor(Color.LightCyan);
                sheet.Cells["A2:B2"].Style.Font.Italic=true;
                sheet.Cells["B1:B2"].Style.Font.Name = "Consolas";

                var exporter = range.CreateHtmlExporter();
                exporter.Settings.Accessibility.TableSettings.AddAccessibilityAttributes = false;
                var result = exporter.GetSinglePage();

                Assert.AreEqual(
                    "<html><head><style type=\"text/css\">table.epplus-table{font-family:Calibri;font-size:11pt;border-spacing:0;border-collapse:collapse;word-wrap:break-word;white-space:nowrap;}.epp-hidden {display:none;}.epp-al {text-align:left;}.epp-ar {text-align:right;}.epp-dcw {width:64px;}.epp-drh {height:20px;}.epp-s1{background-repeat:repeat;background:url(data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPSc0JyBoZWlnaHQ9JzInPjxyZWN0IHdpZHRoPSc0JyBoZWlnaHQ9JzInIGZpbGw9JyNlMGZmZmYnLz48cmVjdCB4PScyJyB5PScwJyB3aWR0aD0nMScgaGVpZ2h0PScxJyBmaWxsPScjZjA4MDgwJy8+PHJlY3QgeD0nMCcgeT0nMScgd2lkdGg9JzEnIGhlaWdodD0nMScgZmlsbD0nI2YwODA4MCcvPjwvc3ZnPg==);color:#0000ff;font-weight:bolder;border-bottom:thin solid #ff0000;white-space: nowrap;}.epp-s2{background-repeat:repeat;background:url(data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPSc0JyBoZWlnaHQ9JzInPjxyZWN0IHdpZHRoPSc0JyBoZWlnaHQ9JzInIGZpbGw9JyNlMGZmZmYnLz48cmVjdCB4PScyJyB5PScwJyB3aWR0aD0nMScgaGVpZ2h0PScxJyBmaWxsPScjZjA4MDgwJy8+PHJlY3QgeD0nMCcgeT0nMScgd2lkdGg9JzEnIGhlaWdodD0nMScgZmlsbD0nI2YwODA4MCcvPjwvc3ZnPg==);font-family:Consolas;color:#0000ff;font-weight:bolder;border-bottom:thin solid #ff0000;white-space: nowrap;}.epp-s3{font-style:italic;white-space: nowrap;}.epp-s4{font-family:Consolas;font-style:italic;white-space: nowrap;}</style></head><body><table class=\"epplus-table\"><thead><tr><th data-datatype=\"string\" class=\"epp-al epp-s1\">Name</th><th data-datatype=\"number\" class=\"epp-al epp-s2\">Age</th></tr></thead><tbody><tr><td class=\"epp-s3\">John Doe</td><td data-value=\"23\" class=\"epp-ar epp-s4\">23</td></tr></tbody></table></body></html>",
                    result);

                var resultAsync = await exporter.GetSinglePageAsync();
                Assert.AreEqual(result, resultAsync);
                SaveAndCleanup(package);
                File.WriteAllText("c:\\temp\\" + sheet.Name + ".html", result);

            }
        }
        [TestMethod]
        public async Task ShouldExportHtmlWithMergedCells()
        {
            using (var package = OpenPackage("HtmlMergeCells.xlsx", true))
            {
                var sheet = package.Workbook.Worksheets.Add("Horizontal");
                sheet.Cells["A1"].Value = "Merge Horizontal";
                sheet.Cells["A1:C1"].Merge = true;
                sheet.Cells["C2:C4"].Merge = true;
                sheet.Cells["C2"].Value = "Merge Vertical";
                sheet.Cells["C2"].Style.TextRotation = 255;

                sheet.Cells["A2"].Value = "Name";
                sheet.Cells["B2"].Value = "Age";
                sheet.Cells["A3"].Value = "John Doe";
                sheet.Cells["B3"].Value = 23;
                sheet.Cells["A3"].Value = "Jane Doe";
                sheet.Cells["B3"].Value = 25;
                sheet.Cells["A4"].Value = "James Doe";
                sheet.Cells["B4"].Value = 2;

                sheet.Cells["A1:B1"].Style.Font.Bold = true;
                sheet.Cells["A1:B1"].Style.Font.Color.SetColor(Color.Blue);
                sheet.Cells["A1:B1"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                sheet.Cells["A1:B1"].Style.Border.Bottom.Color.SetColor(Color.Red);
                sheet.Cells["A1:B1"].Style.Fill.PatternType = ExcelFillStyle.DarkTrellis;
                sheet.Cells["A1:B1"].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                sheet.Cells["A1:B1"].Style.Fill.PatternColor.SetColor(Color.LightCyan);
                sheet.Cells["A2:B2"].Style.Font.Italic = true;
                sheet.Cells["B1:B2"].Style.Font.Name = "Consolas";

                var range = sheet.Cells["A1:C4"];
                var exporter = range.CreateHtmlExporter();
                exporter.Settings.SetColumnWidth = true;
                exporter.Settings.SetRowHeight = true;
                exporter.Settings.Accessibility.TableSettings.AddAccessibilityAttributes = false;
                var result = exporter.GetSinglePage();
                File.WriteAllText("c:\\temp\\" + sheet.Name + ".html", result);
                var resultAsync = await exporter.GetSinglePageAsync();
                SaveAndCleanup(package);
                Assert.AreEqual(result, resultAsync);

            }
        }
        [TestMethod]
        public void WriteHtmlFiles()
        {
            using (var package = OpenTemplatePackage("issue485.xlsx"))
            {
                SaveRangeFile(package, "Avances", "B3:T112");
                SaveRangeFile(package, "Avances TD", "B2:L62");
                SaveRangeFile(package, "Excel docClikalia", "A1:Q345");
            }
            using (var package = OpenTemplatePackage("Calculate Worksheet.xlsx"))
            {
                SaveRangeFile(package, "All Questions", "D1:BF1049", 2);
            }
        }

        [TestMethod]
        public void WriteAllsvenskan()
        {
            using (var p = OpenTemplatePackage("Allsvenskan2001.xlsx"))
            {
                var sheet = p.Workbook.Worksheets[0];
                var exporter = sheet.Cells["B5:M19"].CreateHtmlExporter();
                exporter.Settings.SetColumnWidth = true;
                exporter.Settings.SetRowHeight = true;
                var html=exporter.GetSinglePage();
                File.WriteAllText("c:\\temp\\" + sheet.Name + ".html", html);
            }
        }
        [TestMethod]
        public async Task WriteImagesAsync()
        {
            using (var p = OpenTemplatePackage("20-CreateAFileSystemReport.xlsx"))
            {
                var sheet = p.Workbook.Worksheets[0];                
                var exporter = sheet.Cells["A1:E30"].CreateHtmlExporter();
                exporter.Settings.SetColumnWidth = true;
                exporter.Settings.SetRowHeight = true;
                exporter.Settings.IncludePictures = true;
                exporter.Settings.Minify = false;
                var html = exporter.GetSinglePage();
                var htmlAsync = await exporter.GetSinglePageAsync(); 
                File.WriteAllText("c:\\temp\\" + sheet.Name + ".html", html);
                Assert.AreEqual(html, htmlAsync);
            }
        }

        private static void SaveRangeFile(ExcelPackage package, string ws, string address, int headerRows=1)
        {
            var sheet = package.Workbook.Worksheets[ws];
            var range = sheet.Cells[address];
            var exporter = range.CreateHtmlExporter();
            exporter.Settings.SetColumnWidth = true;
            exporter.Settings.HeaderRows = headerRows;
            File.WriteAllText("c:\\temp\\" + sheet.Name + ".html", exporter.GetSinglePage());
        }
    }
}
    