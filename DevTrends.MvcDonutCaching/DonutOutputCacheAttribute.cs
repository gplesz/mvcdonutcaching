﻿using System;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using System.Web.UI;
using System.Web;

namespace DevTrends.MvcDonutCaching
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class DonutOutputCacheAttribute : ActionFilterAttribute, IExceptionFilter
    {
        private const string CallbackKey = "d0nutCallback";

        private readonly IKeyGenerator _keyGenerator;
        private readonly IDonutHoleFiller _donutHoleFiller;
        private readonly IExtendedOutputCacheManager _outputCacheManager;
        private readonly ICacheSettingsManager _cacheSettingsManager;
        private readonly ICacheHeadersHelper _cacheHeadersHelper;

        private CacheSettings _cacheSettings;
        private string _cacheKey;

        public int Duration { get; set; }
        public string VaryByParam { get; set; }
        public string VaryByCustom { get; set; }
        public string CacheProfile { get; set; }
        public OutputCacheLocation Location { get; set; }

        public DonutOutputCacheAttribute()
        {
            var keyBuilder = new KeyBuilder();

            _keyGenerator = new KeyGenerator(keyBuilder);
            _donutHoleFiller = new DonutHoleFiller(new EncryptingActionSettingsSerialiser(new ActionSettingsSerialiser(), new Encryptor()));
            _outputCacheManager = new OutputCacheManager(OutputCache.Instance, keyBuilder);
            _cacheSettingsManager = new CacheSettingsManager();
            _cacheHeadersHelper = new CacheHeadersHelper();

            Duration = -1;
            Location = (OutputCacheLocation)(-1);
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            _cacheSettings = BuildCacheSettings();

            if (_cacheSettings.IsServerCachingEnabled)
            {
                _cacheKey = _keyGenerator.GenerateKey(filterContext, _cacheSettings);

                var cachedItem = _outputCacheManager.GetItem(_cacheKey);

                if (cachedItem != null)
                {
                    filterContext.Result = new ContentResult
                    { 
                        Content = _donutHoleFiller.ReplaceDonutHoleContent(cachedItem.Content, filterContext),
                        ContentType = cachedItem.ContentType
                    };
                }
            }

            if (filterContext.Result == null)
            {
                var callbackKey = BuildCallbackKey(filterContext);

                var cachingWriter = new StringWriter(CultureInfo.InvariantCulture);

                var originalWriter = filterContext.HttpContext.Response.Output;

                filterContext.HttpContext.Response.Output = cachingWriter;

                filterContext.HttpContext.Items[callbackKey] = new Action<bool>(hasErrors =>
                {
                    filterContext.HttpContext.Items.Remove(callbackKey);

                    filterContext.HttpContext.Response.Output = originalWriter;

                    if (!hasErrors)
                    {
                        var cacheItem = new CacheItem
                        {
                            Content = cachingWriter.ToString(),
                            ContentType = filterContext.HttpContext.Response.ContentType
                        };

                        filterContext.HttpContext.Response.Write(_donutHoleFiller.RemoveDonutHoleWrappers(cacheItem.Content, filterContext));

                        if (_cacheSettings.IsServerCachingEnabled && filterContext.HttpContext.Response.StatusCode == 200)
                        {
                            _outputCacheManager.AddItem(_cacheKey, cacheItem, DateTime.Now.AddSeconds(_cacheSettings.Duration));
                        }
                    }
                });
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            ExecuteCallback(filterContext, false);

            if (!filterContext.IsChildAction)
            {
                _cacheHeadersHelper.SetCacheHeaders(filterContext.HttpContext.Response, _cacheSettings);
            }
        }

        public void OnException(ExceptionContext filterContext)
        {
            ExecuteCallback(filterContext, true);
        }

        private void ExecuteCallback(ControllerContext context, bool hasErrors)
        {
            var callbackKey = BuildCallbackKey(context);

            var callback = context.HttpContext.Items[callbackKey] as Action<bool>;

            if (callback != null)
            {
                callback.Invoke(hasErrors);
            }
        }

        private string BuildCallbackKey(ControllerContext context)
        {
            var actionName = context.RouteData.Values["action"].ToString();
            var controllerName = context.RouteData.Values["controller"].ToString();

            return string.Format("{0}.{1}.{2}", CallbackKey, controllerName, actionName);
        }
        
        private CacheSettings BuildCacheSettings()
        {
            CacheSettings cacheSettings;

            if (string.IsNullOrEmpty(CacheProfile))
            {
                cacheSettings = new CacheSettings
                {
                    IsCachingEnabled = _cacheSettingsManager.IsCachingEnabledGlobally,
                    Duration = Duration,
                    VaryByCustom = VaryByCustom,
                    VaryByParam = VaryByParam,
                    Location = (int)Location == -1 ? OutputCacheLocation.Server : Location
                };
            }
            else
            {
                var cacheProfile = _cacheSettingsManager.RetrieveOutputCacheProfile(CacheProfile);

                cacheSettings = new CacheSettings
                {
                    IsCachingEnabled = _cacheSettingsManager.IsCachingEnabledGlobally && cacheProfile.Enabled,
                    Duration = Duration == -1 ? cacheProfile.Duration : Duration,
                    VaryByCustom = VaryByCustom ?? cacheProfile.VaryByCustom,
                    VaryByParam = VaryByParam ?? cacheProfile.VaryByParam,
                    Location = (int)Location == -1 ? ((int)cacheProfile.Location == -1 ? OutputCacheLocation.Server : cacheProfile.Location) : Location
                };
            }

            if (cacheSettings.Duration == -1)
            {
                throw new HttpException("The directive or the configuration settings profile must specify the 'duration' attribute.");
            }

            if (cacheSettings.Duration < 0)
            {
                throw new HttpException("The 'duration' attribute must have a value that is greater than or equal to zero.");
            }

            return cacheSettings;
        }        
    }
}