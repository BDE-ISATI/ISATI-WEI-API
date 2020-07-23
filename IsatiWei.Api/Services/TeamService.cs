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
            var team = await _teams.FindAsync(team => team.Id == teamId);

            return await team.FirstOrDefaultAsync();
        }

        public async Task<List<Team>> GetTeamsAsync()
        {
            var teams = await _teams.FindAsync(team => true);

            return await teams.ToListAsync();
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
                Score = 0
            };

            if (TeamExist(team))
            {
                throw new Exception("The team already exist");
            }

            await _teams.InsertOneAsync(team);

            return team;
        }


        /*
         * Utility stuff
         */
        public async Task<int> GetTeamRankAsync(String teamId)
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
