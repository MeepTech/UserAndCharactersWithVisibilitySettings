// See https://aka.ms/new-console-template for more information
using SendGrid;
using SendGrid.Helpers.Mail;

var apiKey = "PUT_KEY_HERE";
var client = new SendGridClient(apiKey);
var from = new EmailAddress("PUT_SENDER_EMAIL_HERE", "Example User");
var subject = "Sending with SendGrid is Fun";
var to = new EmailAddress("test@example.com", "Example User");
var plainTextContent = "and easy to do anywhere, even with C#";
var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
var response = await client.SendEmailAsync(msg);
var ret  = response.StatusCode; 
Console.WriteLine(ret);