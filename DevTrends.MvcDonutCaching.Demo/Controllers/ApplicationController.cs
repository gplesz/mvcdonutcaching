﻿using System.Web.Mvc;
using DevTrends.MvcDonutCaching.Annotations;

namespace DevTrends.MvcDonutCaching.Demo.Controllers
{
    public abstract partial class ApplicationController : Controller
    {
        public OutputCacheManager OutputCacheManager
        {
            get; 
            [UsedImplicitly] set;
        }
    }
}