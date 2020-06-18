using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace HiarcIntegrationTest
{
    [Collection("Hiarc")]
    public class HiarcTestBase
    {
        public const string AWS_EAST_STORAGE = "hiarc-aws-s3-east";
        public const string AWS_WEST_STORAGE = "hiarc-aws-s3-west";
        public const string AZURE_STORAGE_1 = "hiarc-azure-blob-1";
        public const string AZURE_STORAGE_2 = "hiarc-azure-blob-2";
        public const string GOOGLE_EAST_STORAGE = "hiarc-google-storage-east";
        public const string GOOGLE_WEST_STORAGE = "hiarc-google-storage-west";

        public const string TEST_FILE_TINY = "Test.txt";
        public const string TEST_FILE_TINY_NEW_VERSION = "NewVersionOfTest.txt";
        public const int LARGE_ENTITY_COUNT = 10;
        public const string ADMIN_NAME = Hiarc.Core.Admin.DEFAULT_ADMIN_NAME;

        protected readonly HiarcClient _hiarc;

        public HiarcTestBase()
        {
            _hiarc = new HiarcClient();
            var success = _hiarc.ResetDB().Result;
            Assert.True(success);
        }

        public Dictionary<string,object> TestMetadata
        {
            get
            {
                return new Dictionary<string, object>
                {
                    { "department", "sales" },
                    { "quotaCarrying", true },
                    { "targetRate", 4.234 },
                    { "level", 3 },
                    { "startDate", DateTime.Parse("2020-02-29T22:33:50.134Z").ToUniversalTime() }
                };
            }
        }

        public void AssertMetadata(Dictionary<string,object> expected, Dictionary<string,object> actual)
        {
            Assert.Equal(expected["department"], ((JsonElement)actual["department"]).ToString());
            Assert.Equal(expected["quotaCarrying"], ((JsonElement)actual["quotaCarrying"]).GetBoolean());
            Assert.Equal(expected["targetRate"], ((JsonElement)actual["targetRate"]).GetDouble());
            Assert.Equal(expected["level"], ((JsonElement)actual["level"]).GetInt32());
            Assert.Equal((DateTime)expected["startDate"], ((JsonElement)actual["startDate"]).GetDateTime());
        }

        public void AssertFileHash(string file1Path, string file2Path)
        {
            var file1Bytes = System.IO.File.ReadAllBytes(file1Path);
            var file2Bytes = System.IO.File.ReadAllBytes(file2Path);

            var sha1 = System.Security.Cryptography.SHA1.Create();
            var file1Hash = Convert.ToBase64String(sha1.ComputeHash(file1Bytes));
            var file2Hash = Convert.ToBase64String(sha1.ComputeHash(file2Bytes));

            Assert.Equal(file1Hash, file2Hash);
        }
    }
}