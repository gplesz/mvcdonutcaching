using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DevTrends.MvcDonutCaching.Demo.Helpers
{
    public static class T4MVCDonutCachingExtensions
    {
        private static readonly IActionSettingsSerialiser Serialiser = new EncryptingActionSettingsSerialiser(new ActionSettingsSerialiser(), new Encryptor());

        private static string GetSerialisedActionSettings(string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            var actionSettings = new ActionSettings
            {
                ActionName = actionName,
                ControllerName = controllerName,
                RouteValues = routeValues
            };
            return Serialiser.Serialise(actionSettings);
        }

        private static MvcHtmlString getDonutCachingCommentsForT4MvcActionResult(HtmlHelper htmlHelper, ActionResult result)
        {
            var actionResultDetails = System.Web.Mvc.T4Extensions.GetT4MVCResult(result);
            string actionName = actionResultDetails.Action;
            string controllerName = actionResultDetails.Controller;
            RouteValueDictionary routeValues = actionResultDetails.RouteValueDictionary;

            var serialisedActionSettings = GetSerialisedActionSettings(actionName, controllerName, routeValues);
            var actionContent = System.Web.Mvc.Html.ChildActionExtensions.Action(htmlHelper, actionName, controllerName, routeValues);
            return new MvcHtmlString(string.Format("<!--Donut#{0}#-->{1}<!--EndDonut-->", serialisedActionSettings, actionContent));
        }


        public static MvcHtmlString Action(this HtmlHelper htmlHelper, ActionResult result, bool excludeFromParentCache)
        {
            if (excludeFromParentCache)
            {
                return getDonutCachingCommentsForT4MvcActionResult(htmlHelper, result);
            }
            return System.Web.Mvc.T4Extensions.Action(htmlHelper, result);
        }

    }
}