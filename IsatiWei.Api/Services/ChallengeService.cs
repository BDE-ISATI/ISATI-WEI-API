using IsatiWei.Api.Models;
using IsatiWei.Api.Settings;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Services
{
    public class ChallengeService
    {
        private readonly IMongoCollection<Team> _teams;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Challenge> _challenges;

        public ChallengeService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _teams = database.GetCollection<Team>("teams");
            _users = database.GetCollection<User>("users");
            _challenges = database.GetCollection<Challenge>("challenges");
        }

        public async Task<Challenge> GetChallengeAsync(string challengeId)
        {
            var challenge = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.Id == challengeId)).FirstOrDefaultAsync();

            if (challenge != null && challenge.Image != null)
            {
                challenge.Base64Image = Convert.ToBase64String(challenge.Image);
            }

            return challenge;
        }

        public async Task<List<Challenge>> GetChallengesAsync()
        {
            var challenges = await (await _challenges.FindAsync(databaseChallenge => true)).ToListAsync();

            foreach (var challenge in challenges)
            {
                challenge.Base64Image = Convert.ToBase64String(challenge.Image);
            }

            return challenges;
        }

        /*
         * Edition stuff
         */
        public Task CreateChallengeAsync(Challenge toCreate)
        {
            if (string.IsNullOrWhiteSpace(toCreate.Name)) throw new Exception("You must provide a name to the challenge");
            if (string.IsNullOrWhiteSpace(toCreate.Description)) throw new Exception("You must provide a description to the challenge");


            toCreate.Image = Convert.FromBase64String(toCreate.Base64Image);

            return _challenges.InsertOneAsync(toCreate);
        }

        public Task UpdateChallengeAsync(string id, Challenge toUpdate)
        {
            if (string.IsNullOrWhiteSpace(toUpdate.Id)) throw new Exception("The id must be provided in the body");
            if (string.IsNullOrWhiteSpace(toUpdate.Name)) throw new Exception("You must provide a name to the challenge");
            if (string.IsNullOrWhiteSpace(toUpdate.Description)) throw new Exception("You must provide a description to the challenge");

            toUpdate.Image = Convert.FromBase64String(toUpdate.Base64Image);

            return _challenges.ReplaceOneAsync(challenge => challenge.Id == id, toUpdate);
        }

        public Task DeleteChallengeAsync(string challengeId)
        {
            return _challenges.DeleteOneAsync(challenge => challenge.Id == challengeId);
        }

        /*
         * Game Stuff
         */
        public async Task SubmitChallengeForValidatiob(string userId, string challengeId, byte[] proofImage)
        {
            User user = await (await _users.FindAsync(databaseUser => databaseUser.Id == userId)).FirstOrDefaultAsync();
            if (user == null) throw new Exception("The user doesn't exist");

            Challenge challenge = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.Id == challengeId)).FirstOrDefaultAsync();
            if (challenge == null) throw new Exception("The challenge doesn't exist");
            if (challenge.IsForTeam) throw new Exception("You can't submit team challenge to validation");

            user.WaitingCallenges[challenge.Id] = proofImage;

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == user.Id, user);
        }

        public async Task ValidateChallengeForUser(string userId, string challengeId)
        {
            User user = await (await _users.FindAsync(databaseUser => databaseUser.Id == userId)).FirstOrDefaultAsync();
            if (user == null) throw new Exception("The user doesn't exist");

            Team userTeam = await GetTeamForUser(userId);
            if (userTeam == null) throw new Exception("The user appear to not belong to a team");

            Challenge challenge = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.Id == challengeId)).FirstOrDefaultAsync();
            if (challenge == null) throw new Exception("The challenge doesn't exist");
            if (challenge.IsForTeam) throw new Exception("You can't validate team challenge for individuals");

            user.WaitingCallenges.Remove(challenge.Id);

            // Since we can realize some challenges multiple times, we first 
            // check if their is a key to increment, otherwise we create it
            if (user.FinishedCallenges.ContainsKey(challengeId))
            {
                user.FinishedCallenges[challengeId] += 1;
            }
            else
            {
                user.FinishedCallenges[challengeId] = 1;
            }

            // We update the scores
            user.Score += challenge.Value;
            userTeam.Score += challenge.Value;

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == user.Id, user);
            await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == userTeam.Id, userTeam);
        }

        public async Task ValidateChallengeForTeam(string teamId, string challengeId)
        {
            Team team = await (await _teams.FindAsync(databaseTeam => databaseTeam.Id == teamId)).FirstOrDefaultAsync();
            if (team == null) throw new Exception("The team doesn't exisit");

            Challenge challenge = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.Id == challengeId)).FirstOrDefaultAsync();
            if (challenge == null) throw new Exception("The challenge doesn't exist");
            if (!challenge.IsForTeam) throw new Exception("You can't validate an individual challenge for team");

            // Since we can realize some challenges multiple times, we first 
            // check if their is a key to increment, otherwise we create it
            if (team.FinishedCallenges.ContainsKey(challengeId))
            {
                team.FinishedCallenges[challengeId] += 1;
            }
            else
            {
                team.FinishedCallenges[challengeId] = 1;
            }

            team.Score += challenge.Value;

            await _teams.ReplaceOneAsync(databaseTeam => databaseTeam.Id == team.Id, team);
        }

        /*
         * Utility stuff
         */
        private async Task<Team> GetTeamForUser(string userId)
        {
            var teams = await (await _teams.FindAsync(team => true)).ToListAsync();

            foreach (Team team in teams)
            {
                if (team.Members.Contains(userId))
                {
                    return team;
                }
            }

            return null;
        }
    }
}
