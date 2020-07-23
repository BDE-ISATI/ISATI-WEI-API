using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Models.Authentication
{
    // This class is the model for registration only and it's mapped to a json object
    // The json should looks like this :
    // 
    // {
    //     "firstName": "Victor",
    //     "lastName": "DENIS",
    //     "email": "admin@feldrise.com",
    //     "username": "Feldrise",
    //     "password": "MySecurePassword"
    // }
    public class UserRegister
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
