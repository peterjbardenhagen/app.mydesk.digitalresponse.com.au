using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using WebSupergoo;
using WebSupergoo.ABCpdf9;

namespace MyDeskASPNet
{
    public partial class MakeThumbnails : System.Web.UI.Page
    {
        public string dir;

        protected void Page_Load(object sender, EventArgs e)
        {
            string[] dir = new string[1];
            dir[0] = @"C:\inetpub\Websites\mydesk.com.au_2.0\Clients\SalesEngineTT\FilesLibrary\Files";
            
            foreach (string theDir in dir) {
                foreach (string file in Directory.GetFiles(theDir))
                {
                    if (!File.Exists(file.ToString() + "_thumb.jpg"))
                    {
                        if (Path.GetExtension(file) == ".pdf")
                        {
                            try
                            {
                                Doc theDoc = new Doc();
                                theDoc.Read(file);
                                theDoc.Rendering.DotsPerInch = 36;
                                theDoc.PageNumber = 0;
                                theDoc.Rect.String = theDoc.CropBox.String;
                                theDoc.Rendering.ColorSpace = XRendering.ColorSpaceType.Rgb;
                                theDoc.Rendering.Save(file.ToString() + "_thumb.jpg");
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
        }
    }
}