using Xunit;
using Hiarc.Core.Models.Requests;
using System;
using Hiarc.Core.Models;
using System.Collections.Generic;

namespace HiarcIntegrationTest.Tests
{
    public class CollectionTests : HiarcTestBase
    {
        [Fact]
        public async void CollectionCRUD()
        {
            var c1 = await _hiarc.CreateCollection();
            var fetchedCollection = await _hiarc.GetCollection(c1.Key);
            Assert.Equal(c1, fetchedCollection, new EntityComparer());

            var newName = "New Name";
            var newDescription = "New description";
            var updateRequest = new UpdateCollectionRequest { Name=newName, Description=newDescription };
            var updatedCollection = await _hiarc.UpdateCollection(c1.Key, updateRequest);
            Assert.Equal(newName, updatedCollection.Name);
            Assert.Equal(newDescription, updatedCollection.Description);
            Assert.True(updatedCollection.ModifiedAt > updatedCollection.CreatedAt);

            updateRequest = new UpdateCollectionRequest { Key="new key", Name=newName, Description=newDescription };
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.UpdateCollection(c1.Key, updateRequest));

            await _hiarc.DeleteCollection(c1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetCollection(c1.Key));
        }

        [Fact]
        public async void CreateAccessAndUpdateCollectionAsUser()
        {
            var u1 = await _hiarc.CreateUser();
            var c1 = await _hiarc.CreateCollection(asUserKey: u1.Key);
            var fetchedCollection = await _hiarc.GetCollection(c1.Key, asUserKey: u1.Key);
            Assert.Equal(c1, fetchedCollection, new EntityComparer());

            var u2 = await _hiarc.CreateUser();
            await _hiarc.AddUserToCollection(c1.Key, u2.Key, AccessLevel.READ_ONLY);
            fetchedCollection = await _hiarc.GetCollection(c1.Key, asUserKey: u2.Key);
            Assert.Equal(c1, fetchedCollection, new EntityComparer());

            var u3 = await _hiarc.CreateUser();
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetCollection(c1.Key, asUserKey:u3.Key));

            fetchedCollection = await _hiarc.GetCollection(c1.Key); //admin use case
            Assert.Equal(c1, fetchedCollection, new EntityComparer());

            var newName = "New Name";
            var newDescription = "New description";
            var updateRequest = new UpdateCollectionRequest { Name=newName, Description=newDescription };
            var updatedCollection = await _hiarc.UpdateCollection(c1.Key, updateRequest, asUserKey: u1.Key);
            Assert.Equal(newName, updatedCollection.Name);
            Assert.Equal(newDescription, updatedCollection.Description);
            Assert.True(updatedCollection.ModifiedAt > updatedCollection.CreatedAt);

            newName = "New Name 2";
            newDescription = "New description 2";
            updateRequest = new UpdateCollectionRequest { Name=newName, Description=newDescription };
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.UpdateCollection(c1.Key, updateRequest, asUserKey: u2.Key));
            
            await _hiarc.AddUserToCollection(c1.Key, u3.Key, AccessLevel.READ_WRITE);
            newName = "New Name 3";
            newDescription = "New description 3";
            updateRequest = new UpdateCollectionRequest { Name=newName, Description=newDescription };
            updatedCollection = await _hiarc.UpdateCollection(c1.Key, updateRequest, asUserKey: u3.Key);
            Assert.Equal(newName, updatedCollection.Name);
            Assert.Equal(newDescription, updatedCollection.Description);
            Assert.True(updatedCollection.ModifiedAt > updatedCollection.CreatedAt);

            var u4 = await _hiarc.CreateUser();
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.UpdateCollection(c1.Key, updateRequest, asUserKey: u4.Key));

            newName = "New Name 3";
            newDescription = "New description 3";
            updateRequest = new UpdateCollectionRequest { Name=newName, Description=newDescription };
            updatedCollection = await _hiarc.UpdateCollection(c1.Key, updateRequest); //admin use case
            Assert.Equal(newName, updatedCollection.Name);
            Assert.Equal(newDescription, updatedCollection.Description);
            Assert.True(updatedCollection.ModifiedAt > updatedCollection.CreatedAt);
        }

        [Fact]
        public async void HierarchyTests()
        {
            var u1 = await _hiarc.CreateUser();
            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();
            var c4 = await _hiarc.CreateCollection();
            var c5 = await _hiarc.CreateCollection();
            var c6 = await _hiarc.CreateCollection();
            await _hiarc.AddChildCollection(c1.Key,c2.Key);
            await _hiarc.AddChildCollection(c2.Key,c3.Key);
            await _hiarc.AddChildCollection(c3.Key,c4.Key);
            await _hiarc.AddChildCollection(c4.Key,c5.Key);
            await _hiarc.AddChildCollection(c6.Key,c4.Key);

            await _hiarc.AddUserToCollection(c2.Key, u1.Key, AccessLevel.READ_ONLY);

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetCollection(c1.Key, asUserKey:u1.Key));

            var fetchedCollection = await _hiarc.GetCollection(c2.Key, asUserKey: u1.Key);
            Assert.Equal(c2, fetchedCollection, new EntityComparer());

            fetchedCollection = await _hiarc.GetCollection(c3.Key, asUserKey: u1.Key);
            Assert.Equal(c3, fetchedCollection, new EntityComparer());

            fetchedCollection = await _hiarc.GetCollection(c4.Key, asUserKey: u1.Key);
            Assert.Equal(c4, fetchedCollection, new EntityComparer());

            fetchedCollection = await _hiarc.GetCollection(c5.Key, asUserKey: u1.Key);
            Assert.Equal(c5, fetchedCollection, new EntityComparer());

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetCollection(c6.Key, asUserKey:u1.Key));
        }

        [Fact]
        public async void PreventCycle()
        {
            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();
            var c4 = await _hiarc.CreateCollection();
            var c5 = await _hiarc.CreateCollection();
            var c6 = await _hiarc.CreateCollection();
            var c7 = await _hiarc.CreateCollection();
            await _hiarc.AddChildCollection(c1.Key,c2.Key);
            await _hiarc.AddChildCollection(c2.Key,c3.Key);
            await _hiarc.AddChildCollection(c3.Key,c4.Key);
            await _hiarc.AddChildCollection(c4.Key,c5.Key);
            await _hiarc.AddChildCollection(c6.Key,c4.Key);
            await _hiarc.AddChildCollection(c6.Key,c7.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddChildCollection(c5.Key,c1.Key));
        }

        [Fact]
        public async void GetChildCollections()
        {
            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();
            var c4 = await _hiarc.CreateCollection();
            var c5 = await _hiarc.CreateCollection();
            var c6 = await _hiarc.CreateCollection();
            await _hiarc.AddChildCollection(c1.Key,c2.Key);
            await _hiarc.AddChildCollection(c1.Key,c3.Key);
            await _hiarc.AddChildCollection(c1.Key,c4.Key);
            await _hiarc.AddChildCollection(c2.Key,c5.Key);
            await _hiarc.AddChildCollection(c4.Key,c6.Key);

            var children = await _hiarc.GetChildrenForCollection(c1.Key);
            Assert.Equal(3, children.Count);
            Assert.Contains(c2, children, new EntityComparer());
            Assert.Contains(c3, children, new EntityComparer());
            Assert.Contains(c4, children, new EntityComparer());
            Assert.DoesNotContain(c5, children, new EntityComparer());
            Assert.DoesNotContain(c6, children, new EntityComparer());
        }

        [Fact]
        public async void GetItems()
        {
            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();
            var c4 = await _hiarc.CreateCollection();
            var c5 = await _hiarc.CreateCollection();
            var c6 = await _hiarc.CreateCollection();
            await _hiarc.AddChildCollection(c1.Key,c2.Key);
            await _hiarc.AddChildCollection(c1.Key,c3.Key);
            await _hiarc.AddChildCollection(c1.Key,c4.Key);
            await _hiarc.AddChildCollection(c2.Key,c5.Key);
            await _hiarc.AddChildCollection(c4.Key,c6.Key);

            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var f2 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var f3 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var f4 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var f5 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            await _hiarc.AddFileToCollection(c1.Key, f1.Key);
            await _hiarc.AddFileToCollection(c1.Key, f2.Key);
            await _hiarc.AddFileToCollection(c1.Key, f3.Key);
            await _hiarc.AddFileToCollection(c2.Key, f4.Key);
            await _hiarc.AddFileToCollection(c3.Key, f5.Key);

            var items = await _hiarc.GetItemsForCollection(c1.Key);

            var children = items.ChildCollections;
            Assert.Equal(3, children.Count);
            Assert.Contains(c2, children, new EntityComparer());
            Assert.Contains(c3, children, new EntityComparer());
            Assert.Contains(c4, children, new EntityComparer());
            Assert.DoesNotContain(c5, children, new EntityComparer());
            Assert.DoesNotContain(c6, children, new EntityComparer());

            var files = items.Files;
            Assert.Equal(3, files.Count);
            Assert.Contains(f1, files, new EntityComparer());
            Assert.Contains(f2, files, new EntityComparer());
            Assert.Contains(f3, files, new EntityComparer());
            Assert.DoesNotContain(f4, files, new EntityComparer());
            Assert.DoesNotContain(f5, files, new EntityComparer());

            await _hiarc.DeleteFile(f1.Key);
            await _hiarc.DeleteFile(f2.Key);
            await _hiarc.DeleteFile(f3.Key);
            await _hiarc.DeleteFile(f4.Key);
            await _hiarc.DeleteFile(f5.Key);
        }

        [Fact]
        public async void AddChildCollectionAsUser()
        {
            var u1 = await _hiarc.CreateUser();
            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddChildCollection(c1.Key,c2.Key, asUserKey:u1.Key));

            await _hiarc.AddUserToCollection(c1.Key,u1.Key, AccessLevel.READ_ONLY);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddChildCollection(c1.Key,c2.Key, asUserKey:u1.Key));

            var u2 = await _hiarc.CreateUser();
            await _hiarc.AddUserToCollection(c1.Key,u2.Key, AccessLevel.READ_WRITE);
            await _hiarc.AddChildCollection(c1.Key,c2.Key, asUserKey:u2.Key);
        }

        [Fact]
        public async void DeleteWithFile()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));   
            var c1 = await _hiarc.CreateCollection();
            await _hiarc.AddFileToCollection(c1.Key, f1.Key);

            await _hiarc.DeleteCollection(c1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetCollection(c1.Key));

            var result = await _hiarc.GetFile(f1.Key);
            Assert.Equal(f1, result, new EntityComparer());

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void DeleteAsUser()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));   
            var c1 = await _hiarc.CreateCollection();

            var u1 = await _hiarc.CreateUser();
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.DeleteCollection(c1.Key, asUserKey:u1.Key));

            await _hiarc.AddUserToCollection(c1.Key, u1.Key, AccessLevel.READ_ONLY);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.DeleteCollection(c1.Key, asUserKey:u1.Key));

            var u2 = await _hiarc.CreateUser();
            await _hiarc.AddUserToCollection(c1.Key, u2.Key, AccessLevel.READ_WRITE);
            await _hiarc.DeleteCollection(c1.Key, asUserKey:u2.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetCollection(c1.Key));

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void AddRemoveFiles()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY)); 
            var f2 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));  
            var f3 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var c1 = await _hiarc.CreateCollection();
            await _hiarc.AddFileToCollection(c1.Key, f1.Key);
            await _hiarc.AddFileToCollection(c1.Key, f2.Key);
            await _hiarc.AddFileToCollection(c1.Key, f3.Key);

            var files = await _hiarc.GetFilesForCollection(c1.Key);
            Assert.Equal(3, files.Count);

            await _hiarc.RemoveFileFromCollection(c1.Key, f3.Key);
            files = await _hiarc.GetFilesForCollection(c1.Key);
            Assert.Equal(2, files.Count);

            await _hiarc.DeleteFile(f1.Key);
            await _hiarc.DeleteFile(f2.Key);
            await _hiarc.DeleteFile(f3.Key);
        }

        [Fact]
        public async void AddRemoveFilesAsUser()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var u1 = await _hiarc.CreateUser();
            var c1 = await _hiarc.CreateCollection();

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddFileToCollection(c1.Key, f1.Key, asUserKey:u1.Key));

            await _hiarc.AddUserToCollection(c1.Key, u1.Key, AccessLevel.READ_ONLY);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddFileToCollection(c1.Key, f1.Key, asUserKey:u1.Key));

            var u2 = await _hiarc.CreateUser();
            await _hiarc.AddUserToCollection(c1.Key, u2.Key, AccessLevel.READ_WRITE);
            await _hiarc.AddFileToCollection(c1.Key, f1.Key, asUserKey:u2.Key);
            
            var files = await _hiarc.GetFilesForCollection(c1.Key, asUserKey:u2.Key);
            Assert.Single(files);

            var u3 = await _hiarc.CreateUser();
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.RemoveFileFromCollection(c1.Key, f1.Key, asUserKey:u3.Key));

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.RemoveFileFromCollection(c1.Key, f1.Key, asUserKey:u1.Key));

            await _hiarc.RemoveFileFromCollection(c1.Key, f1.Key, asUserKey:u2.Key);
            files = await _hiarc.GetFilesForCollection(c1.Key);
            Assert.Empty(files);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void GetFilesForUser()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY)); 
            var f2 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));  
            var f3 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var c1 = await _hiarc.CreateCollection();
            await _hiarc.AddFileToCollection(c1.Key, f1.Key);
            await _hiarc.AddFileToCollection(c1.Key, f2.Key);
            await _hiarc.AddFileToCollection(c1.Key, f3.Key);

            var u1 = await _hiarc.CreateUser();
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetFilesForCollection(c1.Key, asUserKey:u1.Key));

            await _hiarc.AddUserToCollection(c1.Key, u1.Key, AccessLevel.READ_ONLY);

            var files = await _hiarc.GetFilesForCollection(c1.Key, asUserKey:u1.Key);
            Assert.Equal(3, files.Count);

            var bearerToken = await _hiarc.CreateUserCredentials(u1.Key);
            files = await _hiarc.GetFilesForCollection(c1.Key, bearerToken: bearerToken.BearerToken);
            Assert.Equal(3, files.Count);

            await _hiarc.DeleteFile(f1.Key);
            await _hiarc.DeleteFile(f2.Key);
            await _hiarc.DeleteFile(f3.Key);
        }

        [Fact]
        public async void AddUsersAsUser()
        {
            var u1 = await _hiarc.CreateUser();
            var u2 = await _hiarc.CreateUser();
            var c1 = await _hiarc.CreateCollection();

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddUserToCollection(c1.Key,u2.Key,AccessLevel.READ_ONLY,asUserKey:u1.Key));

            await _hiarc.AddUserToCollection(c1.Key,u1.Key, AccessLevel.READ_ONLY);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddUserToCollection(c1.Key,u2.Key,AccessLevel.READ_ONLY,asUserKey:u1.Key));

            var u3 = await _hiarc.CreateUser();
            await _hiarc.AddUserToCollection(c1.Key,u3.Key,AccessLevel.READ_WRITE);

            await _hiarc.AddUserToCollection(c1.Key,u2.Key,AccessLevel.READ_ONLY,asUserKey:u3.Key);
        }

        [Fact]
        public async void AddGroupsAsUser()
        {
            var u1 = await _hiarc.CreateUser();
            var g1 = await _hiarc.CreateGroup();
            var c1 = await _hiarc.CreateCollection();

            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddGroupToCollection(c1.Key,g1.Key,AccessLevel.READ_ONLY,asUserKey:u1.Key));

            await _hiarc.AddUserToCollection(c1.Key,u1.Key, AccessLevel.READ_ONLY);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddGroupToCollection(c1.Key,g1.Key,AccessLevel.READ_ONLY,asUserKey:u1.Key));

            var u3 = await _hiarc.CreateUser();
            await _hiarc.AddUserToCollection(c1.Key,u3.Key,AccessLevel.READ_WRITE);

            await _hiarc.AddGroupToCollection(c1.Key,g1.Key,AccessLevel.READ_ONLY,asUserKey:u3.Key);
        }

        [Fact]
        public async void GetAllCollections()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY)); //make sure we ignore identity collections

            var count = LARGE_ENTITY_COUNT;
            for(var i=0; i<count; i++)
            {
                await _hiarc.CreateCollection();
            }

            var allCollections = await _hiarc.GetAllCollections();
            Assert.Equal(count, allCollections.Count);

            var u1 = await _hiarc.CreateUser();
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetAllCollections(asUserKey:u1.Key));

            var bearerToken = await _hiarc.CreateUserCredentials(u1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetAllCollections(bearerToken: bearerToken.BearerToken));

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void CreateCollectionWithMetadata()
        {
            var md = TestMetadata;
            var c1 = await _hiarc.CreateCollection(md);
            var fetchedCollection = await _hiarc.GetCollection(c1.Key);
            
            Assert.Equal(c1, fetchedCollection, new EntityComparer());
            AssertMetadata(md, fetchedCollection.Metadata);
        }

        [Fact]
        public async void UpdateMetadata()
        {
            var md = TestMetadata;
            var c1 = await _hiarc.CreateCollection(md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", "support" },
                { "quotaCarrying", false },
                { "targetRate", 7.271 },
                { "level", 2 },
                { "startDate", DateTime.Parse("2020-02-25T22:33:50.134Z").ToUniversalTime() }
            };

            var request = new UpdateCollectionRequest
            {
                Metadata = updatedMD
            };

            var updatedCollection = await _hiarc.UpdateCollection(c1.Key, request);
            AssertMetadata(updatedMD, updatedCollection.Metadata);
        }

        [Fact]
        public async void NullOutMetadata()
        {
            var md = TestMetadata;
            var c1 = await _hiarc.CreateCollection(md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", null },
                { "quotaCarrying", null }
            };

            var request = new UpdateCollectionRequest
            {
                Metadata = updatedMD
            };

            var updatedCollection = await _hiarc.UpdateCollection(c1.Key, request);
            Assert.Equal(3, updatedCollection.Metadata.Keys.Count);

            updatedMD = new Dictionary<string, object>
            {
                { "targetRate", null },
                { "level", null },
                { "startDate", null }
            };

            request = new UpdateCollectionRequest
            {
                Metadata = updatedMD
            };

            updatedCollection = await _hiarc.UpdateCollection(c1.Key, request);
            Assert.Null(updatedCollection.Metadata);
        }

        [Fact]
        public async void FindCollections()
        {
            var md = TestMetadata;
            var c1 = await _hiarc.CreateCollection(md);
            
            md["quotaCarrying"] = false;
            await _hiarc.CreateCollection(md);
            
            await _hiarc.CreateCollection();

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

            var request = new FindCollectionsRequest { Query = query };
            var foundCollections = await _hiarc.FindCollections(request);
            Assert.Single(foundCollections);
            Assert.Equal(c1, foundCollections[0], new EntityComparer());
        }
    }
}
