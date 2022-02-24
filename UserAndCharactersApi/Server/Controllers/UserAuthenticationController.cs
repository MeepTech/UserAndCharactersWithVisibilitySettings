using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using UserWithCharacterVisibility.Server.Services;
using UserWithCharacterVisibility.Dtos;
using UserWithCharacterVisibility.Models;
using System.Linq;
using System.Text.Json;

namespace UserWithCharacterVisibility.Server.Controllers {

  /// <summary>
  /// For logging in/ out/ and registering new users
  /// </summary>
  [ApiController]
  [Route("api/user")]
  public abstract class UserAuthenticationController<TUser, TCharacter> : Controller
    where TUser : User<TUser, TCharacter>
    where TCharacter : Character<TUser, TCharacter>
  {

    readonly SignInManager<TUser> _signInManager;
    readonly UserManager<TUser> _userManager;
    readonly IMapper _mapper;
    readonly IEmailSender _emailSender;

    public UserAuthenticationController(
      IMapper mapper,
      UserManager<TUser> userManager,
      SignInManager<TUser> signInManager,
      IEmailSender emailSender
    ) {
      _mapper = mapper;
      _userManager = userManager;
      _signInManager = signInManager;
      _emailSender = emailSender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SuccessfulReplyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Register([FromBody] CreateNewUserDto newUserData) {
      if(ModelState.IsValid) {
        TUser newUser = _mapper.Map<TUser>(newUserData);
        IdentityResult results 
          = await _userManager.CreateAsync(newUser, newUserData.Password);

        if(results.Succeeded) {
          await _sendUserRegistrationEmail(newUser);
          return Ok(new SuccessfulReplyDto {
            Message = "New User Created, Please Confirm Your Email"
          });
        }
        else {
          foreach(var error in results.Errors) {
            ModelState.AddModelError(string.Empty, error.Description);
          }
        }
      }

      return StatusCode(StatusCodes.Status401Unauthorized, ModelState
        .GetFailureReplyDto("Invalid Model State")
      );
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(SuccessfulReplyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout() {
      if(User.Identity.IsAuthenticated) {
        await _signInManager.SignOutAsync();
        return Ok(new SuccessfulReplyDto {
          Message = "Logged Out Successfully"
        });
      }

      return StatusCode(StatusCodes.Status401Unauthorized, new FailureReplyDto {
        Message = "Not Logged In"
      });
    }

    [HttpPost("login")]
    //[ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(SuccessfulReplyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Login([FromBody] UserLoginDto loginData) {
      if(ModelState.IsValid) {
        TUser user = await _userManager.FindByNameAsync(loginData.Username);
        if(user == null) {
          return StatusCode(StatusCodes.Status404NotFound, new FailureReplyDto {
            Message = "Invalid Username"
          });
        }
        var results = await _signInManager.PasswordSignInAsync(user, loginData.Password, true, false);
        if(results.Succeeded) {
          return Ok(new SuccessfulReplyDto<string> {
            Message = $"Successfully Logged In As {user.UserName}",
            Result = user.UserName
          });
        }
        else return StatusCode(StatusCodes.Status401Unauthorized, new FailureReplyDto {
          Message= "Could Not Log User In. Login Results:" + JsonSerializer.Serialize(results)
        });
      }

      return StatusCode(StatusCodes.Status401Unauthorized, ModelState.GetFailureReplyDto(
        "Invalid Model State"
      ));
    }

    [HttpGet("resend-confirmation/{email}")]
    public async Task<ActionResult> ResendEmailConfirmation(string email) {
      if(ModelState.IsValid) {
        TUser user = await _userManager.FindByEmailAsync(email);
        if(user != null) {
          if(!user.EmailConfirmed) {
            try {
              await _sendUserRegistrationEmail(user);
            } catch(Exception ex) {
              ModelState.TryAddModelException(nameof(email), ex);
            }
          }
        }
      }

      return StatusCode(StatusCodes.Status417ExpectationFailed, ModelState.GetFailureReplyDto(
        "Could Not Send Email Validation"
      ));
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(SuccessfulReplyDto<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(FailureReplyDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ConfirmEmail([System.Web.Http.FromUri] string userId, [System.Web.Http.FromUri] string token) {
      if(ModelState.IsValid) {
        TUser user 
          = await _userManager.FindByIdAsync(userId);

        if(user is null) {
          return StatusCode(StatusCodes.Status404NotFound, new FailureReplyDto {
            Message = "Invalid TUser Id"
          });
        }

        IdentityResult result 
          = await _userManager.ConfirmEmailAsync(
            user,
            Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token))
          );

        if(result.Succeeded) {
          await _signInManager.SignInAsync(user, true);
          return Ok(new SuccessfulReplyDto<string> {
            Message = "Email Address Confirmed",
            Result = user.Id
          });
        }
        else {
          return StatusCode(StatusCodes.Status401Unauthorized, new FailureReplyDto {
            Message = "Invalid Email Confirmation Token"
          });
        }
      }

      return StatusCode(StatusCodes.Status401Unauthorized, ModelState.GetFailureReplyDto(
        "Invalid Model State"
      ));
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public ActionResult Delete([FromBody] DeleteUserDto deleteData, [System.Web.Http.FromUri] bool asAdmin = false) {
      throw new NotImplementedException();
    }

    [HttpPost("/test/{email}/{subject}/{message}")]
    public void TestSendEmail(string email, string subject, string message) {
      _emailSender.SendEmailAsync(email, subject, message);
    }

    /// <summary>
    /// Update the current user's settings.
    /// </summary>
    [HttpPost("{userName}/visibilitySettings")]
    public ActionResult UpdateVisibilitySettings(string userName, [FromBody] VisibilitySettings<TUser, TCharacter> settings) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Request access to a user page:
    /// </summary>
    [HttpPost("{userName}/requestAccessFor/{characterIdentifier}")]
    public ActionResult RequestAccess(string userName, string characterIdentifier) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Send a user registration validation email.
    /// </summary>
    async Task _sendUserRegistrationEmail(TUser user) {
      // generate the email validation token
      string validationToken 
        = await _userManager.GenerateEmailConfirmationTokenAsync(user);
      validationToken 
        = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(validationToken));
      string validationUrl
        = Extensions.ConfirmEmailPageAddress
        + "?userId=" + user.Id
        + "&token=" + validationToken;

      await _emailSender.SendEmailAsync(
        user.Email,
        "Confirm your email",
        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(validationUrl)}'>clicking here</a>."
      );
    }
  }
}
