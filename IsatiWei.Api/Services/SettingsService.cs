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
    public class SettingsService
    {
        private readonly IMongoCollection<GameSettings> _settings;

        public SettingsService(IMongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            
            _settings = database.GetCollection<GameSettings>("game_settings");
        }

        public async Task<GameSettings> GetSettingsAsync()
        {
            GameSettings settings = await (await _settings.FindAsync(databaseGameSettings => true)).FirstOrDefaultAsync();

            if (settings == null)
            {
                return new GameSettings()
                {
                    IsUsersRankingVisible = false,
                    IsTeamsRankingVisible = false
                };
            }

            return settings;
        }

        public async Task ToggleUsersRankingVisibility()
        {
            GameSettings settings = await (await _settings.FindAsync(databaseGameSettings => true)).FirstOrDefaultAsync();

            if (settings == null)
            {
                var newSettings = new GameSettings()
                {
                    IsUsersRankingVisible = true,
                    IsTeamsRankingVisible = false
                };
                await _settings.InsertOneAsync(newSettings);
            }
            else
            {
                settings.IsUsersRankingVisible = !settings.IsUsersRankingVisible;
                await _settings.ReplaceOneAsync(databaseGameSettings => databaseGameSettings.Id == settings.Id, settings);
            }
        }

        public async Task ToggleTeamsRankingVisibility()
        {
            GameSettings settings = await (await _settings.FindAsync(databaseGameSettings => true)).FirstOrDefaultAsync();

            if (settings == null)
            {
                var newSettings = new GameSettings()
                {
                    IsUsersRankingVisible = false,
                    IsTeamsRankingVisible = true
                };
                await _settings.InsertOneAsync(newSettings);
            }
            else
            {
                settings.IsTeamsRankingVisible = !settings.IsTeamsRankingVisible;
                await _settings.ReplaceOneAsync(databaseGameSettings => databaseGameSettings.Id == settings.Id, settings);
            }
        }
    }
}
