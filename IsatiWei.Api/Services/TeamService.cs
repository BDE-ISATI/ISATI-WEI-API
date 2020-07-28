using IsatiWei.Api.Models;
using IsatiWei.Api.Settings;
using MongoDB.Bson;
using MongoDB.Driver;
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

        public TeamService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _teams = database.GetCollection<Team>("teams");
            _users = database.GetCollection<User>("users");
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

        /*
         * Edition stuff
         */
        public async Task<Team> CreateTeamAsyn(string name, string captainId)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("You must provide a name for the team", "name");
            if (string.IsNullOrWhiteSpace(captainId)) throw new ArgumentException("You must provide a captain for the team", "captainId");

            // We change the captain role
            User newCaptain = await (await _users.FindAsync(user => user.Id == captainId)).FirstOrDefaultAsync();

            if (newCaptain == null)
            {
                throw new Exception("The user specified to be captain is not valid");
            }

            if (newCaptain.Role == UserRoles.Captain) throw new Exception("The user choosed is already a captain");

            if (newCaptain.Role != UserRoles.Administrator)
            {
                newCaptain.Role = UserRoles.Captain;
                await _users.ReplaceOneAsync(user => user.Id == newCaptain.Id, newCaptain);
            }

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

            await _teams.InsertOneAsync(team);

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

            await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == toUpdate.Id, current);
        }

        public Task DeleteTeamAsync(string teamId)
        {
            return _teams.DeleteOneAsync(team => team.Id == teamId);
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
            if (await GetUserTeamAsync(userId) != null) throw new Exception("The user already belong to a team");

            Team team = await (await _teams.FindAsync(databaseTeam => databaseTeam.Id == teamId)).FirstOrDefaultAsync();
            if (team == null) throw new Exception("The team doesn't exist");

            team.Members.Add(userId);

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
