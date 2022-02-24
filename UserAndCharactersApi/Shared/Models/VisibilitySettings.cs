using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UserWithCharacterVisibility {

  /// <summary>
  /// The visibility settings
  /// </summary>
  public enum Visibility {
    Hidden, // not visible to anyone but the creator. Redirects. For WIPs usually.
    AdminOnly, // not visible to anyone except admins. Used for testing.
    Public, // can be viewd by anyone, visible in all lists
    Unlisted, // publicly findable by name but doesn't show up in lists
    Obfuscated, // visible to anyone with the randomly generated link
    WhitelistOnly, // visible only to members who own a character in a whitelist, you must add them or approve a user request.
    FollowedOnly, // only visible to people you follow a character of. Redirects for other people.
    MutualsOnly // Visible to mutuals only. Redirects for other people.
  }

  namespace Models {

    /// <summary>
    /// Used to configure a user, character, or list's accessability and visibility to other users.
    /// </summary>
    public class VisibilitySettings<TUser, TCharacter> 
      where TCharacter : Character<TUser, TCharacter> 
      where TUser : User<TUser, TCharacter>
    {

      /// <summary>
      /// Database Id
      /// </summary>
      [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      [JsonIgnore]
      public int Id {
        get; 
        private set;
      }

      /// <summary>
      /// The overall visibility setting
      /// </summary>
      public Visibility Visibility { 
        get;
        private set;
      }

      /// <summary>
      /// Allow other users to request access to this item's whitelist
      /// </summary>
      public bool AllowWhitelistRequests {
        get;
        private set;
      }

      /// <summary>
      /// Any whitelisted characters
      /// </summary>
      public virtual List<TCharacter> Whitelist {
        get;
        private set;
      } = null;

      /// <summary>
      /// Any blacklisted characters
      /// </summary>
      public virtual List<TCharacter> Blacklist {
        get;
        private set;
      } = null;
    }
  }
}