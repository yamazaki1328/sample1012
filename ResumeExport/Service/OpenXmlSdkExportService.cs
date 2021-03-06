﻿using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NotesFor.HtmlToOpenXml;
using ResumeExport.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ResumeExport.Service
{
    //檔案匯出服務 (OpenXML SDK)
    public class OpenXmlExportService
    {
        /// <summary>
        /// 完全以 HTML 編輯內容，並將其匯出成檔案
        /// </summary>
        /// <param name="result">執行結果</param>
        /// <param name="msg">回傳: 訊息</param>
        /// <returns>匯出的 docx 文件資訊流</returns>
        public byte[] ExportByHtml(out bool result, out string msg)
        {
            result = true;
            msg = "";
            MemoryStream ms = new MemoryStream();

            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
                {
                    #region 文件宣告與定義

                    //建立 MainDocumentPart 類別物件 mainPart，加入主文件部分 
                    MainDocumentPart mainPart = doc.AddMainDocumentPart();
                    //實例化 Document(w) 部分
                    mainPart.Document = new Document();
                    //建立 Body 類別物件，於加入 Doucment(w) 中加入 Body 內文
                    Body body = mainPart.Document.AppendChild(
                        new Body(
                            new SectionProperties(new PageMargin()
                            {
                                Left = 600,
                                Right = 600,
                                Bottom = 700,
                                Top = 740
                            })));

                    #endregion

                    #region 產出內容

                    Paragraph paragraph;
                    Run run;

                    //建立「段落 Paragraph」類別物件，於 Body 本文中加入段落 Paragraph(p)                    
                    paragraph = body.AppendChild(new Paragraph());
                    //建立 Run 類別物件，於 段落 Paragraph(p) 中加入文字屬性 Run(r) 範圍
                    run = paragraph.AppendChild(new Run());
                    //在文字屬性 Run(r) 範圍中加入文字內容
                    run.AppendChild(new Text("履歷匯出範例"));
                    run.AppendChild(new Break());
                    run.AppendChild(new Text("(使用 HTML 直接匯出)"));


                    //建立要產出的 HTML 內容                    
                    Resume model = new Resume();
                    StringBuilder html = new StringBuilder();
                    html.Append("Name: " + model.Name + "<br />");
                    html.Append("Gender: " + model.Gender + "<br />");
                    html.Append("Email: " + model.Email + "<br />");
                    html.Append("Address: " + model.Address + "<br />");
                    html.Append("Phone: " + model.Phone + "<br />");
                    html.Append("Mobile: " + model.Mobile + "<br />");
                    html.Append("Description1:<br />" + HttpUtility.HtmlDecode(model.Description1) + "<br /></p>");
                    html.Append("Description2:<br />" + HttpUtility.HtmlDecode(model.Description2) + "<br /></p>");

                    if (model.JobHistory.Count > 0)
                    {
                        int i = 1;
                        model.JobHistory = model.JobHistory.OrderBy(x => x.StartDT).ToList();
                        html.Append("<p>簡歷</p>");

                        //注意: HTML Table 轉成 OpenXML SDK 的 Table 物件時不會有框線，因此框線須直接於 HTML table Tag 中設定
                        html.Append("<table width=\"100%\" border=\"1\" cellspacing=\"0\" cellpadding=\"4\"><tr><th>項目</th><th>任職</th><th>職稱</th><th>開始時間</th><th>結束時間</th></tr>");
                        foreach (var h in model.JobHistory)
                        {
                            html.Append("<tr>");
                            html.Append("<td>" + i.ToString() + "</td>");
                            html.Append("<td>" + h.CompanyName + "</td>");
                            html.Append("<td>" + h.JobTitle + "</td>");
                            html.Append("<td>" + (h.StartDT.HasValue ? h.StartDT.Value.ToShortDateString() : "") + "</td>");
                            html.Append("<td>" + (h.EndDT.HasValue ? h.EndDT.Value.ToShortDateString() : "") + "</td>");
                            html.Append("</tr>");
                            i++;
                        }
                        html.Append("</table>");
                    }

                    //將 HTML 內容轉換成 XML，並添加至文件內
                    HtmlConverter converter = new HtmlConverter(mainPart);
                    converter.ParseHtml(html.ToString());

                    //分隔線
                    paragraph = body.AppendChild(new Paragraph(new Run(new Text("-----------------------------------------------------------------"))));

                    #region 動態建立表格

                    //建立一個新的空白表格
                    Table table = new Table();

                    //建立表格屬性物件 (TableProperties object)，並設定表格邊框及框度
                    TableProperties tblProp = new TableProperties(
                        new TableBorders(
                            new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 }),
                        new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct });


                    //將表格屬性物件 (TableProperties) 指定給表格 (table) 物件
                    TableStyle tableStyle = new TableStyle { Val = "LightShadingAccent1" };
                    tblProp.TableStyle = tableStyle;
                    table.AppendChild(tblProp); //<= 指定

                    //建立表格儲存格屬性
                    TableCellProperties cellProp = new TableCellProperties(
                        new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center },
                        new TableCellWidth() { Type = TableWidthUnitValues.Auto }
                        );


                    //建立表格列 (Row) 及儲存格 (Cell)
                    TableRow tableRow = new TableRow();
                    TableCell tableCell1 = new TableCell();

                    //將儲存格屬性 (TableCellProperties) 指定給新建立的儲存格
                    tableCell1.Append(cellProp);

                    //在儲存格中添加段落，並於段落內加入文字
                    tableCell1.Append(new Paragraph(new Run(new Text("測試 Some cell text."))));

                    //將儲存格添加至表格列中
                    tableRow.Append(tableCell1);


                    //建立第二個儲存格 (直接複製第一個儲存格)，並加入至表格列中
                    TableCell tableCell2 = new TableCell(tableCell1.OuterXml);
                    tableRow.Append(tableCell2);


                    //最後將表格列添加到表格物件上
                    table.Append(tableRow);

                    //將表格附加到文件 Body 上
                    mainPart.Document.Body.Append(table);

                    #endregion

                    #endregion

                    #region 套用樣式

                    //由於已經定義了段落樣式，因此將文件中所有的段落取出後，一一套用段落樣式
                    foreach (var p in mainPart.Document.Descendants<Paragraph>())
                    {
                        ApplyStyleToParagraph(doc, "BasicParagraphStyle", "Basic Paragraph Style", p);
                        p.ParagraphProperties.SpacingBetweenLines = new SpacingBetweenLines() { After = "300", LineRule = LineSpacingRuleValues.Auto };
                    }

                    //上一步驟會將一般文章段落樣式套在表格儲存格內的文字段落，因此取出表格內的所有段落，將樣式覆寫
                    foreach (var t in mainPart.Document.Descendants<Table>())
                    {
                        foreach (var cell_p in t.Descendants<Paragraph>())
                        {
                            cell_p.ParagraphProperties.SpacingBetweenLines = new SpacingBetweenLines() { After = "0", LineRule = LineSpacingRuleValues.Auto };
                        }
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                result = false;
                msg = ex.Message;
            }


            if (result)
            {
                return ms.ToArray();
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// 以既有檔案進行套印，並將其匯出成檔案
        /// </summary>
        /// <param name="result">執行結果</param>
        /// <param name="msg">回傳: 訊息</param>
        /// <returns>匯出的 docx 文件資訊流</returns>
        public byte[] ExportByDocx(out bool result, out string msg)
        {
            result = true;
            msg = "";            
            MemoryStream ms = new MemoryStream();

            try
            {
                //使用書籤
                byte[] templateBytes = System.IO.File.ReadAllBytes(HttpContext.Current.Server.MapPath("~/App_Data/MyResumeSample_Bookmark.docx"));
                new MemoryStream(templateBytes).CopyTo(ms);
                using (WordprocessingDocument doc = WordprocessingDocument.Open(ms, true))
                {
                    Resume model = new Resume();
                    SetBookmarkValue(doc, "NAME", model.Name ?? "");
                    SetBookmarkValue(doc, "GENDER", model.Gender ?? "");
                    SetBookmarkValue(doc, "EMAIL", model.Email ?? "");
                    SetBookmarkValue(doc, "ADDRESS", model.Address ?? "");
                    SetBookmarkValue(doc, "PHONE", model.Phone ?? "");
                    SetBookmarkValue(doc, "MOBBILE", model.Mobile ?? "");
                    SetBookmarkValueWithHtmlValue(doc, "DESCRIPTION1", model.Description1 ?? "");
                    SetBookmarkValueWithHtmlValue(doc, "DESCRIPTION2", model.Description2 ?? "");
                    

                    #region 動態建立表格

                    //建立一個新的空白表格
                    Table table = new Table();

                    //建立表格屬性物件 (TableProperties object)，並設定表格邊框及框度
                    TableProperties tblProp = new TableProperties(
                        new TableBorders(
                            new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 },
                            new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Sawtooth), Size = 1 }),
                        new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct });


                    //將表格屬性物件 (TableProperties) 指定給表格 (table) 物件
                    TableStyle tableStyle = new TableStyle { Val = "LightShadingAccent1" };
                    tblProp.TableStyle = tableStyle;
                    table.AppendChild(tblProp); //<= 指定

                    //建立表格儲存格屬性
                    TableCellProperties cellProp = new TableCellProperties(
                        new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center },
                        new TableCellWidth() { Type = TableWidthUnitValues.Auto }
                        );


                    //建立表格列 (Row) 及儲存格 (Cell)
                    TableRow tableRow = new TableRow();
                    TableCell tableCell1 = new TableCell();

                    //將儲存格屬性 (TableCellProperties) 指定給新建立的儲存格
                    tableCell1.Append(cellProp);

                    //在儲存格中添加段落，並於段落內加入文字
                    tableCell1.Append(new Paragraph(new Run(new Text("測試 Some cell text."))));

                    //將儲存格添加至表格列中
                    tableRow.Append(tableCell1);


                    //建立第二個儲存格 (直接複製第一個儲存格)，並加入至表格列中
                    TableCell tableCell2 = new TableCell(tableCell1.OuterXml);
                    tableRow.Append(tableCell2);


                    //最後將表格列添加到表格物件上
                    table.Append(tableRow);


                    //var table = new Table(
                    //new TableProperties(
                    //    new TableStyle() { Val = "TableGrid" },
                    //    new TableWidth() { Width = 0, Type = TableWidthUnitValues.Auto }
                    //    ),
                    //    new TableGrid(
                    //        new GridColumn() { Width = (UInt32Value)1018U },
                    //        new GridColumn() { Width = (UInt32Value)3544U }),
                    //new TableRow(
                    //    new TableCell(
                    //        new TableCellProperties(
                    //            new TableCellWidth() { Width = 0, Type = TableWidthUnitValues.Auto }),
                    //        new Paragraph(
                    //            new Run(
                    //                new Text("Category Name"))
                    //        )),
                    //    new TableCell(
                    //        new TableCellProperties(
                    //            new TableCellWidth() { Width = 4788, Type = TableWidthUnitValues.Dxa }),
                    //        new Paragraph(
                    //            new Run(
                    //                new Text("Value"))
                    //        ))
                    //),
                    //new TableRow(
                    //    new TableCell(
                    //        new TableCellProperties(
                    //            new TableCellWidth() { Width = 0, Type = TableWidthUnitValues.Auto }),
                    //        new Paragraph(
                    //            new Run(
                    //                new Text("C1"))
                    //        )),
                    //    new TableCell(
                    //        new TableCellProperties(
                    //            new TableCellWidth() { Width = 0, Type = TableWidthUnitValues.Auto }),
                    //        new Paragraph(
                    //            new Run(
                    //                new Text("V1"))
                    //        ))
                    //));

                    #endregion

                    SetBookmarkValueWithTable(doc, "TABLE", table);
                    

                    #region 套用樣式

                    var mainPart = doc.MainDocumentPart;
                    
                    //由於已經定義了段落樣式，因此將文件中所有的段落取出後，一一套用段落樣式
                    foreach (var p in mainPart.Document.Descendants<Paragraph>())
                    {
                        ApplyStyleToParagraph(doc, "BasicParagraphStyle", "Basic Paragraph Style", p);
                        p.ParagraphProperties.SpacingBetweenLines = new SpacingBetweenLines() { After = "300", LineRule = LineSpacingRuleValues.Auto };
                    }
                    

                    //上一步驟會將一般文章段落樣式套在表格儲存格內的文字段落，因此取出表格內的所有段落，將樣式覆寫
                    foreach (var t in mainPart.Document.Descendants<Table>())
                    {
                        foreach (var cell_p in t.Descendants<Paragraph>())
                        {
                            cell_p.ParagraphProperties.SpacingBetweenLines = new SpacingBetweenLines() { After = "0", LineRule = LineSpacingRuleValues.Auto };
                        }
                    }

                    #endregion

                }


                //使用文字替換(不建議)
                //byte[] templateBytes = System.IO.File.ReadAllBytes(HttpContext.Current.Server.MapPath("~/App_Data/MyResumeSample2.docx"));
                //new MemoryStream(templateBytes).CopyTo(ms);
                //using (WordprocessingDocument doc = WordprocessingDocument.Open(ms, true))
                //{
                //    var body = doc.MainDocumentPart.Document.Body;
                //    var paras = body.Elements<Paragraph>();

                //    Resume model = new Resume();
                //    foreach (var para in paras)
                //    {
                //        foreach (var run in para.Elements<Run>())
                //        {
                //            foreach (var text in run.Elements<Text>())
                //            {
                //                if (text.Text.Contains("REPLACE-TO-NAME#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-NAME#", model.Name ?? "");
                //                }
                //                if (text.Text.Contains("REPLACE-TO-GENDER#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-GENDER#", model.Gender ?? "");
                //                }
                //                if (text.Text.Contains("REPLACE-TO-EMAIL#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-EMAIL#", model.Email ?? "");
                //                }
                //                if (text.Text.Contains("REPLACE-TO-ADDRESS#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-ADDRESS#", model.Address ?? "");
                //                }
                //                if (text.Text.Contains("REPLACE-TO-PHONE#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-PHONE#", model.Phone ?? "");
                //                }
                //                if (text.Text.Contains("REPLACE-TO-MOBILE#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-MOBILE#", model.Mobile ?? "");
                //                }

                //                if (text.Text.Contains("REPLACE-TO-DESCRIPTION1#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-DESCRIPTION1#", "");

                //                    HtmlConverter converter = new HtmlConverter(doc.MainDocumentPart);
                //                    //HtmlConverter converter = new HtmlConverter(mainPart);
                //                    converter.ParseHtml(model.Description1);
                //                }

                //                if (text.Text.Contains("REPLACE-TO-DESCRIPTION2#"))
                //                {
                //                    text.Text = text.Text.Replace("REPLACE-TO-DESCRIPTION2#", model.Description2 ?? "");
                //                }

                //                //將 HTML 內容轉換成 XML，並添加至文件內
                //                //HtmlConverter converter = new HtmlConverter(mainPart);
                //                //converter.ParseHtml(model.Description1 ?? "");

                //            }
                //        }
                //    }
                //}


            }
            catch (Exception ex)
            {
                result = false;
                msg = ex.Message;
            }

            if (result)
            {
                return ms.ToArray();
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 設定書籤內容
        /// </summary>
        /// <param name="doc">文件</param>
        /// <param name="BookmarkName">書籤名稱 (有區分大小寫)</param>
        /// <param name="BookmarkValue">書籤內容</param>
        /// <param name="DeleteAfterSetvalue">是否在設定完書籤內容後，將書籤移除 (預設: True)</param>
        private void SetBookmarkValue(WordprocessingDocument doc, string BookmarkName, string BookmarkValue, bool DeleteAfterSetvalue = true)
        {
            Body body = doc.MainDocumentPart.Document.GetFirstChild<Body>();
            var paras = body.Elements<Paragraph>();

            //Iterate through the paragraphs to find the bookmarks inside
            foreach (var para in paras)
            {
                var bookMarkStarts = para.Elements<BookmarkStart>();
                var bookMarkEnds = para.Elements<BookmarkEnd>();

                foreach (BookmarkStart bookMarkStart in bookMarkStarts)
                {
                    if (bookMarkStart.Name == BookmarkName)
                    {
                        //Get the id of the bookmark start to find the bookmark end
                        var id = bookMarkStart.Id.Value;
                        var bookmarkEnd = bookMarkEnds.Where(i => i.Id.Value == id).First();
                        var runElement = new Run(new Text(BookmarkValue));
                        para.InsertAfter(runElement, bookmarkEnd);

                        if (DeleteAfterSetvalue)
                        {
                            //移除書籤
                            bookMarkStart.Remove();
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 將包含 HTML 的內容設定至指定的書籤內
        /// (已知問題: 若 HTML 包含圖片，圖片部分會異常無法顯示，待解決)
        /// </summary>
        /// <param name="doc">文件</param>
        /// <param name="BookmarkName">書籤名稱 (有區分大小寫)</param>
        /// <param name="Html">HTML內容</param>
        /// <param name="DeleteAFterSetvalue">是否在設定完書籤內容後，將書籤移除 (預設: True)</param>
        private void SetBookmarkValueWithHtmlValue(WordprocessingDocument doc, string BookmarkName, string Html, bool DeleteAfterSetvalue = true)
        {
            StringBuilder xhtmlBuilder = new StringBuilder();
            xhtmlBuilder.Append("<HTML>");
            xhtmlBuilder.Append("<body>");
            xhtmlBuilder.Append(Html);
            xhtmlBuilder.Append("</body>");
            xhtmlBuilder.Append("</HTML >");

            string altChunkId = "chunk_" + BookmarkName.ToLower();
            AlternativeFormatImportPart chunk = doc.MainDocumentPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.Xhtml, altChunkId);
            using (MemoryStream xhtmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xhtmlBuilder.ToString())))
            {
                chunk.FeedData(xhtmlStream);

                AltChunk altChunk = new AltChunk();
                altChunk.Id = altChunkId;

                var res = from bm in doc.MainDocumentPart.Document.Body.Descendants<BookmarkStart>()
                          where bm.Name == BookmarkName
                          select bm;
                var bookmark = res.SingleOrDefault();
                var parent = bookmark.Parent;
                parent.InsertAfterSelf(altChunk);


                if (bookmark != null && DeleteAfterSetvalue)
                {
                    bookmark.Remove();
                }
            }
        }


        /// <summary>
        /// 將表格物件設定至指定的書籤內
        /// </summary>
        /// <param name="doc">文件</param>
        /// <param name="BookmarkName">書籤名稱 (有區分大小寫)</param>
        /// <param name="table">表格內容</param>
        /// <param name="DeleteAfterSetValue">是否在設定完書籤內容後，將書籤移除 (預設: True)</param>
        /// <remarks>參考: http://stackoverflow.com/questions/1612511/insert-openxmlelement-after-word-bookmark-in-open-xml-sdk</remarks>
        private void SetBookmarkValueWithTable(WordprocessingDocument doc, string BookmarkName, Table table, bool DeleteAfterSetValue = true)
        {
            var mainPart = doc.MainDocumentPart;
            var res = from bm in mainPart.Document.Body.Descendants<BookmarkStart>()
                      where bm.Name == BookmarkName
                      select bm;
            var bookmark = res.SingleOrDefault();
            if (bookmark != null)
            {
                var parent = bookmark.Parent;   // bookmark's parent element

                //可放置文字
                // simple paragraph in one declaration
                //Paragraph newParagraph = new Paragraph(new Run(new Text("Hello, World!")));

                //// build paragraph piece by piece
                //Text text = new Text("Hello, World!");
                //Run run = new Run(new RunProperties(new Bold()));
                //run.Append(text);
                //Paragraph newParagraph = new Paragraph(run);

                Paragraph tableParagraph = new Paragraph();
                // insert after bookmark parent
                parent.InsertAfterSelf(tableParagraph);

                // insert after new paragraph
                tableParagraph.InsertBeforeSelf(table);

                if (DeleteAfterSetValue)
                {
                    bookmark.Remove();
                }
            }
        }








        #region 定義樣式
        //參考連結: https://msdn.microsoft.com/en-us/library/office/cc850838.aspx

        // Apply a style to a paragraph.
        private static void ApplyStyleToParagraph(WordprocessingDocument doc, string styleid, string stylename, Paragraph p)
        {
            // If the paragraph has no ParagraphProperties object, create one.
            if (p.Elements<ParagraphProperties>().Count() == 0)
            {
                p.PrependChild<ParagraphProperties>(new ParagraphProperties());
            }

            // Get the paragraph properties element of the paragraph.
            ParagraphProperties pPr = p.Elements<ParagraphProperties>().First();

            // Get the Styles part for this document.
            StyleDefinitionsPart part =
                doc.MainDocumentPart.StyleDefinitionsPart;

            // If the Styles part does not exist, add it and then add the style.
            if (part == null)
            {
                part = AddStylesPartToPackage(doc);
                AddNewStyle(part, styleid, stylename);
            }
            else
            {
                // If the style is not in the document, add it.
                if (IsStyleIdInDocument(doc, styleid) != true)
                {
                    // No match on styleid, so let's try style name.
                    string styleidFromName = GetStyleIdFromStyleName(doc, stylename);
                    if (styleidFromName == null)
                    {
                        AddNewStyle(part, styleid, stylename);
                    }
                    else
                        styleid = styleidFromName;
                }
            }

            // Set the style of the paragraph.
            pPr.ParagraphStyleId = new ParagraphStyleId() { Val = styleid };
        }

        // Return true if the style id is in the document, false otherwise.
        private static bool IsStyleIdInDocument(WordprocessingDocument doc, string styleid)
        {
            // Get access to the Styles element for this document.
            Styles s = doc.MainDocumentPart.StyleDefinitionsPart.Styles;

            // Check that there are styles and how many.
            int n = s.Elements<Style>().Count();
            if (n == 0)
                return false;

            // Look for a match on styleid.
            Style style = s.Elements<Style>()
                .Where(st => (st.StyleId == styleid) && (st.Type == StyleValues.Paragraph))
                .FirstOrDefault();
            if (style == null)
                return false;

            return true;
        }

        // Return styleid that matches the styleName, or null when there's no match.
        private static string GetStyleIdFromStyleName(WordprocessingDocument doc, string styleName)
        {
            StyleDefinitionsPart stylePart = doc.MainDocumentPart.StyleDefinitionsPart;
            string styleId = stylePart.Styles.Descendants<StyleName>()
                .Where(s => s.Val.Value.Equals(styleName) &&
                    (((Style)s.Parent).Type == StyleValues.Paragraph))
                .Select(n => ((Style)n.Parent).StyleId).FirstOrDefault();
            return styleId;
        }

        // Create a new style with the specified styleid and stylename and add it to the specified
        // style definitions part.
        private static void AddNewStyle(StyleDefinitionsPart styleDefinitionsPart, string styleid, string stylename)
        {
            // Get access to the root element of the styles part.
            Styles styles = styleDefinitionsPart.Styles;

            // Create a new paragraph style and specify some of the properties.
            Style style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = styleid,
                CustomStyle = true
            };
            StyleName styleName1 = new StyleName() { Val = stylename };
            BasedOn basedOn1 = new BasedOn() { Val = "Normal" };
            NextParagraphStyle nextParagraphStyle1 = new NextParagraphStyle() { Val = "Normal" };
            style.Append(styleName1);
            style.Append(basedOn1);
            style.Append(nextParagraphStyle1);

            // Create the StyleRunProperties object and specify some of the run properties.
            StyleRunProperties styleRunProperties1 = new StyleRunProperties();
            Bold bold1 = new Bold();
            Color color1 = new Color() { ThemeColor = ThemeColorValues.Accent2 };
            RunFonts font1 = new RunFonts() { EastAsia = "DFKai-SB" /*正黑體: Microsoft JhengHei, 標楷體：DFKai-SB, 細明體：MingLiU, 新細明體：PMingLiU */};
            //Italic italic1 = new Italic();
            //Specify a 12 point size.
            FontSize fontSize1 = new FontSize() { Val = "24" };
            //styleRunProperties1.Append(bold1);
            //styleRunProperties1.Append(color1);
            styleRunProperties1.Append(font1);
            styleRunProperties1.Append(fontSize1);
            //styleRunProperties1.Append(italic1);


            // Add the run properties to the style.
            style.Append(styleRunProperties1);

            // Add the style to the styles part.
            styles.Append(style);
        }

        // Add a StylesDefinitionsPart to the document.  Returns a reference to it.
        private static StyleDefinitionsPart AddStylesPartToPackage(WordprocessingDocument doc)
        {
            StyleDefinitionsPart part;
            part = doc.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            Styles root = new Styles();
            root.Save(part);
            return part;
        }

        #endregion
    }
}