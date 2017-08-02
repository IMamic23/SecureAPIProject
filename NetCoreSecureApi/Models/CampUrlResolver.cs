using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCodeCamp.Data.Entities;
using NetCoreSecureApi.Controllers;

namespace NetCoreSecureApi.Models
{
    public class CampUrlResolver : IValueResolver<Camp, CampModel, string>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CampUrlResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public string Resolve(Camp source, CampModel destination, string destMember, ResolutionContext context)
        {
            var url = (IUrlHelper)_httpContextAccessor.HttpContext.Items[BaseController.Urlhelper];
            return url.Link("CampGet", new
            {
                moniker = source.Moniker
            });
        }
    }
}
