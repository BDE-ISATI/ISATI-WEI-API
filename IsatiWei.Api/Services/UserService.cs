using IsatiWei.Api.Models;
using IsatiWei.Api.Settings;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>("users");
        }

        /*
         * Profile related functions
         */
        public async Task UpdateProfilePicture(string id, byte[] newProfilePicture)
        {
            User user = await (await _users.FindAsync(databaseUser => databaseUser.Id == id)).FirstOrDefaultAsync();
            if (user == null) throw new Exception("Can't find the user");

            user.ProfilePicture = newProfilePicture;

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == user.Id, user);
        }
    }
}
