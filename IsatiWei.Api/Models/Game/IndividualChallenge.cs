﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Models.Game
{
    public class IndividualChallenge
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public int Value { get; set; }
        public bool WaitingValidation { get; set; }
        public int NumberLeft { get; set; }
    }
}
