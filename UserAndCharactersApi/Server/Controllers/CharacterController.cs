using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserWithCharacterVisibility.Dtos;
using UserWithCharacterVisibility.Models;

namespace UserWithCharacterVisibility.Server.Controllers {

  /// <summary>
  /// For accessing user data
  /// </summary>
  [ApiController]
  [Route("api/character")]
  public abstract class CharacterController<TUser, TCharacter> : Controller
    where TCharacter : Character<TUser, TCharacter>
    where TUser : User<TUser, TCharacter>
  {

    readonly UserManager<TUser> _userManager;
    readonly DbContext<TUser, TCharacter> _dbContext;

    protected CharacterController(
      UserManager<TUser> userManager,
      DbContext<TUser, TCharacter> dbContext
    ) {
      _userManager = userManager;
      _dbContext = dbContext;
    }

    [HttpGet("{characterIdentifier}")]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status401Unauthorized)]
    public abstract Task<IActionResult> Get(string characterIdentifier, [System.Web.Http.FromUri] bool reviewAsAdmin = false);

    protected async Task<IActionResult> GetLogic(string characterIdentifier, bool reviewAsAdmin = false) {
      TCharacter foundCharacter;

      // admin review
      if(reviewAsAdmin && User.IsInRole("Admin")) {
        foundCharacter = _dbContext.Characters
          .Where(character => character.Id == characterIdentifier)
          .FirstOrDefault();
        if(foundCharacter == null) {
          foundCharacter = _dbContext.Characters
            .Where(character => character.Id == characterIdentifier)
            .FirstOrDefault();
        }

        return Ok(new SuccessfulReplyDto<TCharacter> {
          Result = foundCharacter,
          Message = "Admin Access To Character Granted."
        });
      } // ==

      if((foundCharacter = await GetCharacterIfLoggedInUserCanSeeThem(characterIdentifier, true)) != null) {
        // If they're whitelist only and they allow whitelist requests:
        if(foundCharacter.VisibilityOptions.Visibility == Visibility.WhitelistOnly) {
          TUser currentUser;
          // whitelist only works for logged in users
          if((currentUser = await this.TryToGetLoggedInUser(_userManager)) != null) {
            // load this user character list
            _dbContext.Entry(currentUser)
              .Collection(u => u.Characters)
              .Load();

            // just correct whitelist already, so return ok
            if(foundCharacter.VisibilityOptions.Whitelist
              .Intersect(currentUser.Characters)
              .Any()
            ) {
              return Ok(new SuccessfulReplyDto<TCharacter> {
                Result = foundCharacter.ObfuscateJsonFor(currentUser),
                Message = "Character Found."
              });
            } // if someone can request:
            else if(foundCharacter.VisibilityOptions.AllowWhitelistRequests) {
              return Unauthorized(new FailureReplyDto {
                Message = "Not Autorized To View User. You Can Request Access Though."
              }); //$"/character/{foundCharacter.UniqueName}/requestAccess
            }
          }
        }
      }

      return NotFound(new FailureReplyDto {
        Message = "Character Not Found."
      });
    }

    [HttpGet("{characterIdentifier}/creator")]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status404NotFound)]
    public abstract Task<IActionResult> GetCreator(string characterIdentifier, [System.Web.Http.FromUri] bool reviewAsAdmin = false);

    protected async Task<IActionResult> GetCreatorLogic(string characterIdentifier, bool reviewAsAdmin = false) {
      throw new NotImplementedException(); //todo: && foundUserCharacter.VisibilityOptions.Visibility != Visibility.Unlisted
    }

    [HttpGet("{characterIdentifier}/watchlist")]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status404NotFound)]
    public abstract Task<IActionResult> GetWatchlist(string characterIdentifier, [System.Web.Http.FromUri] bool reviewAsAdmin = false);

    protected async Task<IActionResult> GetWatchlistLogic(string characterIdentifier, bool reviewAsAdmin = false) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Update the current user's settings.
    /// </summary>
    [HttpPost("{characterIdentifier}/visibilitySettings")]
    public ActionResult UpdateVisibilitySettings(string characterIdentifier, [FromBody] VisibilitySettings<TUser, TCharacter> settings) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Request access to a user page:
    /// </summary>
    [HttpPost("{characterIdentifier}/requestAccessFor/{newCharacter}")]
    public ActionResult RequestAccess(string characterIdentifier, string newCharacter) {
      throw new NotImplementedException();
    }

    [HttpPost("delete")]
    public Task<ActionResult> Delete([FromBody] DeleteCharacterDto deleteCharacterData, [System.Web.Http.FromUri] bool asAdmin = false) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Helper to try to get the given character if it's allowed to be viewed by the logged in user
    /// </summary>
    protected async Task<TCharacter> GetCharacterIfLoggedInUserCanSeeThem(string characterIdentifier, bool ignoreWhitelist = false) {
      string loggedInUserIdentifier;
      bool foundById = false;

      TCharacter foundCharacter = _dbContext.Characters
        .Where(character => character.Id == characterIdentifier)
        .FirstOrDefault();
      if(foundCharacter == null) {
        foundById = true;
        foundCharacter = _dbContext.Characters
          .Where(character => character.Id == characterIdentifier)
          .FirstOrDefault();
      }

      if(foundCharacter == null) {
        return null;
      }

      if((loggedInUserIdentifier = User.FindFirst(ClaimTypes.Name).Value) != null) {
        // current user is the logged in one:
        if(loggedInUserIdentifier == foundCharacter.Creator.UserName) {
          return foundCharacter;
        }

        TUser currentUser = await _userManager.FindByNameAsync(loggedInUserIdentifier);

        // we have a current user and a logged in user
        if(currentUser != null) {
          if(!foundById && foundCharacter.VisibilityOptions.Visibility == Visibility.Obfuscated) {
            return null;
          } 
          else if(foundCharacter.VisibilityOptions.Visibility == Visibility.WhitelistOnly) {
            return ignoreWhitelist
              ? foundCharacter
              : foundCharacter.CheckIfWhitelistAppliesTo(currentUser, _dbContext)
                ? foundCharacter
                : null;
          }
          else if(currentUser.CanSee(foundCharacter, _dbContext)) {
            return foundCharacter;
          }
        }

        return null;
      }
      else if(foundCharacter.VisibilityOptions.Visibility == Visibility.Public 
        || foundCharacter.VisibilityOptions.Visibility == Visibility.Obfuscated
      ) {
        return foundCharacter;
      }

      return null;
    }
  }
}
