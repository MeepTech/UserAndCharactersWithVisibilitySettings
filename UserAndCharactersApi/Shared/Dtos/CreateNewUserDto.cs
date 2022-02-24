using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserWithCharacterVisibility.Dtos {

  /// <summary>
  /// DTO for creating a new user
  /// </summary>
  public record CreateNewUserDto : UsernamePasswordDto {
      public string Email { get; init; }
  };
}
