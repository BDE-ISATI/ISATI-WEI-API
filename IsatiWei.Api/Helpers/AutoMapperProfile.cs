using AutoMapper;
using IsatiWei.Api.Models;
using IsatiWei.Api.Models.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserRegister, User>();
            CreateMap<UserLogin, User>();
        }
    }
}
