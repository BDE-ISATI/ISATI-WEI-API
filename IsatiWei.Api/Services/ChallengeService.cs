using IsatiWei.Api.Models;
using IsatiWei.Api.Models.Game;
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

        public async Task<List<IndividualChallenge>> GetChallengeForPlayerAsync(string playerId)
        {
            var challenges = await (await _challenges.FindAsync(databaseChallenge => !databaseChallenge.IsForTeam)).ToListAsync();

            var player = await (await _users.FindAsync(databaseUser => databaseUser.Id == playerId)).FirstOrDefaultAsync();
            if (player == null) return new List<IndividualChallenge>();

            List<IndividualChallenge> result = new List<IndividualChallenge>();

            foreach (var challenge in challenges)
            {
                result.Add(new IndividualChallenge()
                {
                    Id = challenge.Id,
                    Name = challenge.Name,
                    Description = challenge.Description,
                    Base64Image = Convert.ToBase64String(challenge.Image),
                    Value = challenge.Value,
                    WaitingValidation = player.WaitingCallenges.ContainsKey(challenge.Id),
                    NumberLeft = challenge.NumberOfRepetitions - player.FinishedCallenges[challenge.Id]

                });
            }

            return result;
        }

        public async Task<List<TeamChallenge>> GetChallengeForTeamAsync(string teamId)
        {
            var challenges = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.IsForTeam)).ToListAsync();

            var team = await (await _teams.FindAsync(databaseTeam => databaseTeam.Id == teamId)).FirstOrDefaultAsync();
            if (team == null) return new List<TeamChallenge>();

            List<TeamChallenge> result = new List<TeamChallenge>();

            foreach (var challenge in challenges)
            {
                result.Add(new TeamChallenge()
                {
                    Id = challenge.Id,
                    Name = challenge.Name,
                    Description = challenge.Description,
                    Base64Image = Convert.ToBase64String(challenge.Image),
                    Value = challenge.Value,
                    NumberLeft = challenge.NumberOfRepetitions - team.FinishedCallenges[challenge.Id]

                });
            }

            return result;
        }

        public async Task<List<WaitingChallenge>> GetWaitingChallenges(string captainId)
        {
            var captain = await (await _users.FindAsync(databaseUser => databaseUser.Id == captainId)).FirstOrDefaultAsync();
            if (captain == null) return null;

            // Admin can see all waiting challenges, captain only his team's one
            List<User> players;
            if (captain.Role == UserRoles.Administrator)
            {
                players = await (await _users.FindAsync(databaseUser => databaseUser.Role == UserRoles.Default)).ToListAsync();
            }
            else
            {
                Team team = await (await _teams.FindAsync(databaseTeam => databaseTeam.CaptainId == captainId)).FirstOrDefaultAsync();
                if (team == null) return null;

                players = await (await _users.FindAsync(databaseUser => team.Members.Contains(databaseUser.Id))).ToListAsync();
            }

            List<WaitingChallenge> result = new List<WaitingChallenge>();

            foreach (var player in players)
            {
                var challenges = await (await _challenges.FindAsync(databaseChallenge => player.WaitingCallenges.ContainsKey(databaseChallenge.Id))).ToListAsync();

                foreach (var challenge in challenges)
                {
                    result.Add(new WaitingChallenge()
                    {
                        Id = challenge.Id,
                        ValidatorId = player.Id,
                        Name = challenge.Name,
                        Description = challenge.Description,
                        Base64Image = Convert.ToBase64String(challenge.Image),
                        Base64ProofImage = Convert.ToBase64String(player.WaitingCallenges[challenge.Id])
                    });
                }
            }

            return result;
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
        public async Task SubmitChallengeForValidationAsync(string userId, string challengeId, byte[] proofImage)
        {
            User user = await (await _users.FindAsync(databaseUser => databaseUser.Id == userId)).FirstOrDefaultAsync();
            if (user == null) throw new Exception("The user doesn't exist");

            Challenge challenge = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.Id == challengeId)).FirstOrDefaultAsync();
            if (challenge == null) throw new Exception("The challenge doesn't exist");
            if (challenge.IsForTeam) throw new Exception("You can't submit team challenge to validation");

            user.WaitingCallenges[challenge.Id] = proofImage;

            await _users.ReplaceOneAsync(databaseUser => databaseUser.Id == user.Id, user);
        }

        public async Task ValidateChallengeForUserAsync(string userId, string challengeId)
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

        public async Task ValidateChallengeForTeamAsync(string teamId, string challengeId)
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
