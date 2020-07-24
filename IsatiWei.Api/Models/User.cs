using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Models
{
    static class UserRoles
    {
        public const string Default = "Default";
        public const string Captain = "Captain";
        public const string Administrator = "Administrator";
    }

    // Represent the user
    public class User
    {
        /*
         * General things
         */
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }

        public string PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }

        /* 
         * Application specific things
         */
        public string Role { get; set; }
        public int Score { get; set; }

        public Dictionary<string, byte[]> WaitingCallenges;
        public Dictionary<string, int> FinishedCallenges;
    }
}
