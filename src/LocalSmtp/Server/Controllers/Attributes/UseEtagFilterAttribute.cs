﻿// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Controllers

using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace LocalSmtp.Server.Controllers.Attributes
{
    public class UseEtagFilterAttribute : Attribute, IActionFilter
    {
        public UseEtagFilterAttribute() { }

        private const string IfNoneMatchHeader = "If-None-Match";

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.Request.Method == HttpMethod.Get.Method)
            {
                if (context.HttpContext.Response.StatusCode == 200)
                {

                    if (context.Result is ObjectResult results)
                    {
                        string hashable = null;
                        if (results.Value is IEnumerable<ICacheByKey> cacheableList)
                        {
                            if (cacheableList.Any())
                            {
                                hashable = string.Join(",", cacheableList.Select(i => i.CacheKey));
                            }
                            else
                            {
                                hashable = "empty";
                            }

                        }
                        else if (results.Value is ICacheByKey cachableObject)
                        {
                            hashable = cachableObject.CacheKey;
                        }

                        if (!string.IsNullOrEmpty(hashable))
                        {

                            byte[] hashBytes = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(hashable));
                            string etag = Convert.ToBase64String(hashBytes);

                            if (context.HttpContext.Request.Headers.Keys.Contains(IfNoneMatchHeader) && context.HttpContext.Request.Headers[IfNoneMatchHeader].ToString() == etag)
                            {
                                context.Result = new StatusCodeResult(304);
                            }
                            context.HttpContext.Response.Headers.Add("ETag", new[] { etag });
                        }
                    }
                }
            }
        }
    }
}
