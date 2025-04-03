using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DUT_Accommodation_Platform.Controllers
{
    public class LandlordController : Controller
    {
        [AllowAnonymous]
        // GET: Landlord
        public ActionResult Dashboard()
        {
            return View();
        }
    }
}