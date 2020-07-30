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
        private readonly IMongoCollection<GameSettings> _gameSettings;

        private readonly GridFSBucket _gridFS;

        public UserService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>("users");
            _gameSettings = database.GetCollection<GameSettings>("game_settings");
            _gridFS = new GridFSBucket(database);
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var user = await (await _users.FindAsync(databaseUser => databaseUser.Id == userId)).FirstOrDefaultAsync();

            if (user == null)
            {
                return null;
            }

            // We need to remove password hash for security reasons;
            user.PasswordHash = null;

            return user;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            var users = await (await _users.FindAsync(databaseUser => true)).ToListAsync();

            // We need to remove password hash for security reasons;
            foreach (var user in users)
            {
                user.PasswordHash = null;
            }

            return users;
        }

        public async Task<List<User>> GetUsersRankingAsync(string userRequestingId)
        {
            if ((await (await _users.FindAsync(databaseUser => databaseUser.Id == userRequestingId)).FirstOrDefaultAsync()).Role != UserRoles.Administrator)
            {
                GameSettings settings = await (await _gameSettings.FindAsync(databaseGameSettings => true)).FirstOrDefaultAsync();

                if (settings == null)
                {
                    settings = new GameSettings()
                    {
                        IsUsersRankingVisible = false,
                        IsTeamsRankingVisible = false
                    };
                }

                if (!settings.IsUsersRankingVisible)
                {
                    return null;
                }
            }

            var sortedUsers = await _users.Find(user => true).Sort(new BsonDocument("Score", -1)).ToListAsync();

            List<User> result = new List<User>();

            foreach (var user in sortedUsers)
            {
                if (user.Role == UserRoles.Captain || user.Role == UserRoles.Administrator)
                {
                    continue;
                }

                user.PasswordHash = null;
                result.Add(user);
            }

            return result;
        }

        /*
         * Edition stuff
         */
        public async Task UpdateUserAsync(User toUpdate)
        {
            if (string.IsNullOrWhiteSpace(toUpdate.Id)) throw new Exception("The id must be provided in the body");
            
            // If we don't do it manally old images are never deleted
            var oldUser = await (await _users.FindAsync(databaseUser => databaseUser.Id == toUpdate.Id)).FirstOrDefaultAsync();
            if (oldUser == null) throw new Exception("The user doesn't exist");

            if (toUpdate.ProfilePictureId == "modified")
            {
                if (!string.IsNullOrWhiteSpace(oldUser.ProfilePictureId))
                {
                    await _gridFS.DeleteAsync(new ObjectId(oldUser.ProfilePictureId));
                }

                var userImage = await _gridFS.UploadFromBytesAsync($"user_{toUpdate.Id}", toUpdate.ProfilePicture);
                toUpdate.ProfilePicture = null;
                toUpdate.ProfilePictureId = userImage.ToString();
            }
            else
            {
                toUpdate.ProfilePictureId = oldUser.ProfilePictureId;
            }

            toUpdate.WaitingCallenges = oldUser.WaitingCallenges;
            toUpdate.FinishedCallenges = oldUser.FinishedCallenges;
            toUpdate.PasswordHash = oldUser.PasswordHash;
            toUpdate.PasswordSalt = oldUser.PasswordSalt;

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == toUpdate.Id, toUpdate);
        }

        public async Task DeleteUserAsync(string userId)
        {
            var oldUser = await GetUserAsync(userId);
            if (oldUser != null)
            {
                await _gridFS.DeleteAsync(new ObjectId(oldUser.ProfilePictureId));
            }

            await _users.DeleteOneAsync(databaseUser => databaseUser.Id == userId);
        }

        /*
         * Profile picture related functions
         */
        public async Task<byte[]> GetProfilePicture(string userId)
        {
            var user = await GetUserAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.ProfilePictureId))
            {
                return null;
            }

            return await _gridFS.DownloadAsBytesAsync(new ObjectId(user.ProfilePictureId));
        }

        public async Task UpdateProfilePicture(string id, byte[] newProfilePicture)
        {
            User user = await (await _users.FindAsync(databaseUser => databaseUser.Id == id)).FirstOrDefaultAsync();
            if (user == null) throw new Exception("Can't find the user");

            if (!string.IsNullOrWhiteSpace(user.ProfilePictureId))
            {
                await _gridFS.DeleteAsync(new ObjectId(user.ProfilePictureId));
            }

            var userImage = await _gridFS.UploadFromBytesAsync($"user_{user.Id}", newProfilePicture);

            user.ProfilePictureId = userImage.ToString();

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == user.Id, user);
        }
    }
}
