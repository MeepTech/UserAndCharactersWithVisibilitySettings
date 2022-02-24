using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace UserWithCharacterVisibility.Models {

  /// <summary>
  ///  A user of the site.
  ///  Generic base class for access
  /// </summary>
  public abstract class User 
    : IdentityUser {}

  /// <summary>
  ///  A user of the site
  /// </summary>
  public partial class User<TUser, TCharacter> : User
    where TUser : User<TUser, TCharacter>
    where TCharacter : Character<TUser, TCharacter>
  {

    /// <summary>
    /// If this user is an admin
    /// </summary>
    public virtual bool IsAdmin {
      get;
      private set;
    } = false;

    /// <summary>
    /// visibility settings
    /// </summary>
    [JsonIgnore]
    public virtual VisibilitySettings<TUser, TCharacter> VisibilityOptions {
      get;
      private set;
    } = new();

    /// <summary>
    /// This user's characters.
    /// If you're logged in you can see your own private ones, if not you can only see ones that match the right visibility settings
    /// </summary>
    public IEnumerable<TCharacter> Characters
      => _characters; internal List<TCharacter> _characters
        = new();

    /// <summary>
    /// The ID used as a url if obfuscation is on for this User.
    /// </summary>
    [JsonIgnore]
    public string CurrentObfuscationId {
      get;
      private set;
    } = null;

    /// <summary>
    /// Conver this to a dto
    /// </summary>
    public virtual Dto ToDtoFor(TUser viewer)
      => new((TUser)this, viewer);

    /// <summary>
    /// Conver this to a dto
    /// </summary>
    public virtual Dto ToDtoForAdmin()
      => new((TUser)this);

    /// <summary>
    /// Check if this user can see this other user.
    /// </summary>
    public bool CanSee(TUser otherUser, DbContext dbContext) {
      // preserves permission loaded characters
      List<TCharacter> cachedCharacters = null;

      if(!dbContext.Entry(this).Collection(u => u.Characters).IsLoaded) {
        cachedCharacters = _characters;
        // load this user character list
        dbContext.Entry(this)
        .Collection(u => u.Characters)
        .Load();
      }

      // blacklist check
      if(otherUser.VisibilityOptions.Blacklist
        .Intersect(Characters)
        .Any()
      ) {
        _characters = cachedCharacters ?? _characters;
        return false;
      }

      ///Return a result based on visibility
      bool @return = false;
      switch(otherUser.VisibilityOptions.Visibility) {
        case Visibility.Hidden:
          @return = false;
          break;

        case Visibility.AdminOnly:
          @return = IsAdmin;
          break;

        case Visibility.Public:
        case Visibility.Unlisted:
        case Visibility.Obfuscated:
          @return = true;
          break;

        case Visibility.WhitelistOnly:
          @return = otherUser.CheckIfWhitelistAppliesTo((TUser)this, dbContext);
          break;

        case Visibility.FollowedOnly:
          dbContext.Entry(otherUser)
            .Collection(u => u.Characters)
            .Load();
          otherUser.Characters.ToList().ForEach(
            c => dbContext.Entry(c)
              .Collection(c => c.WatchList)
            );
          @return = otherUser.Characters
            .SelectMany(c => c.WatchList)
            .Intersect(Characters)
            .Any();
          break;

        case Visibility.MutualsOnly:
          dbContext.Entry(otherUser)
            .Collection(u => u.Characters)
            .Load();
          otherUser.Characters.ToList().ForEach(
            c => dbContext.Entry(c)
              .Collection(c => c.WatchList)
            );
          // mutuals check:
          foreach(TCharacter otherUserUserCharacter in otherUser.Characters) {
            if(otherUserUserCharacter.WatchList.Any(characterWatchedByUserCharacter => {
              TCharacter thisUserCharacter;
              if((thisUserCharacter = Characters
                .FirstOrDefault(curentUserChar => curentUserChar.UniqueName
                  .Equals(characterWatchedByUserCharacter.UniqueName)
              )) != null) {
                dbContext.Entry(thisUserCharacter)
                  .Collection(c => c.WatchList);
                if(thisUserCharacter.WatchList.Contains(otherUserUserCharacter)) {
                  return true;
                }
              }

              return false;
            })) {
              @return = true;
              break;
            }
          }

          @return = false;
          break;

        default:
          @return = false;
          break;
      }

      _characters = cachedCharacters ?? _characters;
      return @return;
    }

    /// <summary>
    /// Check if this user can see this character
    /// </summary>
    public bool CanSee(TCharacter character, DbContext dbContext) {
      // preserves permission loaded characters
      List<TCharacter> cachedCharacters = null;

      if(!dbContext.Entry(this).Collection(u => u.Characters).IsLoaded) {
        cachedCharacters = _characters;
        dbContext.Entry(this)
          .Collection(u => u.Characters)
          .Load();
      }
      // blacklist check
      if(character.VisibilityOptions.Blacklist
        .Intersect(Characters)
        .Any()
      ) {
        _characters = cachedCharacters ?? _characters;
        return false;
      }

      ///@return =  a result based on visibility
      bool @return = false;
      switch(character.VisibilityOptions.Visibility) {
        case Visibility.Hidden:
          @return =  false;
          break;

        case Visibility.AdminOnly:
          @return = IsAdmin;
          break;

        case Visibility.Public:
        case Visibility.Unlisted:
        case Visibility.Obfuscated:
          @return =  true;
          break;

        case Visibility.WhitelistOnly:
          @return = character.CheckIfWhitelistAppliesTo((TUser)this, dbContext);
          break;

        case Visibility.FollowedOnly:
          dbContext.Entry(character)
              .Collection(c => c.WatchList);
          @return =  character
            .WatchList
            .Intersect(Characters)
            .Any();
          break;

        case Visibility.MutualsOnly:
          dbContext.Entry(character)
              .Collection(c => c.WatchList);
          // mutuals check:
          if(character.WatchList.Any(characterWatchedByUserCharacter => {
            TCharacter thisUserCharacter;
            if((thisUserCharacter = Characters
              .FirstOrDefault(curentUserChar => curentUserChar.UniqueName
                .Equals(characterWatchedByUserCharacter.UniqueName)
            )) != null) {
              dbContext.Entry(thisUserCharacter)
                .Collection(c => c.WatchList);
              if(thisUserCharacter.WatchList.Contains(character)) {
                return true;
              }
            }

            return false;
          })) {
            @return =  true;
            break;
          }

          @return = false;
          break;
        default:
          @return =  false;
          break;
      }

      _characters = cachedCharacters ?? _characters;
      return @return;
    }

    /// <summary>
    /// Check this user's whitelist to see if another user applies.
    /// </summary>
    public bool CheckIfWhitelistAppliesTo(TUser otherUser, DbContext dbContext) {
      // preserves permission loaded characters
      List<TCharacter> cachedCharacters = null;

      // load this user character list
      if(!dbContext.Entry(otherUser).Collection(u => u.Characters).IsLoaded) {
        cachedCharacters = otherUser._characters;
        dbContext.Entry(otherUser)
          .Collection(u => u.Characters)
          .Load();
      }

      // just correct whitelist already, so return ok
      if(VisibilityOptions.Whitelist
        .Intersect(otherUser.Characters)
        .Any()
      ) {
        otherUser._characters = cachedCharacters ?? otherUser._characters;
        return true;
      }

      otherUser._characters = cachedCharacters ?? otherUser._characters;
      return false;
    }
  }
}
