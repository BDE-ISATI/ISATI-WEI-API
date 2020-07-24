using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Models.Game
{
    public class WaitingChallenge
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string ValidatorId { get; set; } // Can be both a player or a team ID
        public string Name { get; set; }
        public string Description { get; set; }
        [BsonIgnore]
        public string Base64Image { get; set; }
        [BsonIgnore]
        public string Base64ProofImage { get; set; }
    }
}
