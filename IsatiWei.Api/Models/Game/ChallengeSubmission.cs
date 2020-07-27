using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IsatiWei.Api.Models.Game
{
    public class ChallengeSubmission
    {
        public string ValidatorId { get; set; }
        public byte[] ProofImage { get; set; }
    }
}
