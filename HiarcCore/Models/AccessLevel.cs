using System.Collections.Generic;

namespace Hiarc.Core.Models
{ 
    public class AccessLevel
    {
        public const string CO_OWNER = "CO_OWNER";
        public const string READ_WRITE = "READ_WRITE";
        public const string READ_ONLY = "READ_ONLY";
        public const string UPLOAD_ONLY = "UPLOAD_ONLY";

        public static readonly List<string> VALID_ACCESS_LEVELS = new List<string> {CO_OWNER, READ_WRITE, READ_ONLY, UPLOAD_ONLY};

        public static bool IsValid(string accessLevel)
        {
            return VALID_ACCESS_LEVELS.Contains(accessLevel);
        }
    }

    public class AccessLevelGroup
    {
        public static readonly List<string> CoOwner = new List<string>() { AccessLevel.CO_OWNER };
        public static readonly List<string> ReadWriteOrHigher = new List<string>() { AccessLevel.READ_WRITE, AccessLevel.CO_OWNER };
        public static readonly List<string> ReadOnlyOrHigher = new List<string>() { AccessLevel.READ_ONLY, AccessLevel.READ_WRITE, AccessLevel.CO_OWNER};
        public static readonly List<string> UploadOnlyOrHigher = new List<string>() { AccessLevel.UPLOAD_ONLY, AccessLevel.READ_WRITE, AccessLevel.CO_OWNER };
    }
}