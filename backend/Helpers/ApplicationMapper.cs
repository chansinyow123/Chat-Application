using AutoMapper;
using backend.Data;
using backend.Models.Account;
using backend.Models.Chat;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Helpers
{
    public class ApplicationMapper : Profile
    {
        public ApplicationMapper()
        {
            CreateMap<ApplicationUser, UserModel>()
                .ForMember(
                    dest => dest.Image,
                    opt => opt.MapFrom(src => (src.File != null && src.File.Length > 0) ? $"/api/account/image/{src.Id}" : null)
                )
                .ReverseMap();

            CreateMap<PrivateChat, MessageModel>()
                .ForMember(
                    dest => dest.Message,
                    opt => opt.MapFrom(src => src.Message ?? "")
                )
                .ForMember(
                    dest => dest.File,
                    opt => opt.MapFrom(src => (src.File != null && src.File.Length > 0) ? $"/api/private/file/{src.Id}" : null)
                )
                .ForMember(
                    dest => dest.FileSize,
                    opt => opt.MapFrom(src => (src.File != null && src.File.Length > 0) ? src.File.Length : (int?) null)
                )
                .ForMember(
                    dest => dest.OnSeen,
                    opt => opt.MapFrom(src => (src.OnSeen != null))
                )
                .ReverseMap();

            CreateMap<GroupChat, MessageModel>()
                .ForMember(
                    dest => dest.Message,
                    opt => opt.MapFrom(src => src.Message ?? "")
                )
                .ForMember(
                    dest => dest.File,
                    opt => opt.MapFrom(src => (src.File != null && src.File.Length > 0) ? $"/api/group/file/{src.Id}" : null)
                )
                .ForMember(
                    dest => dest.FileSize,
                    opt => opt.MapFrom(src => (src.File != null && src.File.Length > 0) ? src.File.Length : (int?)null)
                )
                .ReverseMap();

            CreateMap<Group, GroupModel>()
                .ForMember(
                    dest => dest.Image,
                    opt => opt.MapFrom(src => (src.File != null && src.File.Length > 0) ? $"/api/group/image/{src.Id}" : null)
                )
                .ReverseMap();
        }
    }
}
