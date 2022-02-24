using UserWithCharacterVisibility.Dtos;
using UserWithCharacterVisibility.Models;

namespace UserWithCharacterVisibility.Server {
  public class AutoMapperProfiles : AutoMapper.Profile {
    public AutoMapperProfiles() {
      CreateMap<CreateNewUserDto, User>();
    }
  }
}
