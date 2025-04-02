using System.Net;
using System.Net.Mail;

namespace TaskTrackPro.Services;

public class EmailService
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _fromEmail = "ronyvdhimmar.work@gmail.com"; // Replace with your email
    private readonly string _password = "hlch ojbr kfzy oybi"; // Replace with your app password

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Welcome to TaskTrackPro!";
        var body = GetWelcomeEmailTemplate(userName);

        using var message = new MailMessage(_fromEmail, toEmail, subject, body)
        {
            IsBodyHtml = true
        };

        using var client = new SmtpClient(_smtpServer, _smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_fromEmail, _password)
        };

        await client.SendMailAsync(message);
    }

    private string GetWelcomeEmailTemplate(string userName)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f9f9f9;'>
                <div style='background: linear-gradient(135deg, #0061f2 0%, #00a6f2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: #ffffff; margin: 0; font-size: 28px; text-shadow: 2px 2px 4px rgba(0,0,0,0.2);'>Welcome to TaskTrackPro!</h1>
                </div>
                <div style='background-color: #ffffff; padding: 40px; border-radius: 0 0 10px 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                    <h2 style='color: #333333; margin-bottom: 20px;'>Hello {userName}! 👋</h2>
                    <p style='color: #555555; font-size: 16px; line-height: 1.6;'>Thank you for joining TaskTrackPro. We're thrilled to have you as part of our community!</p>

                    <div style='background-color: #f8f9fa; border-left: 4px solid #0061f2; padding: 20px; margin: 25px 0; border-radius: 4px;'>
                        <p style='color: #333333; font-weight: bold; margin-bottom: 15px;'>With TaskTrackPro, you'll be able to:</p>
                        <ul style='color: #555555; padding-left: 20px;'>
                            <li style='margin-bottom: 10px;'>📋 Manage your tasks efficiently</li>
                            <li style='margin-bottom: 10px;'>📈 Track your progress in real-time</li>
                            <li style='margin-bottom: 10px;'>👥 Collaborate seamlessly with your team</li>
                            <li style='margin-bottom: 10px;'>🎯 Achieve your goals faster</li>
                        </ul>
                    </div>

                    <div style='text-align: center; margin: 35px 0;'>
                        <a href='http://localhost:5089/Auth/Login' style='background: linear-gradient(135deg, #0061f2 0%, #00a6f2 100%); color: white; padding: 14px 30px; text-decoration: none; border-radius: 25px; font-weight: bold; display: inline-block; box-shadow: 0 4px 6px rgba(0,97,242,0.2); transition: transform 0.2s;'>Login to Your Account</a>
                    </div>

                    <p style='color: #555555; margin-top: 30px;'>Need help getting started? Our support team is here for you 24/7!</p>

                    <div style='margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee;'>
                        <p style='color: #555555; margin-bottom: 5px;'>Best regards,</p>
                        <p style='color: #333333; font-weight: bold; margin-top: 0;'>The TaskTrackPro Team</p>
                    </div>
                </div>
                <div style='text-align: center; padding: 20px;'>
                    <p style='color: #6c757d; font-size: 14px;'>
                        © 2024 TaskTrackPro. All rights reserved.<br>
                        <a href='#' style='color: #0061f2; text-decoration: none;'>Privacy Policy</a> • 
                        <a href='#' style='color: #0061f2; text-decoration: none;'>Terms of Service</a>
                    </p>
                </div>
            </body>
            </html>";
    }
}