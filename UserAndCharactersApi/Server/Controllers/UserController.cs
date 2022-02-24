using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserWithCharacterVisibility.Dtos;
using UserWithCharacterVisibility.Models;

namespace UserWithCharacterVisibility.Server.Controllers {

  /// <summary>
  /// For accessing user data
  /// </summary>
  [ApiController]
  [Route("api/user")]
  public abstract class UserController<TUser, TCharacter> : Controller 
    where TUser : User<TUser, TCharacter>
    where TCharacter : Character<TUser, TCharacter>
  {

    protected readonly UserManager<TUser> userManager;
    protected readonly DbContext<TUser,TCharacter> dbContext;
    readonly SignInManager<TUser> _signInManager;

    protected UserController(
      UserManager<TUser> userManager,
      SignInManager<TUser> signInManager,
      DbContext<TUser, TCharacter> dbContext
    ) {
      this.userManager = userManager;
      this.dbContext = dbContext;
      _signInManager = signInManager;
    }

    [HttpGet("{userName}")]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status401Unauthorized)]
    public abstract Task<IActionResult> Get(string userName, [System.Web.Http.FromUri] bool reviewAsAdmin = false);

    /// <summary>
    /// For implimentation.
    /// </summary>
    protected async Task<IActionResult> GetLogic(string userName, bool reviewAsAdmin = false) {
    
      // admin review
      if(reviewAsAdmin && User.IsInRole("Admin")) {
        return Ok(new SuccessfulReplyDto<User<TUser, TCharacter>.Dto> {
          Result = ((await userManager.FindByNameAsync(userName))
            ?? (await userManager.FindByIdAsync(userName))).ToDtoForAdmin(),
          Message = "Admin TUser Access Granted"
        });
      } // ==

      TUser foundUser;
      // get the desired user, if the currently logged in user can see them:
      if((foundUser = await GetUserIfLoggedInUserCanSee(userName, true)) != null) {
        // If they're whitelist only and they allow whitelist requests:
        if(foundUser.VisibilityOptions.Visibility == Visibility.WhitelistOnly) {
          TUser currentUser;
          // whitelist only works for logged in users
          if((currentUser = await this.TryToGetLoggedInUser(userManager)) != null) {
            if(foundUser.CheckIfWhitelistAppliesTo(currentUser, dbContext)) {
              return Ok(new SuccessfulReplyDto<User<TUser, TCharacter>.Dto> {
                Message = "TUser Found.",
                Result = foundUser.ToDtoFor(currentUser)
              });
            }
            // if someone can request:
            else if(foundUser.VisibilityOptions.AllowWhitelistRequests) {
              return Unauthorized(new FailureReplyDto {
                Message = "Not Autorized To View TUser. You Can Request Access Though."
              }); //$"/user/{foundUser.UserName}/requestAccess"
            }
          }
        }
      }

      return NotFound(new FailureReplyDto {
        Message = "TUser Not Found."
      });
    }

    /// <summary>
    /// Get the characters that you can see for a given user
    /// </summary>
    [HttpGet("{userName}/characters")]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status404NotFound)]
    public abstract Task<ActionResult> GetCharacters(string userName, [System.Web.Http.FromUri] bool reviewAsAdmin = false);

    /// <summary>
    /// Get the characters that you can see for a given user
    /// </summary>
    protected async Task<(int status, object value)> GetCharactersLogic(string userName, bool reviewAsAdmin = false) {
      // Admin review
      if(reviewAsAdmin && ((await this.TryToGetLoggedInUser(userManager))?.IsAdmin ?? false)) {
        TUser found = (await userManager.FindByNameAsync(userName))
          ?? (await userManager.FindByIdAsync(userName));
        dbContext.Entry(found)
          .Collection(u => u.Characters)
          .Load();
        return (StatusCodes.Status200OK, new SuccessfulReplyDto<IEnumerable<TCharacter>> {
          Message = "Admin Access Granted For Characters For TUser",
          Result = (IEnumerable<TCharacter>)found.Characters
        });
      } // ==

      TUser foundUser;
      if((foundUser = await GetUserIfLoggedInUserCanSee(userName)) != null) {
        dbContext.Entry(foundUser)
          .Collection(u => u.Characters)
          .Load();

        TUser loggedInUser = await this.TryToGetLoggedInUser(userManager);
        if(loggedInUser != null) {
          return (StatusCodes.Status200OK, new SuccessfulReplyDto<IEnumerable<TCharacter>> {
            Message = "Found Characters For TUser",
            Result = foundUser.Characters.Where(
              foundUserCharacter 
                => loggedInUser.CanSee(foundUserCharacter, dbContext)
                  && foundUserCharacter.VisibilityOptions.Visibility != Visibility.Unlisted
            ).Select(character => character.ObfuscateJsonFor(loggedInUser))
          });
        }

        return (StatusCodes.Status200OK, new SuccessfulReplyDto<IEnumerable<TCharacter>> {
          Message = "Found Characters For TUser",
          Result = foundUser.Characters.Where(
            foundUserCharacter 
              => foundUserCharacter.VisibilityOptions.Visibility 
                == Visibility.Public
          ).Select(character => character.ObfuscateJsonFor(loggedInUser))
        });
      }
      else
        return (StatusCodes.Status404NotFound, new FailureReplyDto {
          Message = "TUser Not Found."
        });
    }
    
    /// <summary>
     /// Helper to try to get the given user if it's allowed to be viewed by the logged in user
     /// </summary>
    protected async Task<TUser> GetUserIfLoggedInUserCanSee(string userIdentifier, bool ignoreWhitelist = false) {
      TUser foundUser 
        = await userManager.FindByNameAsync(userIdentifier);
      TUser currentUser 
        = await this.TryToGetLoggedInUser(userManager);

      // if someone is logged in:
      if(currentUser != null) {
        bool foundById = false;
        // try by id if it wasn't a username
        if(foundUser == null) {
          foundUser = await userManager.FindByIdAsync(userIdentifier);
          foundById = true;
        }

        // we have a current user and a logged in user
        if(foundUser != null) {
          if(!foundById && foundUser.VisibilityOptions.Visibility == Visibility.Obfuscated) {
            return null;
          }
          else if(foundUser.VisibilityOptions.Visibility == Visibility.WhitelistOnly) {
            return ignoreWhitelist
              ? foundUser
              : foundUser.CheckIfWhitelistAppliesTo(currentUser, dbContext)
                ? foundUser
                : null;
          }
          else if(currentUser.CanSee(foundUser, dbContext)) {
            return foundUser;
          }
        }

        return null;
      }
      else if((foundUser?.VisibilityOptions.Visibility ?? Visibility.Hidden) == Visibility.Public) {
        return foundUser;
      }
      else if(foundUser == null
        && (foundUser = await userManager.FindByIdAsync(userIdentifier)) != null
        && foundUser.VisibilityOptions.Visibility == Visibility.Obfuscated
      ) {
        return foundUser;
      }

      return null;
    }
  }
}
