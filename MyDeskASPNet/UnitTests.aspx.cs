using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MyDeskASPNet
{
    public partial class UnitTests : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Server.Execute("GenerateQuote.aspx?Mode=1&Qid=2068&Attention=peter&ToEmail=peterb@digitalresponse.com.au&FromFax=&ToFax=&CurrencyName=Australia%20Dollars&CurrencyRate=1&CurrencyPrefix=$&Notes=peter&WorkingDir=/Clients/SalesEngineTL");
        }
    }
}