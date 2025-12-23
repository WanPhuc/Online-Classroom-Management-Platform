using System.Security.Claims;
using BTL_QuanLyLopHocTrucTuyen.Helpers;
using BTL_QuanLyLopHocTrucTuyen.Models;
using BTL_QuanLyLopHocTrucTuyen.Models.Enums;
using BTL_QuanLyLopHocTrucTuyen.Models.ViewModels;
using BTL_QuanLyLopHocTrucTuyen.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BTL_QuanLyLopHocTrucTuyen.Controllers.API
{
    [Route("api/authen")]
    [ApiController]
    public class AuthenticationController(IUserRepository userRepository, IRoleRepository roleRepository, IMemoryCache memoryCache) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Thông tin đăng nhập không hợp lệ." });

            var user = await userRepository.ValidateUser(request.Email, request.Password);
            if (user == null)
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

            if (memoryCache.TryGetValue(user.Id, out Guid cacheSession))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Conflict(new { message = "Tài khoản này đã được đăng nhập ở thiết bị khác." });
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
                return Conflict(new { message = $"Người dùng {User.Identity.Name} đã đăng nhập." });

            var sessionId = Guid.NewGuid();
            memoryCache.Set(user.Id, sessionId, TimeSpan.FromMinutes(30));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("SessionId", sessionId.ToString())
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Ok(new { message = $"Đăng nhập thành công. Chào mừng {user.FullName}!" });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.GetUserId();
            if (userId != Guid.Empty)
            {
                memoryCache.Remove(userId);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Đăng xuất thành công." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Thông tin đăng ký không hợp lệ." });

            var existingUser = await userRepository.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return Conflict(new { message = "Email này đã được sử dụng." });

                

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = SecurityHelper.HashPassword(request.Password),
                RoleId = roleRepository.DefaultRole.Id
            };

            await userRepository.AddAsync(user);
            return Ok(new { message = "Đăng ký tài khoản thành công." });
        }
    }
}
