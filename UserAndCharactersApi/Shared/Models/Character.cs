using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;

namespace UserWithCharacterVisibility.Models {

  /// <summary>
  /// A character made by a user
  /// </summary>
  public partial class Character<TUser, TCharacter> 
    where TUser : User<TUser, TCharacter>
    where TCharacter : Character<TUser, TCharacter>
  {

    /// <summary>
    /// Unique Id
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Id {
      get;
      private set;
    }

    /// <summary>
    /// The ID used as a url if obfuscation is on for this User.
    /// </summary>
    [JsonIgnore]
    public string CurrentObfuscationId {
      get;
      private set;
    } = Guid.NewGuid().ToString();

    /// <summary>
    /// The unique name of the character. Used for non obfuscated urls
    /// </summary>
    public string UniqueName {
      get;
      private set;
    }

    /// <summary>
    /// The user-set display name of the character
    /// </summary>
    [Column("displayName")]
    public string DefaultDisplayName {
      get;
      private set;
    }

    /// <summary>
    /// The list of characters this character is watching/following.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<TCharacter> WatchList
      => _watchList;
    List<TCharacter> _watchList
      = new();

    /// <summary>
    /// visibility settings
    /// </summary>
    [JsonIgnore]
    public virtual VisibilitySettings<TUser, TCharacter> VisibilityOptions {
      get;
      private set;
    } = new ();

    /// <summary>
    /// The user who created this character
    /// </summary>
    [JsonIgnore]
    public virtual TUser Creator {
      get;
      private set;
    }

    /// <summary>
    /// For making a new character.
    /// </summary>
    protected Character(TUser creator, string uniqueName, string displayName = null, VisibilitySettings<TUser, TCharacter> visibilityOptions = null) {
      Creator = creator;
      UniqueName = uniqueName;
      DefaultDisplayName = displayName ?? UniqueName;
      VisibilityOptions = visibilityOptions ?? new VisibilitySettings<TUser, TCharacter>();
    } 

    /// <summary>
    /// Obfuscate this character for the given user viewing and return it via json.
    /// </summary>
    public virtual TCharacter ObfuscateJsonFor(TUser viewer) {
      if(!viewer.Equals(Creator)) {
        if(VisibilityOptions.Visibility == Visibility.Obfuscated) {
          Id = CurrentObfuscationId;
          UniqueName = null;
        } 
        else if(!((VisibilityOptions.Visibility == Visibility.Public)
          || (VisibilityOptions.Visibility == Visibility.FollowedOnly)
          || (VisibilityOptions.Visibility == Visibility.MutualsOnly)
        )) {
          Id = null;
        }
      }

      return (TCharacter)this;
    }

    /// <summary>
    /// Check if this character's whitleist applies to a given user.
    /// </summary>
    public bool CheckIfWhitelistAppliesTo(TUser user, DbContext dbContext) {
      // preserves permission loaded characters
      List<TCharacter> cachedCharacters = null;

      // load this user character list
      if(!dbContext.Entry(user).Collection(u => u.Characters).IsLoaded) {
        cachedCharacters = user._characters;
        dbContext.Entry(user)
        .Collection(u => u.Characters)
        .Load();
      }

      // just correct whitelist already, so return ok
      if(VisibilityOptions.Whitelist
        .Intersect(user.Characters)
        .Any()
      ) {
        user._characters = cachedCharacters ?? user._characters;
        return true;
      }

      user._characters = cachedCharacters ?? user._characters;
      return false;
    }
  }
}