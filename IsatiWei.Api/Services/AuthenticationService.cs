using IsatiWei.Api.Models;
using IsatiWei.Api.Settings;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Services
{
    // Allow to login and register users
    public class AuthenticationService
    {
        private readonly IMongoCollection<User> _users;

        public AuthenticationService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>("users");
        }

        /* 
         * Authentication related functions 
         */
        public User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            // Here the user can login with both his email or his real username, that's why we check both
            var user = _users.Find(user => user.Email == username || user.Username == username).FirstOrDefault();

            if (user == null)
            {
                return null;
            }

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            return user;
        }

        public User Register(User user, string password)
        {
            // Basic checks
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("The password is required", "password");
            if (string.IsNullOrWhiteSpace(user.Email)) throw new Exception("You must provide an email");
            if (string.IsNullOrWhiteSpace(user.Username)) throw new Exception("You must provide a username");
            if (UserExist(user)) throw new Exception("The email or the username is already in use");

            // Password stuff, to ensure we never have clear password stored
            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = Convert.ToBase64String(passwordHash);
            user.PasswordSalt = passwordSalt;

            // Insert user to database, so first we apply him the default data
            user.Role = UserRoles.Default;
            user.WaitingCallenges = new Dictionary<string, byte[]>() { };
            user.FinishedCallenges = new Dictionary<string, int>() { };

            _users.InsertOne(user);

            return user;
        }

        /* 
         * Password and credential related functions
         */
        // We need to be able to check credential with only the password hash
        public bool CheckCredential(string id, string passwordHash)
        {
            User databaseUser = _users.Find(searchedUser => searchedUser.Id == id).FirstOrDefault();

            if (databaseUser == null)
            {
                return false;
            }

            return passwordHash == databaseUser.PasswordHash;
        }

        public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using var hmac = new System.Security.Cryptography.HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }

        private bool UserExist(User checkedUser)
        {
            var exist = _users.AsQueryable<User>().Any(user => user.Email == checkedUser.Email || user.Username == checkedUser.Username);

            return exist;
        }

        private bool VerifyPasswordHash(string password, string storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordSalt");

            var storedHashBytes = Convert.FromBase64String(storedHash);

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < computedHash.Length; ++i)
                {
                    if (computedHash[i] != storedHashBytes[i]) return false;
                }
            }

            return true;
        }
    }
}
