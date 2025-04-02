using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Repositories.Models;
using Repositories.Interfaces;
using Helpers;
using TaskTrackPro.Services;

namespace API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserApiController : ControllerBase
    {
        private readonly IUserInterface _user;
        private readonly IConfiguration _config;
        private readonly FileHelper _fileHelper;
        private readonly string _profileImagePath;

        public UserApiController(IConfiguration configuration, IUserInterface userInterface)
        {
            _config = configuration;
            _user = userInterface;
            _profileImagePath = "../MVC/wwwroot/profile_images";
            _fileHelper = new FileHelper();
        }


        #region Login
        [HttpPost]
        [Route("auth")]
        public async Task<IActionResult> Login([FromForm] LoginVM user)
        {
            User? UserData = await _user.Login(user);
            if (UserData == null) 
            {
                return Unauthorized(new
                {
                    message = "Invalid Credentials"
                });
            }

            Console.WriteLine($"UserID :: {UserData.UserId} :: {UserData.Role}");
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", UserData.UserId.ToString() ?? string.Empty),
                new Claim("UserName", UserData.FirstName + " " + UserData.LastName),
                new Claim(ClaimTypes.Role, UserData.Role.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? string.Empty));
            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: signIn
            );

            return Ok(new
            {
                message = "Login Success",
                data = UserData,
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }
        #endregion


        #region GetAll
        [HttpGet]
        // [Authorize(Roles = "A")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _user.GetAll();
            return Ok(new { data = users });
        }
        #endregion


        #region GetOne
        [HttpGet]
        // [Authorize]
        [Route("{id}")]
        public async Task<IActionResult> GetOne(string id)
        {
            var user = await _user.GetOne(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new { data = user });
        }
        #endregion


        #region Add
        [HttpPost]
        public async Task<IActionResult> Add([FromForm] User model)
        {
            if (model?.ImageFile != null)
            {
                model.Image = await _fileHelper.UploadFile(_profileImagePath, model.ImageFile, model.Image);
            }
            
            var result = await _user.Add(model ?? throw new NullReferenceException("User object is null"));
            if (result == -1)
                return BadRequest(new { message = "Email already exists" });
            if (result == 0)
                return BadRequest(new { message = "Registration failed" });

            var emailService =  new EmailService();

            await emailService.SendWelcomeEmailAsync(model.Email, model.FirstName + " " + model.LastName);

            return Ok(new { message = "Registration successful" });
        }
        #endregion


        #region Update
        [HttpPut]
        // [Authorize]
        public async Task<IActionResult> Update([FromForm] User model)
        {
            if (model?.ImageFile != null)
            {
                model.Image = await _fileHelper.UploadFile(_profileImagePath, model.ImageFile, model.Image);
            }
            var result = await _user.Update(model ?? throw new NullReferenceException("User object is null"));
            if (result == 0)
                return BadRequest(new { message = "Update failed" });

            return Ok(new { message = "Update successful", data = model });
        }
        #endregion


        #region UpdatePassword
        [HttpPut]
        // [Authorize]
        [Route("change-pass")]
        public async Task<IActionResult> UpdatePassword([FromForm] ChangePasswordVM model)
        {
            var result = await _user.UpdatePassword(model);
            if (result == -1)
                return BadRequest(new { message = "Old password is incorrect" });
            if (result == 0)
                return BadRequest(new { message = "Password update failed" });

            return Ok(new { message = "Password updated successfully" });
        }
        #endregion
    }

}