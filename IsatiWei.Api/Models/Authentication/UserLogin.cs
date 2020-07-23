using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Models.Authentication
{
    // This class is the model for login only
    /// NOTES: The user name can be both the email or the actual username in this case
    public class UserLogin
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
