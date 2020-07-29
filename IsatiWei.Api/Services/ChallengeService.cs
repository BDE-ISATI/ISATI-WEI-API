using IsatiWei.Api.Models;
using IsatiWei.Api.Models.Game;
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
    public class ChallengeService
    {
        private readonly IMongoCollection<Team> _teams;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Challenge> _challenges;

        private readonly GridFSBucket _gridFS;

        public ChallengeService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _teams = database.GetCollection<Team>("teams");
            _users = database.GetCollection<User>("users");
            _challenges = database.GetCollection<Challenge>("challenges");

            _gridFS = new GridFSBucket(database);
        }

        public async Task<Challenge> GetChallengeAsync(string challengeId)
        {
            var challenge = await _challenges.FindAsync(databaseChallenge => databaseChallenge.Id == challengeId);

            return await challenge.FirstOrDefaultAsync();
        }

        public async Task<List<Challenge>> GetChallengesAsync()
        {
            var challenges = await _challenges.FindAsync(databaseChallenge => true);

            return await challenges.ToListAsync();
        }

        public async Task<byte[]> GetChallengeImage(string challengeId)
        {
            var challenge = await GetChallengeAsync(challengeId);
            if (challenge == null)
            {
                return null;
            }

            return await _gridFS.DownloadAsBytesAsync(new ObjectId(challenge.ImageId));

        }

        public async Task<List<IndividualChallenge>> GetChallengeForPlayerAsync(string playerId)
        {
            var challenges = await (await _challenges.FindAsync(databaseChallenge => !databaseChallenge.IsForTeam && databaseChallenge.IsVisible)).ToListAsync();

            var player = await (await _users.FindAsync(databaseUser => databaseUser.Id == playerId)).FirstOrDefaultAsync();
            if (player == null) return new List<IndividualChallenge>();

            List<IndividualChallenge> result = new List<IndividualChallenge>();

            foreach (var challenge in challenges)
            {
                result.Add(new IndividualChallenge()
                {
                    Id = challenge.Id,
                    ImageId = challenge.ImageId,
                    Name = challenge.Name,
                    Description = challenge.Description,
                    Value = challenge.Value,
                    WaitingValidation = player.WaitingCallenges.ContainsKey(challenge.Id),
                    NumberLeft = (player.FinishedCallenges.ContainsKey(challenge.Id)) ? challenge.NumberOfRepetitions - player.FinishedCallenges[challenge.Id] : challenge.NumberOfRepetitions

                });
            }

            return result;
        }

        public async Task<List<TeamChallenge>> GetChallengeForTeamAsync(string teamId)
        {
            var challenges = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.IsForTeam && databaseChallenge.IsVisible)).ToListAsync();

            var team = await (await _teams.FindAsync(databaseTeam => databaseTeam.Id == teamId)).FirstOrDefaultAsync();
            if (team == null) return new List<TeamChallenge>();

            List<TeamChallenge> result = new List<TeamChallenge>();

            foreach (var challenge in challenges)
            {
                result.Add(new TeamChallenge()
                {
                    Id = challenge.Id,
                    ImageId = challenge.ImageId,
                    Name = challenge.Name,
                    Description = challenge.Description,
                    Value = challenge.Value,
                    NumberLeft = (team.FinishedCallenges.ContainsKey(challenge.Id)) ? challenge.NumberOfRepetitions - team.FinishedCallenges[challenge.Id] : challenge.NumberOfRepetitions
                });
            }

            return result;
        }

        /*
         * Edition stuff
         */
        public async Task<Challenge> CreateChallengeAsync(Challenge toCreate)
        {
            if (string.IsNullOrWhiteSpace(toCreate.Name)) throw new Exception("You must provide a name to the challenge");
            if (string.IsNullOrWhiteSpace(toCreate.Description)) throw new Exception("You must provide a description to the challenge");

            await _challenges.InsertOneAsync(toCreate);

            var challengeImage = await _gridFS.UploadFromBytesAsync($"challenge_{toCreate.Id}", toCreate.Image);
            toCreate.Image = null;
            toCreate.ImageId = challengeImage.ToString();

            await _challenges.ReplaceOneAsync(databaseChallenge => databaseChallenge.Id == toCreate.Id, toCreate);

            return toCreate;
        }

        public async Task UpdateChallengeAsync(Challenge toUpdate)
        {
            if (string.IsNullOrWhiteSpace(toUpdate.Id)) throw new Exception("The id must be provided in the body");
            if (string.IsNullOrWhiteSpace(toUpdate.Name)) throw new Exception("You must provide a name to the challenge");
            if (string.IsNullOrWhiteSpace(toUpdate.Description)) throw new Exception("You must provide a description to the challenge");

            var oldChallenge = await GetChallengeAsync(toUpdate.Id);
            if (oldChallenge == null) throw new Exception("The challenge you wan't to update doesn't exist");

            if (toUpdate.ImageId == "modified")
            {
                // If we don't do it manally old images are never deleted
                if (!string.IsNullOrWhiteSpace(oldChallenge.ImageId))
                {
                    await _gridFS.DeleteAsync(new ObjectId(oldChallenge.ImageId));
                }

                var challengeImage = await _gridFS.UploadFromBytesAsync($"challenge_{toUpdate.Id}", toUpdate.Image);
                toUpdate.Image = null;
                toUpdate.ImageId = challengeImage.ToString();
            }
            else
            {
                toUpdate.ImageId = oldChallenge.ImageId;
            }

            await _challenges.ReplaceOneAsync(challenge => challenge.Id == toUpdate.Id, toUpdate);
        }

        public async Task DeleteChallengeAsync(string challengeId)
        {
            var oldChallenge = await GetChallengeAsync(challengeId);
            if (oldChallenge != null)
            {
                await _gridFS.DeleteAsync(new ObjectId(oldChallenge.ImageId));
            }

            await _challenges.DeleteOneAsync(challenge => challenge.Id == challengeId);
        }

        /*
         * Game Stuff
         */
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
                if (player.WaitingCallenges == null)
                {
                    continue;
                }

                List<string> keys = player.WaitingCallenges.Keys.ToList();

                var challenges = await (await _challenges.FindAsync(databaseChallenge => keys.Contains(databaseChallenge.Id))).ToListAsync();

                foreach (var challenge in challenges)
                {
                    result.Add(new WaitingChallenge()
                    {
                        Id = challenge.Id,
                        ImageId = challenge.ImageId,
                        ValidatorId = player.Id,
                        ValidatorName = $"{player.FirstName} {player.LastName}",
                        Name = challenge.Name,
                        Description = challenge.Description
                    });
                }
            }

            return result;
        }

        public async Task<List<IndividualChallenge>> GetDoneChallenges(string playerId)
        {
            var player = await (await _users.FindAsync(databaseUser => databaseUser.Id == playerId)).FirstOrDefaultAsync();
            if (player == null) return null;

            List<string> challengesIds = player.FinishedCallenges.Keys.ToList();

            var challenges = await (await _challenges.FindAsync(databaseChallenge => challengesIds.Contains(databaseChallenge.Id))).ToListAsync();

            List<IndividualChallenge> result = new List<IndividualChallenge>();

            foreach (var challenge in challenges)
            {
                result.Add(new IndividualChallenge()
                {
                    Id = challenge.Id,
                    ImageId = challenge.ImageId,
                    Name = challenge.Name,
                    Description = challenge.Description,
                    Value = challenge.Value,
                    WaitingValidation = player.WaitingCallenges.ContainsKey(challenge.Id),
                    NumberLeft = (player.FinishedCallenges.ContainsKey(challenge.Id)) ? challenge.NumberOfRepetitions - player.FinishedCallenges[challenge.Id] : challenge.NumberOfRepetitions

                });
            }

            return result;
        }

        public async Task<byte[]> GetProofImage(String challengeId, String playerId)
        {
            var player = await (await _users.FindAsync(databaseUser => databaseUser.Id == playerId)).FirstOrDefaultAsync();
            if (player == null) return null;
            if (!player.WaitingCallenges.ContainsKey(challengeId)) return null;

            var imageId = player.WaitingCallenges[challengeId];

            return await _gridFS.DownloadAsBytesAsync(imageId);
            //return player.WaitingCallenges[challengeId];
        }

        public async Task SubmitChallengeForValidationAsync(string userId, string challengeId, byte[] proofImage)
        {
            User user = await (await _users.FindAsync(databaseUser => databaseUser.Id == userId)).FirstOrDefaultAsync();
            if (user == null) throw new Exception("The user doesn't exist");
            if (user.Role != UserRoles.Default) throw new Exception("Only players can submit validations");
            if (user.WaitingCallenges.ContainsKey(challengeId)) throw new Exception("This challenge is already waiting for validation");

            Challenge challenge = await (await _challenges.FindAsync(databaseChallenge => databaseChallenge.Id == challengeId)).FirstOrDefaultAsync();
            if (challenge == null) throw new Exception("The challenge doesn't exist");
            if (challenge.IsForTeam) throw new Exception("You can't submit team challenge to validation");

            //user.WaitingCallenges[challenge.Id] = proofImage;
            var proofImageId = await _gridFS.UploadFromBytesAsync($"proof_{userId}{challengeId}", proofImage);
            user.WaitingCallenges[challenge.Id] = proofImageId;

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
