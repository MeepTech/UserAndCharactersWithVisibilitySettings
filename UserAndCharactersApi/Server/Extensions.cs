using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using UserWithCharacterVisibility.Models;
using UserWithCharacterVisibility.Server.Configuration;
using UserWithCharacterVisibility.Server.Services;

namespace UserWithCharacterVisibility.Server {
  public static class Extensions {

    /// <summary>
    /// The confirm emailpage address to use. Can also be used as the default end stub
    /// </summary>
    public static string ConfirmEmailPageAddress {
      get;
      internal set;
    } = "/user/confirm-email";

    public static Dtos.FailureReplyDto GetFailureReplyDto(this ModelStateDictionary modelState, string message)
      => new() {
        Message = message,
        Errors = modelState.Select(x => x.Value.Errors)
          .Where(y => y.Count > 0)
      };

    public static async Task<TUser> TryToGetLoggedInUser<TUser>(this Controller controller, UserManager<TUser> userManager) where TUser : User {
      if(controller.User.Identity.IsAuthenticated) {
        return await userManager.FindByIdAsync(
          userManager.GetUserId(controller.User)
        );
      }
      else
        return null;
    }

    public static IServiceCollection AddUserWithCharacterVisibilityConfigurations<TStartup, TDbContext, TUser, TCharacter>(
      this IServiceCollection services, 
      IConfiguration configuration,
      string emailValidationUrl,
      Action<DbContextOptionsBuilder> dbContextOptionsConfig = null,
      Action<IdentityOptions> identityOptionsConfig = null,
      Action<SendGridEmailSenderOptions> sendGridOptionsConfig = null
    ) 
      where TDbContext : DbContext<TUser, TCharacter>
      where TUser : User<TUser, TCharacter>
      where TCharacter : Character<TUser, TCharacter>
    {
      ConfirmEmailPageAddress = emailValidationUrl;
      services.AddTransient<IEmailSender, SendGridEmailSender>();
      services.Configure<SendGridEmailSenderOptions>(options => {
        options.ApiKey = configuration["SendGrid:ApiKey"];
        options.SenderEmail = configuration["SendGrid:SenderEmail"];
        options.SenderName = configuration["SendGrid:SenderName"];
        sendGridOptionsConfig?.Invoke(options);
      });
      services.AddAutoMapper(typeof(TStartup));
      services
        .AddDbContext<TDbContext>(options => {
          dbContextOptionsConfig?.Invoke(options);
        });
      services.
        AddIdentity<TUser, IdentityRole>(options => {
          options.SignIn.RequireConfirmedAccount = true;
          identityOptionsConfig?.Invoke(options);
        }).AddClaimsPrincipalFactory<UserClaimsPrincipalFactory<TUser, IdentityRole>>()
        .AddEntityFrameworkStores<TDbContext>()
        .AddDefaultTokenProviders();
      return services;
    }
  }
}
