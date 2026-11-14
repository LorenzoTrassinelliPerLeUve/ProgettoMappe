using _4GrapesAPI.Models;
using _4GrapesAPI.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Net;
using Microsoft.AspNet.Identity.Owin;

using _4GrapesAPI.Properties;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Globalization;

namespace _4GrapesAPI.Controllers
{
    [ValidateInput(false)]
    public class _BaseController : System.Web.Mvc.Controller
    {
        protected DB DB = new DB();

        /*SignInManager<Utente, int> _SignInManager;
        //ApplicationUserManager _ApplicationUserManager;

        public SignInManager<Utente, int> SignInManager
        {
            get
            {
                if (_SignInManager == null)
                {
                    _SignInManager = HttpContext.GetOwinContext().Get<SignInManager<Utente, int>, int>();
                }

                return _SignInManager;
            }
        }*/
        /*
        public ApplicationUserManager UserManager
        {
            get
            {
                if (_ApplicationUserManager == null)
                {
                    _ApplicationUserManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                }

                return _ApplicationUserManager;
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
        }*/

        protected void FlashMessage(string text)
        {
            TempData["FlashMessage"] = text;
            TempData["FlashMessageType"] = "green";
        }

        protected void FlashMessageError(string text)
        {
            TempData["FlashMessage"] = text;
            TempData["FlashMessageType"] = "red";
        }

        protected new JsonResult Json(object data)
        {
            return new JsonNetResult()
            {
                Data = data,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,                
            };
        }

        protected string FiltroDaSessione(string valore, string nome_filtro)
        {       
            if (Request.QueryString.Count == 0)
            {
                valore = (string)Session["Filtro." + nome_filtro];
            }
            else
            {
                Session["Filtro." + nome_filtro] = valore;
            }

            return valore;
        }

        protected int FiltroDaSessione(int valore, string nome_filtro)
        {
            if (Request.QueryString.Count == 0)
            {
                valore = (int?)Session["Filtro." + nome_filtro] ?? 0;
            }
            else
            {
                Session["Filtro." + nome_filtro] = valore;
            }

            return valore;
        }

        protected bool? FiltroDaSessione(bool? valore, string nome_filtro)
        {
            if (Request.QueryString.Count == 0)
            {
                valore = (bool?)Session["Filtro." + nome_filtro];
            }
            else
            {
                Session["Filtro." + nome_filtro] = valore;
            }

            return valore;
        }

        protected DateTime FiltroDaSessione(DateTime valore, string nome_filtro)
        {
            if (Request.QueryString.Count == 0)
            {
                valore = (DateTime?)Session["Filtro." + nome_filtro] ?? DateTime.Now;
            }
            else
            {
                Session["Filtro." + nome_filtro] = valore;
            }

            return valore;
        }

        /*protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (Request.IsAuthenticated)
            {
                if (!(filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                    filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true)))
                {
                    //Session.Clear();

                    if (Session == null || Session.Count == 0)
                    {
                        var url = Url.Action("Login", "Account");
                        Response.Redirect(url, true);
                        filterContext.Result = new RedirectResult(url, true);
                    }
                }                  
            }
        }*/

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string lang = null;            
            if (User.Identity.IsAuthenticated)
            {
                var user = User.GetUtente();

                lang = user.Lingua;
            }
            else if(Request.QueryString["lang"] != null)
            {
                lang = Request.QueryString["lang"];
            }
            else if (Request.Cookies["lang"] != null)
            {
                lang = Request.Cookies["lang"].Value;
            }
            else if (Request.UserLanguages != null && Request.UserLanguages.Length > 0 && Request.UserLanguages[0] != null)
            {
                lang = Request.UserLanguages[0];
            }
            else
            {
                lang = "en"; // Default
            }

            try
            {
                var cultureInfo = new  CultureInfo(lang);

                //cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
                //cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
                //cultureInfo.NumberFormat.PercentDecimalSeparator = ".";

                //cultureInfo.NumberFormat.CurrencyGroupSeparator = ",";
                //cultureInfo.NumberFormat.NumberGroupSeparator = ",";
                //cultureInfo.NumberFormat.CurrencyGroupSeparator = ",";

                //cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";

                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("it-IT");
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
            }
            catch (CultureNotFoundException)
            {
                // Gestisci lingue non supportate
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            }
        }       
    }

    public class JsonNetResult : JsonResult
    {
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var response = context.HttpContext.Response;

            response.ContentType = !String.IsNullOrEmpty(ContentType)
                ? ContentType
                : "application/json";

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;

            // If you need special handling, you can call another form of SerializeObject below
            var serializedObject = JsonConvert.SerializeObject(Data, Formatting.Indented);
            response.Write(serializedObject);
        }
    }
}