using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Models
{
    public class Challenge
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        [BsonIgnore]
        public byte[] Image { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string ImageId { get; set; }
        public int Value { get; set; }
        public int NumberOfRepetitions { get; set; }

        public bool IsForTeam { get; set; }
        public bool IsVisible { get; set; }
    }
}
