using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using UserWithCharacterVisibility.Server.Configuration;

namespace UserWithCharacterVisibility.Server {
  namespace Services {

    public interface IEmailSender {
      Task SendEmailAsync(string email, string subject, string message);
    }

    public class SendGridEmailSender : IEmailSender {

      public SendGridEmailSenderOptions Options {
        get; set;
      }

      SendGridClient _client;

      public SendGridEmailSender(
        IOptions<SendGridEmailSenderOptions> options
      ) {
        Options = options.Value;
      }

      public async Task SendEmailAsync(
        string email,
        string subject,
        string message
      ) {
        await Execute(Options.ApiKey, subject, message, email);
      }

      async Task<Response> Execute(
            string apiKey,
            string subject,
            string message,
            string email
      ) {
        _client ??= new SendGridClient(apiKey);
        SendGridMessage msg = new() {
          From = new EmailAddress(
            Options.SenderEmail,
            Options.SenderName
          ),
          Subject = subject,
          PlainTextContent = message,
          HtmlContent = message
        };
        msg.AddTo(new EmailAddress(email));

        // disable tracking settings
        // ref.: https://sendgrid.com/docs/User_Guide/Settings/tracking.html
        msg.SetClickTracking(false, false);
        msg.SetOpenTracking(false);
        msg.SetGoogleAnalytics(false);
        msg.SetSubscriptionTracking(false);

        return await _client.SendEmailAsync(msg);
      }
    }
  }


  namespace Configuration {
    public class SendGridEmailSenderOptions {
      public string ApiKey {
        get;
        set;
      }

      public string SenderEmail {
        get;
        set;
      }

      public string SenderName {
        get;
        set;
      }
    }
  }
}
