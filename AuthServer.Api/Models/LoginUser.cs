﻿namespace AuthServer.Api.Models
{
    public class LoginUser
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string? UserEmail { get; set; }
    }
}
