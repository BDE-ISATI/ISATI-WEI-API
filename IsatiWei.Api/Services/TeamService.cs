using IsatiWei.Api.Models;
using IsatiWei.Api.Models.Team;
using IsatiWei.Api.Settings;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Services
{
    public class TeamService
    {
        private readonly IMongoCollection<Team> _teams;
        private readonly IMongoCollection<User> _users;

        private readonly GridFSBucket _gridFS;

        public TeamService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _teams = database.GetCollection<Team>("teams");
            _users = database.GetCollection<User>("users");

            _gridFS = new GridFSBucket(database);
        }

        public async Task<Team> GetTeamAsync(string teamId)
        {
            var team = await (await _teams.FindAsync(team => team.Id == teamId)).FirstOrDefaultAsync();
            if (team == null) return null;

            var captain = await (await _users.FindAsync(databaseUser => databaseUser.Id == team.CaptainId)).FirstOrDefaultAsync();
            if (captain == null) return null;

            team.CaptainName = $"{captain.FirstName} {captain.LastName}";

            return team;
        }

        public async Task<List<Team>> GetTeamsAsync()
        {
            var teams = await (await _teams.FindAsync(team => true)).ToListAsync();

            foreach (var team in teams)
            {
                var captain = await (await _users.FindAsync(databaseUser => databaseUser.Id == team.CaptainId)).FirstOrDefaultAsync();
                if (captain == null) continue;

                team.CaptainName = $"{captain.FirstName} {captain.LastName}";
            }

            return teams;
        }

        public async Task<List<User>> GetAvailableCaptain()
        {
            var teams = await GetTeamsAsync();
            var users = await (await _users.FindAsync(databaseUser => true)).ToListAsync();

            List<User> result = new List<User>();

            foreach (var user in users)
            {
                user.PasswordHash = "";

                bool isCaptain = false;

                foreach (var team in teams)
                {
                    if (team.CaptainId == user.Id)
                    {
                        isCaptain = true;
                        break;
                    }
                }

                if (!isCaptain)
                {
                    result.Add(user);
                }
            }

            return result;
        }

        /*
         * Profile picture related functions
         */
        public async Task<byte[]> GetTeamImage(string teamId)
        {
            var team = await GetTeamAsync(teamId);
            if (team == null || string.IsNullOrWhiteSpace(team.ImageId))
            {
                return null;
            }

            return await _gridFS.DownloadAsBytesAsync(new ObjectId(team.ImageId));
        }

        /*
         * Edition stuff
         */
        public async Task<Team> CreateTeamAsyn(string name, string captainId, byte[] teamImage)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("You must provide a name for the team", "name");
            if (string.IsNullOrWhiteSpace(captainId)) throw new ArgumentException("You must provide a captain for the team", "captainId");

            // We create the team
            var team = new Team()
            {
                CaptainId = captainId,
                Name = name,
                Members = new List<string>(),
                Score = 0,
                FinishedCallenges = new Dictionary<string, int>()
            };

            if (TeamExist(team))
            {
                throw new Exception("The team already exist");
            }

            // We change the captain role
            User newCaptain = await (await _users.FindAsync(user => user.Id == captainId)).FirstOrDefaultAsync();

            if (newCaptain == null)
            {
                throw new Exception("The user specified to be captain is not valid");
            }

            // We must remove the new captain from it's current team
            Team oldTeam = await GetUserTeamAsync(newCaptain.Id);
            if (oldTeam != null)
            {
                oldTeam.Members.Remove(newCaptain.Id);
                await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == oldTeam.Id, oldTeam);
            }

            if (newCaptain.Role == UserRoles.Captain) throw new Exception("The user choosed is already a captain");

            if (newCaptain.Role != UserRoles.Administrator)
            {
                newCaptain.Role = UserRoles.Captain;
                await _users.ReplaceOneAsync(user => user.Id == newCaptain.Id, newCaptain);
            }

            await _teams.InsertOneAsync(team);

            var databaseTeamImage = await _gridFS.UploadFromBytesAsync($"team_{team.Id}", teamImage);
            team.Image = null;
            team.ImageId = databaseTeamImage.ToString();

            await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == team.Id, team);

            return team;
        }

        public async Task UpdateTeamAsync(Team toUpdate)
        {
            if (string.IsNullOrWhiteSpace(toUpdate.Id)) throw new Exception("The id must be provided in the body");
            if (string.IsNullOrWhiteSpace(toUpdate.Name)) throw new ArgumentException("You must provide a name for the team", "name");
            if (string.IsNullOrWhiteSpace(toUpdate.CaptainId)) throw new ArgumentException("You must provide a captain for the team", "captainId");

            Team current = await (await _teams.FindAsync(databaseTeam => databaseTeam.Id == toUpdate.Id)).FirstOrDefaultAsync();
            if (current == null) throw new Exception("The team you want to update doesn't exist");

            // We need to change the role of the old captain
            if (current.CaptainId != toUpdate.CaptainId)
            {
                User oldCaptain = await (await _users.FindAsync(databaseUser => databaseUser.Id == current.CaptainId)).FirstOrDefaultAsync();
                User newCaptain = await (await _users.FindAsync(databaseUser => databaseUser.Id == toUpdate.CaptainId)).FirstOrDefaultAsync();

                // We must remove the new captain from it's current team
                Team oldTeam = await GetUserTeamAsync(newCaptain.Id);
                if (oldTeam != null)
                {
                    oldTeam.Members.Remove(newCaptain.Id);
                    await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == oldTeam.Id, oldTeam);
                }

                // We add the old captain to the team cause we are nice =P
                current.Members.Add(oldCaptain.Id);

                if (oldCaptain.Role != UserRoles.Administrator)
                {
                    oldCaptain.Role = UserRoles.Default;
                    await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == oldCaptain.Id, oldCaptain);
                }
                
                if (newCaptain.Role != UserRoles.Administrator)
                {
                    newCaptain.Role = UserRoles.Captain;
                    await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == newCaptain.Id, newCaptain);
                }
            }

            // We finally update the team, based on the current to keep members and score
            current.Name = toUpdate.Name;
            current.CaptainId = toUpdate.CaptainId;

            if (!string.IsNullOrWhiteSpace(current.ImageId))
            {
                await _gridFS.DeleteAsync(new ObjectId(current.ImageId));
            }

            var teamImage = await _gridFS.UploadFromBytesAsync($"team_{current.Id}", toUpdate.Image);
            current.Image = null;
            current.ImageId = teamImage.ToString();

            await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == toUpdate.Id, current);
        }

        public async Task DeleteTeamAsync(string teamId)
        {
            var oldTeam = await GetTeamAsync(teamId);
            if (oldTeam != null)
            {
                await _gridFS.DeleteAsync(new ObjectId(oldTeam.ImageId));

                // We remove captain role for old captain
                User oldCaptain = await (await _users.FindAsync(databaseUser => databaseUser.Id == oldTeam.CaptainId)).FirstOrDefaultAsync();

                if (oldCaptain.Role != UserRoles.Administrator)
                {
                    oldCaptain.Role = UserRoles.Default;
                    await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == oldCaptain.Id, oldCaptain);
                }
            }

            await _teams.DeleteOneAsync(team => team.Id == teamId);
        }

        /*
         * Members stuff
         */
        public async Task<Team> GetUserTeamAsync(string userId)
        {
            List<Team> teams = await (await _teams.FindAsync(databaseTeam => true)).ToListAsync();

            foreach (var team in teams)
            {
                if (team.Members.Contains(userId) || team.CaptainId == userId)
                {
                    var captain = await (await _users.FindAsync(databaseUser => databaseUser.Id == team.CaptainId)).FirstOrDefaultAsync();
                    if (captain == null) continue;

                    team.CaptainName = $"{captain.FirstName} {captain.LastName}";

                    return team;
                }
            }

            return null;
        }

        public async Task AddUserToTeam(string teamId, string userId)
        {
            Team oldTeam = await GetUserTeamAsync(userId);
            if (oldTeam != null)
            {
                oldTeam.Members.Remove(userId);
                await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == oldTeam.Id, oldTeam);
            }

            Team team = await (await _teams.FindAsync(databaseTeam => databaseTeam.Id == teamId)).FirstOrDefaultAsync();
            if (team == null) throw new Exception("The team doesn't exist");

            team.Members.Add(userId);

            // We update the database
            await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == team.Id, team);
        }

        public async Task RemoveUserFromeTeam(string teamId, string userId)
        {
            Team team = await (await _teams.FindAsync(databaseTeam => databaseTeam.Id == teamId)).FirstOrDefaultAsync();
            if (team == null) throw new Exception("The team doesn't exist");

            team.Members.Remove(userId);

            // We update the database
            await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == team.Id, team);
        }

        /*
         * Utility stuff
         */
        public async Task<int> GetTeamRankAsync(string teamId)
        {
            var sortedTeams = await _teams.Find(team => true).Sort(new BsonDocument("Score", -1)).ToListAsync();

            return sortedTeams.IndexOf(sortedTeams.Find(team => team.Id == teamId)) + 1;
        }

        private bool TeamExist(Team checkedTeam)
        {
            var exist = _teams.AsQueryable<Team>().Any(team => team.Name == checkedTeam.Name || team.CaptainId == checkedTeam.CaptainId);

            return exist;
        }
    }
}
