﻿using Application.DTO;
using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Services.WebApi.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Transversal.Common;

namespace Services.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUsersApplication _usersApplication;
        private readonly AppSettings _appSettings;
        public UsersController(IUsersApplication usersApplication, IOptions<AppSettings> appSettings)
        {
            _usersApplication = usersApplication;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]  // Conexion anonima porque es el metodo que genera nuestros token
        [HttpPost("Authenticate")]
        public IActionResult Authenticate([FromBody] UsersDto usersDto)
        {
            var response = _usersApplication.Authenticate(usersDto.UserName, usersDto.Password);
            if (response.IsSuccess)
            {
                if (response.Data != null)
                {
                    response.Data.Token = BuildToken(response);
                    return Ok(response);
                }
                else
                    return NotFound(response);
            }
            return BadRequest(response);
        }

        private string BuildToken(Response<UsersDto> userDto)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, userDto.Data.UserId.ToString()) //Internamente contendra el token el id del cliente
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _appSettings.Issuer,
                Audience = _appSettings.Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        }
    }
}
