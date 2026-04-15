using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text.RegularExpressions;

namespace MyDeskASPNet
{
    public partial class GenerateQuote : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string host = Request.ServerVariables["SERVER_NAME"];
            if (host == "localhost")
            {
                host = "techlight.digitalresponse.com.au";
            }

            //bool email = true; // might want to fix later
            //bool fax = false; // might want to fix later
            int qid = Convert.ToInt16(Request["qid"]);
            string notes = Request["notes"]+"".ToString();
            string attention = Request["attention"]+"".ToString();
            string toEmail = Request["toEmail"]+"".ToString();
            string fromFax = Request["fromFax"]+"".ToString();
            string toFax = Request["toFax"]+"".ToString();
            string workingDir = Request["workingDir"]+"".ToString();
            string system = workingDir.Replace("/Clients/", "");
            int mode = Convert.ToInt16(Request["mode"]);

            string path = Server.MapPath(workingDir + "/Quotes/Files/");

            if (qid == 0)
            {
                //email = true;
                qid = 2068;
                workingDir = "/Clients/SalesEngineTL";
                toEmail = "peterb@digitalresponse.com.au";
                mode = 1;
            }

            if (qid > 0)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string file = path + "\\Quote.pdf";
                File.Delete(file);

                string wcUrl = "";

                if (Request.ServerVariables["SERVER_NAME"] == "localhost")
                {
					wcUrl = "https://" + Request.ServerVariables["SERVER_NAME"] + ":" + Request.ServerVariables["SERVER_PORT"] + "/ScrapeToPDF.aspx?system=" + system + "&module=quote&url=" + "https://" + host + workingDir + "/Quotes/View.asp?qid=" + qid.ToString();
				} else {
                    wcUrl = "https://" + Request.ServerVariables["SERVER_NAME"] + ":" + Request.ServerVariables["SERVER_PORT"] + "/MyDeskASPNet/ScrapeToPDF.aspx?system=" + system + "&module=quote&url=" + "https://" + host + workingDir + "/Quotes/View.asp?qid=" + qid.ToString();
                }
				
				//Response.Write(wcUrl);
        
                using (var wc = new System.Net.WebClient())
                {
                    //try
                    //{
                    try
                    {
                        wc.OpenRead(wcUrl);
                        //wc.DownloadFile(wcUrl, file);
                    }
                    catch (Exception ex)
                    {
                        Response.Write(ex.Message + " " + wcUrl);
                    }
                }

                        //if (mode == 1)
                        //{
                        //    email = true;
                        //}
                        //else
                        //{
                        //    email = false;
                        //}

                        //if (mode == 2)
                        //{
                        //    fax = true;
                        //}
                        //else
                        //{
                        //    fax = false;
                        //}

                        System.Threading.Thread.Sleep(5000);

				if (mode == 1)
                        {
					Response.Redirect(workingDir + "/Quotes/Email_Proc.asp?Notes=" + notes + "&Attention=" + attention + "&ToEmail=" + toEmail + "&Qid=" + qid.ToString());
				}
                        else
                        {
                    Response.Redirect(workingDir + "/Quotes/Fax_Proc.asp?Notes=" + notes + "&Attention=" + attention + "&ToEmail=" + toEmail + "&Qid=" + qid.ToString());
                }

                    //}
                    //catch (Exception ex)
                    //{
                    //    Response.Write(ex.Message + " " + wcUrl);
                    //}
            }
        }
    }
}