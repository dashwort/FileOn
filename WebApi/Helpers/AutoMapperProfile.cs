namespace WebApi.Helpers;

using AutoMapper;
using WebApi.Entities;
using WebApi.Models.FFiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // CreateRequest -> User
        CreateMap<CreateRequest, FFile>();

        // UpdateRequest -> User
        CreateMap<UpdateRequest, FFile>()
            .ForAllMembers(x => x.Condition(
                (src, dest, prop) =>
                {
                    // ignore both null & empty string properties
                    if (prop == null) return false;
                    if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

                    return true;
                }
            ));
    }
}