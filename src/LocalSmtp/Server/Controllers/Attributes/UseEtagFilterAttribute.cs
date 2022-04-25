/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Controllers
*/

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
