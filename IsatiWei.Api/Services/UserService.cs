using IsatiWei.Api.Models;
using IsatiWei.Api.Settings;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        private readonly GridFSBucket _gridFS;

        public UserService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>("users");
            _gridFS = new GridFSBucket(database);
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var user = await _users.FindAsync(databaseUser => databaseUser.Id == userId);

            return await user.FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetUsersAsync()
        {
            var users = await _users.FindAsync(databaseUser => true);

            return await users.ToListAsync();
        }

        public async Task UpdateUserAsync(User toUpdate)
        {
            if (string.IsNullOrWhiteSpace(toUpdate.Id)) throw new Exception("The id must be provided in the body");
            
            // If we don't do it manally old images are never deleted
            var oldUser = await GetUserAsync(toUpdate.Id);
            if (oldUser == null) throw new Exception("The user doesn't exist");

            if (oldUser.ProfilePictureId != ObjectId.Empty)
            {
                await _gridFS.DeleteAsync(oldUser.ProfilePictureId);
            }

            var userImage = await _gridFS.UploadFromBytesAsync($"user_{toUpdate.Id}", toUpdate.ProfilePicture);
            toUpdate.ProfilePicture = null;
            toUpdate.ProfilePictureId = userImage;

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == toUpdate, toUpdate);
        }

        /*
         * Profile picture related functions
         */
        public async Task<byte[]> GetProfilePicture(string userId)
        {
            var user = await GetUserAsync(userId);
            if (user == null || user.ProfilePictureId == ObjectId.Empty)
            {
                return null;
            }

            return await _gridFS.DownloadAsBytesAsync(user.ProfilePictureId);
        }

        public async Task UpdateProfilePicture(string id, byte[] newProfilePicture)
        {
            User user = await (await _users.FindAsync(databaseUser => databaseUser.Id == id)).FirstOrDefaultAsync();
            if (user == null) throw new Exception("Can't find the user");

            if (user.ProfilePictureId != ObjectId.Empty)
            {
                await _gridFS.DeleteAsync(user.ProfilePictureId);
            }

            var userImage = await _gridFS.UploadFromBytesAsync($"user_{user.Id}", newProfilePicture);

            user.ProfilePictureId = userImage;

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == user.Id, user);
        }
    }
}
