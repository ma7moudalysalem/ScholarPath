using System.Reflection;
using AutoMapper;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Explicit mappings
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
            .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.ProfileImageUrl));

        // Convention-based mappings (for DTOs implementing IMapFrom<T>)
        ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private void ApplyMappingsFromAssembly(Assembly assembly)
    {
        var mapFromType = typeof(IMapFrom<>);

        const string mappingMethodName = nameof(IMapFrom<object>.Mapping);

        var types = assembly.GetExportedTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == mapFromType))
            .ToList();

        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod(mappingMethodName)
                ?? type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == mapFromType)
                    .Select(i => i.GetMethod(mappingMethodName))
                    .FirstOrDefault();

            methodInfo?.Invoke(instance, new object[] { this });
        }
    }
}

public interface IMapFrom<T>
{
    void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType());
}
