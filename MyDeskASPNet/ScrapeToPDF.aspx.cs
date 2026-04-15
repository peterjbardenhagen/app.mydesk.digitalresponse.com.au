using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebSupergoo;
using WebSupergoo.ABCpdf11;
using System.IO;
using System.Diagnostics;

namespace MyDeskASPNet
{
    public partial class ScrapeToPDF : System.Web.UI.Page
    {
        public string url;
        public string system;
        public string module;

        protected void Page_Load(object sender, EventArgs e)
        {
            //try
            //{
                url = Request.QueryString.Get("url") + "";
                system = Request.QueryString.Get("system") + "";
                module = Request.QueryString.Get("module") + "";
                if (url != "") //temp
                {
                    url += "&email=true";

                    string filePath = Server.MapPath("/Clients/" + system + "/" + module + "s/Files/" + module + ".pdf").ToLower();
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                ///* abcpdf way */
                Doc theDoc = new Doc();
                theDoc.HtmlOptions.PageCachePurge();
                theDoc.HtmlOptions.PageCacheClear();
                theDoc.HtmlOptions.PageCacheEnabled = false;
                theDoc.HtmlOptions.UseNoCache = true;
                theDoc.HtmlOptions.FontEmbed = true;
                theDoc.HtmlOptions.BrowserWidth = 750;
                theDoc.HtmlOptions.FontSubstitute = true;
                theDoc.HtmlOptions.FontProtection = false;
                theDoc.HtmlOptions.AddTags = true;
                theDoc.MediaBox.String = "A4";
                theDoc.Rect.String = theDoc.MediaBox.String;
                theDoc.Rect.Width = theDoc.Rect.Width - 10;
                theDoc.Rect.Height = theDoc.Rect.Height - 10;
                theDoc.Rect.Inset(5, 5);
                AddPage(ref theDoc, url);
                byte[] theData = theDoc.GetData();
                Response.Clear();
                Response.ContentType = "application/pdf";
                Response.AddHeader("content-disposition", "inline");
                Response.AddHeader("content-length", theData.Length.ToString());
                Response.BinaryWrite(theData);
                File.WriteAllBytes(filePath, theData);
                Response.End();
            }
            //}
            //catch (Exception ex)
            //{
            //    Response.Write(ex.Message);
            //    Response.Write(url);
            //}
        }

        public static void AddPage(ref Doc theDoc, string url)
        {
            // Setup the page
            theDoc.Page = theDoc.AddPage();
            //theDoc.HtmlOptions.Engine = WebSupergoo.ABCpdf9.EngineType.Gecko;
            var theId = theDoc.AddImageUrl(url);

            while (true)
            {
                if (!theDoc.Chainable(theId))
                {
                    break;
                }
                theDoc.Page = theDoc.AddPage();
                theId = theDoc.AddImageToChain(theId);
            }
            // flatten PDF
            for (int i = 1; i <= theDoc.PageCount; i++)
            {
                theDoc.PageNumber = i;
                theDoc.Flatten();
            }
        }
    }
}
