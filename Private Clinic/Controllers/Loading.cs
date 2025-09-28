using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Private_Clinic.Controllers
{
    public class LoadingController : Controller
    {
        public ActionResult Index()
        {
            return View(); // trả về Views/Loading/Index.cshtml
        }
    }
}