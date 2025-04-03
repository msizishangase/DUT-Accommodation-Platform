using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using DUT_Accommodation_Platform.Models;
using System.Configuration;
using System.Net.Mail;
using System.Net;

namespace DUT_Accommodation_Platform.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Convert to Identity 2.0 syntax for .NET 4.8
            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await SignInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    shouldLockout: false);

                switch (result)
                {
                    case SignInStatus.Success:
                        return RRedirectToLocal(returnUrl);
                    case SignInStatus.LockedOut:
                        return View("Lockout");
                    case SignInStatus.RequiresVerification:
                        return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    case SignInStatus.Failure:
                    default:
                        ModelState.AddModelError("", "Invalid login attempt.");
                        return View(model);
                }
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        private ActionResult RRedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult RegistrationSuccess(string userType, string email)
        {
            // Determine the appropriate dashboard URL
            var redirectUrl = userType == "Student"
                ? Url.Action("Dashboard", "Student")
                : Url.Action("Dashboard", "Landlord");

            var viewModel = new RegistrationSuccessViewModel
            {
                UserType = userType,
                Email = email,
                RedirectUrl = redirectUrl
            };

            return View(viewModel);
        }

        [AllowAnonymous]
        public ActionResult RegisterStudent()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegisterStudent(StudentRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var studentNumber = model.Email.Split('@')[0];

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    StudentNumber = studentNumber,
                    AccountType = "Student",
                    IsStudentVerified = false
                };

                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await UserManager.AddToRoleAsync(user.Id, "Student");

                    Task.Run(() => SendStudentWelcomeEmail(user));
                    Task.Run(() => SendAdminVerificationRequest(user));

                    return RedirectToAction("RegistrationSuccess", new { 
                        userType = "Student",
                        email = user.Email
                    });
                }
                AddErrors(result);
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult RegisterLandlord()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegisterLandlord(LandlordRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    ContactNumber = model.PhoneNumber,
                    CompanyName = model.CompanyName, // Add this line
                    AccountType = "Landlord",
                    IsLandlordApproved = false,
                    RegistrationDate = DateTime.Now
                };

                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await UserManager.AddToRoleAsync(user.Id, "Landlord");
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    Task.Run(() => SendLandlordWelcomeEmail(user));
                    Task.Run(() => SendAdminLandlordVerificationRequest(user));

                    return RedirectToAction("RegistrationSuccess", new
                    {
                        userType = "Landlord",
                        email = user.Email
                    });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private async Task SendStudentWelcomeEmail(ApplicationUser student)
        {
            try
            {
                var loginUrl = "https://dutcribs.azurewebsites.net/";
                var supportEmail = ConfigurationManager.AppSettings["SupportEmail"] ?? "shangasemsizi469@gmail.com";
                var logoUrl = AzureStorageHelper.GetLogoUrlWithSas();

                // Modern HTML template with responsive design
                var emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ text-align: center; padding: 20px 0; border-bottom: 1px solid #e2e8f0; }}
                        .logo {{ max-width: 180px; height: auto; }}
                        .content {{ padding: 25px 0; }}
                        .details-box {{ background: #f8fafc; border-radius: 8px; padding: 15px; margin: 20px 0; }}
                        .button {{ 
                            display: inline-block; padding: 12px 24px; background-color: #2b6cb0; 
                            color: white !important; text-decoration: none; border-radius: 6px; 
                            font-weight: 600; margin: 15px 0; 
                        }}
                        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e2e8f0; color: #718096; font-size: 14px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <img src='{loginUrl}' alt='DUT Accommodation' class='logo'>
                    </div>
            
                    <div class='content'>
                        <h2 style='color: #2d3748; margin-top: 0;'>Welcome, {student.FullName}!</h2>
                        <p>Your student accommodation account has been successfully created.</p>
                
                        <div class='details-box'>
                            <p><strong>Account Details:</strong></p>
                            <ul style='margin: 10px 0; padding-left: 20px;'>
                                <li>Student Number: <strong>{student.StudentNumber}</strong></li>
                                <li>Email: <strong>{student.Email}</strong></li>
                            </ul>
                        </div>
                
                        <p>Your account is pending verification. You'll receive another notification once approved.</p>
                
                        <a href='{loginUrl}' class='button'>ACCESS YOUR ACCOUNT</a>
                
                        <p style='font-size: 15px;'>
                            Need help? Contact our support team at 
                            <a href='mailto:{supportEmail}'>{supportEmail}</a>
                        </p>
                    </div>
            
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Durban University of Technology Accommodation Portal</p>
                        <p>
                            <a href='https://yourdomain.com/privacy' style='color: #718096;'>Privacy Policy</a> | 
                            <a href='https://yourdomain.com/terms' style='color: #718096;'>Terms of Service</a>
                        </p>
                    </div>
                </body>
                </html>";

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = ConfigurationManager.AppSettings["SmtpHost"];
                    smtpClient.Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(
                        ConfigurationManager.AppSettings["EmailFromAddress"],
                        ConfigurationManager.AppSettings["GmailAppPassword"]);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(
                            ConfigurationManager.AppSettings["EmailFromAddress"],
                            ConfigurationManager.AppSettings["EmailFromName"]),
                        Subject = "Welcome to DUT Accommodation Portal",
                        Body = emailBody,
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };
                    mailMessage.To.Add(student.Email);

                    // Add CC to admin if needed
                    if (bool.Parse(ConfigurationManager.AppSettings["CcAdminOnWelcomeEmails"] ?? "false"))
                    {
                        mailMessage.CC.Add(ConfigurationManager.AppSettings["AdminEmail"]);
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Welcome email failed for {student.Email}: {ex.Message}");
                // Consider adding a retry queue for failed emails
            }
        }

        private async Task SendAdminVerificationRequest(ApplicationUser student)
        {
            try
            {
                var adminEmail = ConfigurationManager.AppSettings["AdminEmail"] ?? "shangasemsizi469@gmail.com";
                var verifyUrl = Url.Action("VerifyStudent", "Admin",
                    new { id = student.Id }, Request.Url.Scheme);
                var logoUrl = AzureStorageHelper.GetLogoUrlWithSas();

                // Modern HTML template
                var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='text-align: center; padding: 20px 0; border-bottom: 1px solid #eee;'>
                        <img src='{logoUrl}' alt='DUT Accommodation' style='max-width: 150px;'>
                    </div>
            
                    <div style='padding: 20px 0;'>
                        <h2 style='color: #2d3748;'>New Student Verification Required</h2>
                
                        <div style='background: #f8f9fa; padding: 15px; border-radius: 4px; margin: 15px 0;'>
                            <p><strong>Name:</strong> {student.FullName}</p>
                            <p><strong>Student No:</strong> {student.StudentNumber}</p>
                            <p><strong>Email:</strong> {student.Email}</p>
                        </div>
                
                        <a href='{verifyUrl}' 
                           style='display: inline-block; padding: 12px 24px; background-color: #2b6cb0; 
                                  color: white; text-decoration: none; border-radius: 4px; font-weight: bold;'>
                            APPROVE STUDENT
                        </a>
                
                        <p style='font-size: 14px; color: #718096; margin-top: 20px;'>
                            This request will expire in 48 hours. If you didn't expect this notification, 
                            please review your account security.
                        </p>
                    </div>
            
                    <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #718096;'>
                        <p>© {DateTime.Now.Year} DUT Accommodation System</p>
                        <p>
                            <a href='https://dutcribs.azurewebsites.net/admin' style='color: #718096;'>Admin Portal</a> | 
                            <a href='https://dutcribs.azurewebsites.net/privacy' style='color: #718096;'>Privacy Policy</a>
                        </p>
                    </div>
                </div>";

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = ConfigurationManager.AppSettings["SmtpHost"];
                    smtpClient.Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(
                        ConfigurationManager.AppSettings["EmailFromAddress"],
                        ConfigurationManager.AppSettings["GmailAppPassword"]);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(
                            ConfigurationManager.AppSettings["EmailFromAddress"],
                            ConfigurationManager.AppSettings["EmailFromName"]),
                        Subject = "ACTION REQUIRED: Student Verification - " + student.StudentNumber,
                        Body = emailBody,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(adminEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Admin alert failed: {ex.Message}");
                // Consider adding a retry mechanism here
            }
        }

        private async Task SendLandlordWelcomeEmail(ApplicationUser landlord)
        {
            try
            {
                var loginUrl = Url.Action("Login", "Account", null, Request.Url.Scheme);
                var supportEmail = ConfigurationManager.AppSettings["SupportEmail"] ?? "support@dutaccommodation.ac.za";
                var helpCenterUrl = "https://yourdomain.com/landlord-help";

                // Professional HTML template with landlord theme colors
                var emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{ 
                            font-family: 'Segoe UI', Arial, sans-serif; 
                            line-height: 1.6; 
                            color: #1a202c; 
                            max-width: 600px; 
                            margin: 0 auto; 
                            padding: 0;
                            background-color: #f5f7fa;
                        }}
                        .email-container {{
                            background: white;
                            border-radius: 8px;
                            overflow: hidden;
                            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
                        }}
                        .header {{ 
                            background: linear-gradient(135deg, #6366F1, #4F46E5);
                            padding: 30px 20px;
                            text-align: center;
                            color: white;
                        }}
                        .logo {{ max-width: 180px; height: auto; }}
                        .content {{ padding: 30px; }}
                        .details-box {{ 
                            background: #f8fafc; 
                            border-left: 4px solid #6366F1;
                            border-radius: 4px; 
                            padding: 15px; 
                            margin: 20px 0; 
                        }}
                        .button {{ 
                            display: inline-block; 
                            padding: 12px 24px; 
                            background: linear-gradient(135deg, #6366F1, #4F46E5);
                            color: white !important; 
                            text-decoration: none; 
                            border-radius: 6px; 
                            font-weight: 600; 
                            margin: 15px 0;
                            text-align: center;
                        }}
                        .footer {{ 
                            margin-top: 30px; 
                            padding: 20px;
                            background: #f8fafc;
                            text-align: center;
                            color: #718096; 
                            font-size: 14px;
                            border-top: 1px solid #e2e8f0;
                        }}
                        .steps {{ margin: 25px 0; }}
                        .step {{ 
                            display: flex;
                            margin-bottom: 15px;
                            align-items: flex-start;
                        }}
                        .step-number {{
                            background: #6366F1;
                            color: white;
                            width: 24px;
                            height: 24px;
                            border-radius: 50%;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            margin-right: 10px;
                            flex-shrink: 0;
                            font-size: 12px;
                            font-weight: bold;
                        }}
                        @media only screen and (max-width: 600px) {{
                            .content {{ padding: 20px; }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <div class='header'>
                            <img src='https://yourdomain.com/images/logo-white.png' alt='DUT CRIBS' class='logo'>
                            <h1 style='margin: 15px 0 0; font-weight: 600;'>Welcome to DUT CRIBS</h1>
                            <p style='margin: 5px 0 0; opacity: 0.9;'>Landlord Portal</p>
                        </div>
        
                        <div class='content'>
                            <h2 style='color: #2d3748; margin-top: 0;'>Hello, {landlord.FullName}!</h2>
                            <p>Thank you for joining DUT CRIBS as a property partner. Your landlord account has been successfully created and is pending approval.</p>
            
                            <div class='details-box'>
                                <p style='margin-top: 0;'><strong>Your Account Details:</strong></p>
                                <ul style='margin: 10px 0; padding-left: 20px;'>
                                    <li>Name: <strong>{landlord.FullName}</strong></li>
                                    <li>Email: <strong>{landlord.Email}</strong></li>
                                    <li>Phone: <strong>{landlord.ContactNumber}</strong></li>
                                </ul>
                            </div>

                            <div class='steps'>
                                <p><strong>Next Steps:</strong></p>
                                <div class='step'>
                                    <div class='step-number'>1</div>
                                    <div>Our team will review your application (usually within 2 business days)</div>
                                </div>
                                <div class='step'>
                                    <div class='step-number'>2</div>
                                    <div>You'll receive an approval email with access instructions</div>
                                </div>
                                <div class='step'>
                                    <div class='step-number'>3</div>
                                    <div>Start listing your properties to thousands of DUT students</div>
                                </div>
                            </div>
            
                            <p>While waiting for approval, you can prepare your property details and photos.</p>
            
                            <div style='text-align: center; margin: 25px 0;'>
                                <a href='{helpCenterUrl}' class='button'>PREPARE YOUR LISTINGS</a>
                            </div>

                            <p style='font-size: 15px;'>
                                Need immediate assistance? Contact our landlord support team at<br>
                                <a href='mailto:{supportEmail}' style='color: #4F46E5;'>{supportEmail}</a>
                            </p>
                        </div>
        
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} Durban University of Technology CRIBS</p>
                            <p style='margin: 10px 0;'>
                                <a href='https://yourdomain.com/privacy' style='color: #4F46E5; text-decoration: none; margin: 0 10px;'>Privacy Policy</a>
                                <a href='https://yourdomain.com/terms' style='color: #4F46E5; text-decoration: none; margin: 0 10px;'>Terms of Service</a>
                                <a href='https://yourdomain.com/contact' style='color: #4F46E5; text-decoration: none; margin: 0 10px;'>Contact Us</a>
                            </p>
                            <p style='font-size: 13px; color: #a0aec0;'>
                                This email was sent to {landlord.Email}. If you didn't create an account, please 
                                <a href='https://yourdomain.com/security' style='color: #4F46E5;'>contact our security team</a>.
                            </p>
                        </div>
                    </div>
                </body>
                </html>";

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = ConfigurationManager.AppSettings["SmtpHost"];
                    smtpClient.Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(
                        ConfigurationManager.AppSettings["EmailFromAddress"],
                        ConfigurationManager.AppSettings["GmailAppPassword"]);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(
                            ConfigurationManager.AppSettings["EmailFromAddress"],
                            "DUT CRIBS Landlord Support"),
                        Subject = "Welcome to DUT CRIBS - Landlord Account Created",
                        Body = emailBody,
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };
                    mailMessage.To.Add(landlord.Email);

                    // Add BCC to landlord management if needed
                    if (bool.Parse(ConfigurationManager.AppSettings["BccLandlordEmails"] ?? "true"))
                    {
                        mailMessage.Bcc.Add("landlord-management@dutcribs.ac.za");
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Landlord welcome email failed for {landlord.Email}: {ex.Message}");
                // Consider adding to a retry queue for failed emails
            }
        }

        private async Task SendAdminLandlordVerificationRequest(ApplicationUser landlord)
        {
            try
            {
                var adminEmail = ConfigurationManager.AppSettings["AdminEmail"] ?? "admin@dutcribs.ac.za";
                var verificationUrl = Url.Action("LandlordDetails", "Admin",
                    new { userId = landlord.Id }, protocol: Request.Url.Scheme);

                // Professional HTML template for admin notification
                var emailBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <style>
                body {{ 
                    font-family: 'Segoe UI', Arial, sans-serif; 
                    line-height: 1.6; 
                    color: #1a202c; 
                    max-width: 600px; 
                    margin: 0 auto; 
                    padding: 20px;
                }}
                .header {{ 
                    background: linear-gradient(135deg, #6366F1, #4F46E5);
                    padding: 20px;
                    text-align: center;
                    color: white;
                    border-radius: 8px 8px 0 0;
                }}
                .logo {{ max-width: 120px; height: auto; }}
                .content {{ 
                    padding: 25px;
                    background: white;
                    border-left: 1px solid #e2e8f0;
                    border-right: 1px solid #e2e8f0;
                }}
                .alert-badge {{
                    background: #F59E0B;
                    color: white;
                    padding: 5px 10px;
                    border-radius: 20px;
                    font-size: 14px;
                    font-weight: bold;
                    display: inline-block;
                    margin-bottom: 15px;
                }}
                .details-box {{ 
                    background: #f8fafc; 
                    border-left: 4px solid #6366F1;
                    border-radius: 4px; 
                    padding: 15px; 
                    margin: 20px 0; 
                }}
                .button {{ 
                    display: inline-block; 
                    padding: 10px 20px; 
                    background: linear-gradient(135deg, #6366F1, #4F46E5);
                    color: white !important; 
                    text-decoration: none; 
                    border-radius: 6px; 
                    font-weight: 600; 
                    margin: 15px 0;
                }}
                .footer {{ 
                    margin-top: 20px; 
                    padding: 15px;
                    text-align: center;
                    color: #718096; 
                    font-size: 13px;
                    background: #f8fafc;
                    border-radius: 0 0 8px 8px;
                    border: 1px solid #e2e8f0;
                }}
                table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
                td {{ padding: 8px 0; border-bottom: 1px solid #edf2f7; }}
                td:first-child {{ font-weight: 600; width: 30%; color: #4a5568; }}
            </style>
        </head>
        <body>
            <div class='header'>
                <img src='https://yourdomain.com/images/logo-white.png' alt='DUT CRIBS Admin' class='logo'>
                <h2 style='margin: 10px 0 0;'>New Landlord Verification Required</h2>
            </div>
    
            <div class='content'>
                <div class='alert-badge'>ACTION REQUIRED</div>
                <p>A new landlord has registered and requires verification:</p>
        
                <div class='details-box'>
                    <table>
                        <tr>
                            <td>Landlord Name:</td>
                            <td><strong>{landlord.FullName}</strong></td>
                        </tr>
                        <tr>
                            <td>Email:</td>
                            <td><strong>{landlord.Email}</strong></td>
                        </tr>
                        <tr>
                            <td>Phone:</td>
                            <td><strong>{landlord.ContactNumber}</strong></td>
                        </tr>
                        <tr>
                            <td>Registered On:</td>
                            <td><strong>{landlord.RegistrationDate.ToString("f")}</strong></td>
                        </tr>
                    </table>
                </div>

                <p>Please review this landlord's information and verify their account:</p>
        
                <div style='text-align: center;'>
                    <a href='{verificationUrl}' class='button'>REVIEW LANDLORD ACCOUNT</a>
                </div>

                <p style='font-size: 14px; color: #4a5568;'>
                    <strong>Note:</strong> This landlord cannot list properties until their account is verified.
                </p>
            </div>
    
            <div class='footer'>
                <p>© {DateTime.Now.Year} Durban University of Technology CRIBS</p>
                <p>
                    <a href='https://yourdomain.com/admin' style='color: #4F46E5; text-decoration: none;'>Admin Portal</a> | 
                    <a href='https://yourdomain.com/admin/help' style='color: #4F46E5; text-decoration: none;'>Admin Guide</a>
                </p>
            </div>
        </body>
        </html>";

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = ConfigurationManager.AppSettings["SmtpHost"];
                    smtpClient.Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(
                        ConfigurationManager.AppSettings["EmailFromAddress"],
                        ConfigurationManager.AppSettings["GmailAppPassword"]);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(
                            ConfigurationManager.AppSettings["EmailFromAddress"],
                            "DUT CRIBS Admin Alerts"),
                        Subject = $"[Action Required] New Landlord Verification: {landlord.FullName}",
                        Body = emailBody,
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };
                    mailMessage.To.Add(adminEmail);

                    // Add secondary admin emails if configured
                    var secondaryAdmins = ConfigurationManager.AppSettings["SecondaryAdminEmails"];
                    if (!string.IsNullOrEmpty(secondaryAdmins))
                    {
                        foreach (var email in secondaryAdmins.Split(';'))
                        {
                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                mailMessage.CC.Add(email.Trim());
                            }
                        }
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Admin notification email failed for landlord {landlord.Id}: {ex.Message}");
                // Consider logging to a more persistent store for admin notifications
            }
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}