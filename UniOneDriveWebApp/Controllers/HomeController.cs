﻿/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using UniOneDriveWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UniOneDriveWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            OneDriveUser user = OneDriveUser.UserForRequest(this.Request);
            ViewBag.ShowSignInButtons = (user == null);

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}