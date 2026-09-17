using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EBill.Services
{
    // Sends transactional email (currently just password-reset links) via a plain
    // SMTP relay configured under the "Smtp" section in appsettings.json.
    // TODO: the Smtp:* values shipped in appsettings.json are placeholders - fill
    // in a real Host/Username/Password (e.g. a Gmail app password, or your hosting
    // provider's SMTP relay) before this can actually deliver mail.
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

         public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException(
                    "Smtp:Host is not configured — set the Smtp section in appsettings.json before email can actually be delivered.");
            }

            var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
            var enableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"];
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = username;
            }
            var fromName = _configuration["Smtp:FromName"] ?? "Stock Management";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            // TEMPORARY — the shared host's mail server presents a certificate for
            // *.webhostbox.net rather than Smtp:Host's own name, so hostname
            // validation fails by default. This accepts any certificate, which
            // gets mail flowing but also means a man-in-the-middle could silently
            // intercept the SMTP credentials above. Must be set BEFORE
            // ConnectAsync — the TLS handshake (and cert check) happens during
            // Connect, so setting it after has no effect (this bit the emailsetup
            // test app first). Replace with the host's actual mail-server hostname
            // in Smtp:Host once you have it from the hosting panel, then remove
            // this callback so real certificate validation applies again.
            client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            // Auto picks implicit SSL (port 465) vs. STARTTLS (port 587) correctly
            // based on the port, so Smtp:Port alone decides which handshake is used.
            var secureSocketOptions = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;

            await client.ConnectAsync(host, port, secureSocketOptions);

            if (!string.IsNullOrEmpty(username))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
