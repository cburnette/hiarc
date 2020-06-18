using Xunit;
using Hiarc.Core.Models.Requests;
using System;
using System.Threading.Tasks;
using Hiarc.Core.Models;
using System.Collections.Generic;

namespace HiarcIntegrationTest.Tests
{
    public class RetentionPolicyTests : HiarcTestBase
    {
        [Fact]
        public async void RetentionPolicyCRUD()
        {
            var rp1 = await _hiarc.CreateRetentionPolicy(60);
            var fetchedRetentionPolicy = await _hiarc.GetRetentionPolicy(rp1.Key);
            Assert.Equal(rp1, fetchedRetentionPolicy, new EntityComparer());
            Assert.Equal((uint)60, fetchedRetentionPolicy.Seconds);

            var newName = "New Name";
            var newDescription = "New description";
            var updateRequest = new UpdateRetentionPolicyRequest { Name=newName, Description=newDescription };
            var updatedRetentionPolicy = await _hiarc.UpdateRetentionPolicy(rp1.Key, updateRequest);
            Assert.Equal(newName, updatedRetentionPolicy.Name);
            Assert.Equal(newDescription, updatedRetentionPolicy.Description);
            Assert.True(updatedRetentionPolicy.ModifiedAt > updatedRetentionPolicy.CreatedAt);

            updateRequest = new UpdateRetentionPolicyRequest { Key="new key", Name=newName, Description=newDescription };
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.UpdateRetentionPolicy(rp1.Key, updateRequest));
        }

        [Fact]
        public async void ApplyToFile()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var rp1 = await _hiarc.CreateRetentionPolicy(1);
            await _hiarc.AddRetentionPolicyToFile(rp1.Key, f1.Key);

            var rp2 = await _hiarc.CreateRetentionPolicy(3);
            await _hiarc.AddRetentionPolicyToFile(rp2.Key, f1.Key);

            var retentionPoliciesApplications = await _hiarc.GetFileRetentionPolicies(f1.Key);
            Assert.True(retentionPoliciesApplications[0].AppliedAt < retentionPoliciesApplications[1].AppliedAt);
            Assert.Equal(rp1.Key, retentionPoliciesApplications[0].RetentionPolicy.Key);
            Assert.Equal((uint)1, retentionPoliciesApplications[0].RetentionPolicy.Seconds);
            Assert.Equal(rp2.Key, retentionPoliciesApplications[1].RetentionPolicy.Key);
            Assert.Equal((uint)3, retentionPoliciesApplications[1].RetentionPolicy.Seconds);

            Console.WriteLine("Waiting 4 seconds...");
            await Task.Delay(4000);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void DeleteFileSinglePolicy()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var rp1 = await _hiarc.CreateRetentionPolicy(2);
            await _hiarc.AddRetentionPolicyToFile(rp1.Key, f1.Key);

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.DeleteFile(f1.Key));

            Console.WriteLine("Waiting 3 seconds...");
            await Task.Delay(3000);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void DeleteFileMultiplePolicies()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var rp1 = await _hiarc.CreateRetentionPolicy(2);
            await _hiarc.AddRetentionPolicyToFile(rp1.Key, f1.Key);

            var rp2 = await _hiarc.CreateRetentionPolicy(5);
            await _hiarc.AddRetentionPolicyToFile(rp2.Key, f1.Key);

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.DeleteFile(f1.Key));

            Console.WriteLine("Waiting 3 seconds...");
            await Task.Delay(3000);

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.DeleteFile(f1.Key));

            await Task.Delay(3000);
            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void UpdateRetentionPeriod()
        {           
            var rp1 = await _hiarc.CreateRetentionPolicy(RetentionPolicy.RETENTION_PERIOD_MONTH);
            var updateRequest = new UpdateRetentionPolicyRequest { Seconds=RetentionPolicy.RETENTION_PERIOD_DAY };

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.UpdateRetentionPolicy(rp1.Key, updateRequest));

            updateRequest = new UpdateRetentionPolicyRequest { Seconds=RetentionPolicy.RETENTION_PERIOD_MAX };
            var updatedRetentionPolicy = await _hiarc.UpdateRetentionPolicy(rp1.Key, updateRequest);
            Assert.Equal(RetentionPolicy.RETENTION_PERIOD_MAX, updatedRetentionPolicy.Seconds);
        }

        [Fact]
        public async void DeleteFileWithUpdatedRetentionPeriod()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var rp1 = await _hiarc.CreateRetentionPolicy(3);
            await _hiarc.AddRetentionPolicyToFile(rp1.Key, f1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.DeleteFile(f1.Key));

            var updateRequest = new UpdateRetentionPolicyRequest { Seconds=10};
            await _hiarc.UpdateRetentionPolicy(rp1.Key, updateRequest);
            Console.WriteLine("Waiting 5 seconds...");
            await Task.Delay(5000);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.DeleteFile(f1.Key));

            Console.WriteLine("Waiting 7 seconds...");
            await Task.Delay(7000);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void CreateWithMetadata()
        {
            var md = TestMetadata;
            var rp1 = await _hiarc.CreateRetentionPolicy(60, md);
            var fetchedRetentionPolicy = await _hiarc.GetRetentionPolicy(rp1.Key);
            
            Assert.Equal(rp1, fetchedRetentionPolicy, new EntityComparer());
            AssertMetadata(md, fetchedRetentionPolicy.Metadata);
        }

        [Fact]
        public async void UpdateMetadata()
        {
            var md = TestMetadata;
            var rp1 = await _hiarc.CreateRetentionPolicy(60, md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", "support" },
                { "quotaCarrying", false },
                { "targetRate", 7.271 },
                { "level", 2 },
                { "startDate", DateTime.Parse("2020-02-25T22:33:50.134Z").ToUniversalTime() }
            };

            var request = new UpdateRetentionPolicyRequest
            {
                Metadata = updatedMD
            };

            var updatedPolicy = await _hiarc.UpdateRetentionPolicy(rp1.Key, request);
            AssertMetadata(updatedMD, updatedPolicy.Metadata);
        }

        [Fact]
        public async void GetAllRetentionPolicies()
        {
            var count = LARGE_ENTITY_COUNT;
            for(var i=0; i<count; i++)
            {
                await _hiarc.CreateRetentionPolicy(60);
            }

            var allPolicies = await _hiarc.GetAllRetentionPolicies();
            Assert.Equal(count, allPolicies.Count);
        }

        [Fact]
        public async void NullOutMetadata()
        {
            var md = TestMetadata;
            var rp1 = await _hiarc.CreateRetentionPolicy(60, md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", null },
                { "quotaCarrying", null }
            };

            var request = new UpdateRetentionPolicyRequest
            {
                Metadata = updatedMD
            };

            var updatedPolicy = await _hiarc.UpdateRetentionPolicy(rp1.Key, request);
            Assert.Equal(3, updatedPolicy.Metadata.Keys.Count);

            updatedMD = new Dictionary<string, object>
            {
                { "targetRate", null },
                { "level", null },
                { "startDate", null }
            };

            request = new UpdateRetentionPolicyRequest
            {
                Metadata = updatedMD
            };

            updatedPolicy = await _hiarc.UpdateRetentionPolicy(rp1.Key, request);
            Assert.Null(updatedPolicy.Metadata);
        }

        [Fact]
        public async void FindRetentionPolicies()
        {
            var md = TestMetadata;
            var rp1 = await _hiarc.CreateRetentionPolicy(60, md);
            
            md["quotaCarrying"] = false;
            await _hiarc.CreateRetentionPolicy(60, md);
            
            await _hiarc.CreateRetentionPolicy(60);

            var query = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "prop", "department" },
                    { "op", "starts with" },
                    { "value", "sal" }
                },
                    new Dictionary<string, object>
                {
                    { "bool", "and" }
                },
                    new Dictionary<string, object>
                {
                    { "parens", "(" }
                },
                    new Dictionary<string, object>
                {
                    { "prop", "targetRate" },
                    { "op", ">=" },
                    { "value", 4.22 }
                },
                    new Dictionary<string, object>
                {
                    { "bool", "and" }
                },
                    new Dictionary<string, object>
                {
                    { "prop", "quotaCarrying" },
                    { "op", "=" },
                    { "value", true }
                },
                    new Dictionary<string, object>
                {
                    { "parens", ")" }
                }
            };

            var request = new FindRetentionPoliciesRequest { Query = query };
            var foundPolicies = await _hiarc.FindRetentionPolicies(request);
            Assert.Single(foundPolicies);
            Assert.Equal(rp1, foundPolicies[0], new EntityComparer());
        }
    }
}