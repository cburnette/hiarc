using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models 
{
    public class UserCredentials
    {
        public string UserKey { get; set; }
        public string BearerToken { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}