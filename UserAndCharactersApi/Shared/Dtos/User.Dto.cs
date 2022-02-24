using System.Text.Json.Serialization;

namespace UserWithCharacterVisibility.Models {

  public partial class User<TUser, TCharacter> where TUser : User<TUser, TCharacter>
    where TCharacter : Character<TUser, TCharacter>
  {
    /// <summary>
    /// The data actually displayed
    /// </summary>
    public class Dto {

      public string Id {
        get;
      }

      [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
      public string ObfuscationId {
        get;
      }

      [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
      public string UserName {
        get;
      }

      [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
      public string Email {
        get;
      }

      public bool IsAdmin {
        get;
      }

      /// <summary>
      /// Make a dto for a non admin viewr.
      /// </summary>
      internal protected Dto(TUser user, TUser viewer) {
        Id = user.Id;
        UserName = user.UserName;

        if(user.Equals(viewer)) {
          Email = user.Email;
          ObfuscationId = user.CurrentObfuscationId;
        } else if (user.VisibilityOptions.Visibility == Visibility.Obfuscated) {
          Id = user.CurrentObfuscationId;
        }

        IsAdmin = user.IsAdmin;
      }

      /// <summary>
      /// Make a dto with all info for an admin.
      /// </summary>
      internal protected Dto(TUser user) {
        Id = user.Id;
        Email = user.Email;
        UserName = user.UserName;
        UserName = user.UserName;
        IsAdmin = user.IsAdmin;
        ObfuscationId = user.CurrentObfuscationId;
      }
    }
  }
}
