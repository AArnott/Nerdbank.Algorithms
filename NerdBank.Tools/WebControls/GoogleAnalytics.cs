using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NerdBank.Tools.WebControls
{
	[DefaultProperty("Account")]
	[ToolboxData("<{0}:GoogleAnalytics runat=server></{0}:GoogleAnalytics>")]
	public class GoogleAnalytics : WebControl
	{
		[Bindable(true)]
		[Category("Behavior")]
		public string Account
		{
			get
			{
				String s = (String)ViewState["Account"];
				return ((s == null) ? String.Empty : s);
			}

			set
			{
				ViewState["Account"] = value;
			}
		}

		protected const bool HideWhenLocalDefault = true;
		[Bindable(true)]
		[Category("Behavior")]
		[DefaultValue(HideWhenLocalDefault)]
		public bool HideWhenLocal
		{
			get
			{
				if (ViewState["HideWhenLocal"] == null) return HideWhenLocalDefault;
				return (bool)ViewState["HideWhenLocal"];
			}
			set
			{
				ViewState["HideWhenLocal"] = value;
			}
		}

		protected override void RenderContents(HtmlTextWriter output)
		{
			if (HideWhenLocal && Page.Request.IsLocal) return;
			if (string.IsNullOrEmpty(Account))
			{
				output.Write(@"<!-- Google Analytics inactive because required Account property was not supplied. -->");
			}
			else
			{
				output.Write(string.Format(@"
	<script src=""{0}"" type=""text/javascript"">
	</script>
	<script type=""text/javascript"">
		_uacct = ""{1}"";
		urchinTracker();
	</script>
", Page.Request.IsSecureConnection ? 
	"https://ssl.google-analytics.com/urchin.js" : "http://www.google-analytics.com/urchin.js",
	Account));
			}
		}
	}
}
