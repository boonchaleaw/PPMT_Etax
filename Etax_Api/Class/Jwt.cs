
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Etax_Api
{
    public class JwtStatus
    {
        public int user_id;
        public int member_id;
        public bool status;
        public string message;
    }
    public static class Jwt
    {
        private static string key = "401b09eab3c013d4ca54922bb802bec8fd5318192b0a75";
        private static string issure = "papermate_etax";
        public static string GenerateJwtToken(int user_id, int member_id)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(issure,
              user_id.ToString(),
              new List<Claim>()
              {
                  new Claim("user_id", user_id.ToString()),
                  new Claim("member_id", member_id.ToString()),
              },
              expires: DateTime.Now.AddDays(1),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static JwtStatus ValidateJwtToken(string token)
        {
            try
            {
                token = token.Replace("Bearer ", "");

                var tokenHandler = new JwtSecurityTokenHandler();
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

                TokenValidationParameters para = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuer = issure,
                    RequireExpirationTime = true,
                    LifetimeValidator = LifetimeValidator,

                };

                tokenHandler.ValidateToken(token, para, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var user_id = jwtToken.Claims.First(claim => claim.Type == "user_id").Value;
                var member_id = jwtToken.Claims.First(claim => claim.Type == "member_id").Value;

                return new JwtStatus()
                {
                    user_id = Convert.ToInt32(user_id),
                    member_id = Convert.ToInt32(member_id),
                    status = true,
                    message = "",
                };

            }
            catch (Exception ex)
            {
                return new JwtStatus()
                {
                    status = false,
                    message = ex.Message,
                };
            }
        }

        private static bool LifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters @params)
        {
            if (expires != null)
            {
                return expires > DateTime.UtcNow;
            }
            return false;
        }
    }
}
