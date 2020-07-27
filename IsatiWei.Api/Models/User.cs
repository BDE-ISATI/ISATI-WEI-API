using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
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

        // Administrators have the same rights as captains as default
        public static bool RoleAuthorized(string checkedRole, string permission)
        {
            if (checkedRole == Administrator) return true;
            if (checkedRole == Captain) return permission == Captain || permission == Default;
            if (checkedRole == Default) return permission == Default;

            return false;
        }
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

        public byte[] ProfilePicture { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }

        public string PasswordHash { get; set; }
        [JsonIgnore]
        public byte[] PasswordSalt { get; set; }

        /* 
         * Application specific things
         */
        public string Role { get; set; }
        public int Score { get; set; }

        [JsonIgnore]
        public Dictionary<string, ObjectId> WaitingCallenges; // Since this one contains proof images,
                                                            // we don't want to transfer it anywhere
        public Dictionary<string, int> FinishedCallenges;
    }
}
