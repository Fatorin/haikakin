using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Haikakin.Models
{
    public class AuthenticateResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public string JwtToken { get; set; }
        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public AuthenticateResponse(User user, string jwtToken, string refreshToken)
        {
            UserId = user.UserId;
            Username = user.Username;
            Email = user.Email;
            PhoneNumber = user.PhoneNumber;
            JwtToken = jwtToken;
            RefreshToken = refreshToken;
        }
    }
}
