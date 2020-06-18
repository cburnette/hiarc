using Xunit;
using Hiarc.Core.Models.Requests;
using System;
using Hiarc.Core.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace HiarcIntegrationTest.Tests
{
    public class GroupTests : HiarcTestBase
    {
        [Fact]
        public async void GroupCRUD()
        {
            var g1 = await _hiarc.CreateGroup();
            var fetchedGroup = await _hiarc.GetGroup(g1.Key);
            Assert.Equal(g1, fetchedGroup, new EntityComparer());

            var newName = "New Name";
            var newDescription = "New description";
            var updateRequest = new UpdateGroupRequest { Name=newName, Description=newDescription };
            var updatedGroup = await _hiarc.UpdateGroup(g1.Key, updateRequest);
            Assert.Equal(newName, updatedGroup.Name);
            Assert.Equal(newDescription, updatedGroup.Description);
            Assert.True(updatedGroup.ModifiedAt > updatedGroup.CreatedAt);

            updateRequest = new UpdateGroupRequest { Key="new key", Name=newName, Description=newDescription };
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.UpdateGroup(g1.Key, updateRequest));

            await _hiarc.DeleteGroup(g1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetGroup(g1.Key));
        }

        [Fact]
        public async void GetAllGroups()
        {
            var count = LARGE_ENTITY_COUNT;
            for(var i=0; i<count; i++)
            {
                await _hiarc.CreateGroup();
            }

            var allGroups = await _hiarc.GetAllGroups();
            Assert.Equal(count, allGroups.Count);
        }

        [Fact]
        public async void CreateGroupWithMetadata()
        {
            var md = TestMetadata;
            var g1 = await _hiarc.CreateGroup(md);
            var fetchedGroup = await _hiarc.GetGroup(g1.Key);
            
            Assert.Equal(g1, fetchedGroup, new EntityComparer());
            AssertMetadata(md, fetchedGroup.Metadata);
        }

        [Fact]
        public async void UpdateMetadata()
        {
            var md = TestMetadata;
            var g1 = await _hiarc.CreateGroup(md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", "support" },
                { "quotaCarrying", false },
                { "targetRate", 7.271 },
                { "level", 2 },
                { "startDate", DateTime.Parse("2020-02-25T22:33:50.134Z").ToUniversalTime() }
            };

            var request = new UpdateGroupRequest
            {
                Metadata = updatedMD
            };

            var updatedGroup = await _hiarc.UpdateGroup(g1.Key, request);
            AssertMetadata(updatedMD, updatedGroup.Metadata);
        }

        [Fact]
        public async void NullOutMetadata()
        {
            var md = TestMetadata;
            var g1 = await _hiarc.CreateGroup(md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", null },
                { "quotaCarrying", null }
            };

            var request = new UpdateGroupRequest
            {
                Metadata = updatedMD
            };

            var updatedGroup = await _hiarc.UpdateGroup(g1.Key, request);
            Assert.Equal(3, updatedGroup.Metadata.Keys.Count);

            updatedMD = new Dictionary<string, object>
            {
                { "targetRate", null },
                { "level", null },
                { "startDate", null }
            };

            request = new UpdateGroupRequest
            {
                Metadata = updatedMD
            };

            updatedGroup = await _hiarc.UpdateGroup(g1.Key, request);
            Assert.Null(updatedGroup.Metadata);
        }

        [Fact]
        public async void FindGroups()
        {
            var md = TestMetadata;
            var g1 = await _hiarc.CreateGroup(md);
            
            md["quotaCarrying"] = false;
            await _hiarc.CreateGroup(md);
            
            await _hiarc.CreateGroup();

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

            var request = new FindGroupsRequest { Query = query };
            var foundGroups = await _hiarc.FindGroups(request);
            Assert.Single(foundGroups);
            Assert.Equal(g1, foundGroups[0], new EntityComparer());
        }
    }
}
