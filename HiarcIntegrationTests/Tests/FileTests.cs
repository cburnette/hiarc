using Xunit;
using Hiarc.Core.Models.Requests;
using System;
using System.Linq;
using Hiarc.Core.Models;
using System.Collections.Generic;
using Hiarc.Core.Storage;

namespace HiarcIntegrationTest.Tests
{
    public class FileTests : HiarcTestBase
    {
        [Fact]
        public async void FileCRUD()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var fetchedFile = await _hiarc.GetFile(f1.Key);
            Assert.Equal(f1, fetchedFile, new EntityComparer());

            var newName = "New Name.txt";
            var newDescription = "New description";
            var updateRequest = new UpdateFileRequest { Name=newName, Description=newDescription };
            var updatedFile = await _hiarc.UpdateFile(f1.Key, updateRequest);
            Assert.Equal(newName, updatedFile.Name);
            Assert.Equal(newDescription, updatedFile.Description);
            Assert.True(updatedFile.ModifiedAt > updatedFile.CreatedAt);

            updateRequest = new UpdateFileRequest { Key="new key", Name=newName, Description=newDescription };
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.UpdateFile(f1.Key, updateRequest));

            await _hiarc.DeleteFile(f1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetFile(f1.Key));
        }

        [Fact]
        public async void FileVersions()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            Assert.Equal(1, f1.VersionCount);

            var fileWithNewVersion = await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION));
            Assert.True(fileWithNewVersion.ModifiedAt > f1.ModifiedAt);
            Assert.Equal(2, fileWithNewVersion.VersionCount);

            var fileWithAnotherNewVersion = await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION));
            Assert.True(fileWithAnotherNewVersion.ModifiedAt > fileWithNewVersion.ModifiedAt);
            Assert.Equal(3, fileWithAnotherNewVersion.VersionCount);

            var fileVersions = await _hiarc.GetFileVersions(f1.Key);
            Assert.Equal(3, fileVersions.Count);
            Assert.True(fileVersions[0].CreatedAt < fileVersions[2].CreatedAt);

            await _hiarc.DeleteFile(f1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetFile(f1.Key));
        }

        [Fact]
        public async void FileVersionsCreatedBy()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            Assert.Equal(1, f1.VersionCount);

            var u1 = await _hiarc.CreateUser();
            await _hiarc.AddUserToFile(u1.Key, f1.Key, AccessLevel.READ_WRITE);

            var fileWithNewVersion = await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION), asUserKey: u1.Key);
            Assert.True(fileWithNewVersion.ModifiedAt > f1.ModifiedAt);
            Assert.Equal(2, fileWithNewVersion.VersionCount);

            var fileVersions = await _hiarc.GetFileVersions(f1.Key);
            Assert.Equal(2, fileVersions.Count);
            Assert.True(fileVersions.First().CreatedAt < fileVersions.Last().CreatedAt);
            Assert.Equal(ADMIN_NAME, fileVersions.First().CreatedBy);
            Assert.Equal(u1.Key, fileVersions.Last().CreatedBy);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void FileVersionsRespectAccessLevel()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            Assert.Equal(1, f1.VersionCount);

            var u1 = await _hiarc.CreateUser();
            await _hiarc.AddUserToFile(u1.Key, f1.Key, AccessLevel.READ_ONLY);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION), asUserKey: u1.Key));

            var u2 = await _hiarc.CreateUser();
            await _hiarc.AddUserToFile(u2.Key, f1.Key, AccessLevel.UPLOAD_ONLY);
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION), asUserKey: u2.Key);

            var u3 = await _hiarc.CreateUser();
            await _hiarc.AddUserToFile(u3.Key, f1.Key, AccessLevel.READ_WRITE);
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION), asUserKey: u3.Key);

            var u4 = await _hiarc.CreateUser();
            await _hiarc.AddUserToFile(u4.Key, f1.Key, AccessLevel.CO_OWNER);
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION), asUserKey: u4.Key);

            var fileVersions = await _hiarc.GetFileVersions(f1.Key);
            Assert.Equal(4, fileVersions.Count);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void FileVersionStaysInLastStorageService()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY), storageService: AWS_EAST_STORAGE);
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION));
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION), storageService: AZURE_STORAGE_1);
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION));

            var fileVersions = await _hiarc.GetFileVersions(f1.Key);
            Assert.Equal(4, fileVersions.Count);
            Assert.Equal(AWS_EAST_STORAGE, fileVersions[0].StorageService);
            Assert.Equal(AWS_EAST_STORAGE, fileVersions[1].StorageService);
            Assert.Equal(AZURE_STORAGE_1, fileVersions[2].StorageService);
            Assert.Equal(AZURE_STORAGE_1, fileVersions[3].StorageService);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void GetAllowedFiles()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var f2 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var f3 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var f4 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            var u1 = await _hiarc.CreateUser();

            await _hiarc.AddUserToFile(u1.Key, f1.Key, AccessLevel.READ_ONLY);

            var c1 = await _hiarc.CreateCollection();
            await _hiarc.AddFileToCollection(c1.Key, f2.Key);
            await _hiarc.AddUserToCollection(c1.Key, u1.Key, accessLevel: AccessLevel.READ_ONLY);

            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();
            await _hiarc.AddChildCollection(c2.Key, c3.Key);
            await _hiarc.AddFileToCollection(c3.Key, f3.Key);
            await _hiarc.AddUserToCollection(c2.Key, u1.Key, accessLevel: AccessLevel.READ_ONLY);

            var requestedFiles = new List<string> {f1.Key, f2.Key, f3.Key, f4.Key};
            var allowedFiles = await _hiarc.FilterAllowedFiles(requestedFiles, asUserKey: u1.Key);

            Assert.Contains(f1.Key, allowedFiles);
            Assert.Contains(f2.Key, allowedFiles);
            Assert.Contains(f3.Key, allowedFiles);
            Assert.DoesNotContain(f4.Key, allowedFiles);

            await _hiarc.DeleteFile(f1.Key);
            await _hiarc.DeleteFile(f2.Key);
            await _hiarc.DeleteFile(f3.Key);
            await _hiarc.DeleteFile(f4.Key);
        }

        [Fact]
        public async void DownloadFile()
        {
            foreach(var ss in All_STORAGE_SERVICES)
            {
                var originalPath = _hiarc.BuildPath(TEST_FILE_TINY);
                var downloadedPath = _hiarc.BuildPath($"Downloaded-{TEST_FILE_TINY}");

                var f1 = await _hiarc.CreateFile(originalPath, storageService: ss);
                await _hiarc.DownloadFile(f1.Key, downloadedPath);      
                this.AssertFileHash(originalPath, downloadedPath);
                
                System.IO.File.Delete(downloadedPath);
                await _hiarc.DeleteFile(f1.Key);
            }       
        }

        [Fact]
        public async void DirectDownloadUrl()
        {
            var originalPath = _hiarc.BuildPath(TEST_FILE_TINY);

            var f1 = await _hiarc.CreateFile(originalPath);
            var directDownloadUrl = await _hiarc.GetFileDirectDownload(f1.Key);
            Assert.Equal(f1.Key, directDownloadUrl.Key); 
            Assert.StartsWith("https",directDownloadUrl.DirectDownloadUrl);
            var now = DateTime.UtcNow;
            Assert.True(directDownloadUrl.ExpiresAt > now);
            Assert.True(directDownloadUrl.ExpiresAt < now.AddSeconds(IStorageService.DEFAULT_EXPIRES_IN_SECONDS));

            int specifiedExpiration = 60;
            directDownloadUrl = await _hiarc.GetFileDirectDownload(f1.Key, specifiedExpiration);
            now = DateTime.UtcNow;
            Assert.True(directDownloadUrl.ExpiresAt > now.AddSeconds(specifiedExpiration-5));
            Assert.True(directDownloadUrl.ExpiresAt < now.AddSeconds(specifiedExpiration));
            
            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void DirectUploadUrl()
        {
            var directUploadUrl = await _hiarc.CreateFileDirectUpload();
            Assert.StartsWith("https",directUploadUrl.DirectUploadUrl);
        }

        [Fact]
        public async void DownloadNewVersionOfFile()
        {
            var originalPath = _hiarc.BuildPath(TEST_FILE_TINY);
            var newVersionPath = _hiarc.BuildPath(TEST_FILE_TINY_NEW_VERSION);
            var downloadedPath = _hiarc.BuildPath($"Downloaded-{TEST_FILE_TINY_NEW_VERSION}");

            var f1 = await _hiarc.CreateFile(originalPath);
            var fileWithNewVersion = await _hiarc.AddNewVersionToFile(f1.Key, newVersionPath);        
            await _hiarc.DownloadFile(fileWithNewVersion.Key, downloadedPath);  
            this.AssertFileHash(newVersionPath, downloadedPath);
            
            System.IO.File.Delete(downloadedPath);
            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void CopyFileAws()
        {
            var source = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY), storageService: AWS_EAST_STORAGE);
            var copy = await _hiarc.CopyFile(source.Key, storageService: AWS_EAST_STORAGE); 
            Assert.NotEqual(copy.Key, source.Key);

            var awsWestCopy = await _hiarc.CopyFile(source.Key, storageService: AWS_WEST_STORAGE); 
            Assert.NotEqual(awsWestCopy.Key, source.Key);

            var azureCopy = await _hiarc.CopyFile(source.Key, storageService: AZURE_STORAGE_1);
            Assert.NotEqual(azureCopy.Key, source.Key);

            var googleCopy = await _hiarc.CopyFile(source.Key, storageService: GOOGLE_EAST_STORAGE);
            Assert.NotEqual(googleCopy.Key, source.Key);

            await _hiarc.DeleteFile(source.Key); 
            await _hiarc.DeleteFile(copy.Key);
            await _hiarc.DeleteFile(awsWestCopy.Key);
            await _hiarc.DeleteFile(azureCopy.Key);
            await _hiarc.DeleteFile(googleCopy.Key);
        }

        [Fact]
        public async void CopyFileAzure()
        {
            var source = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY), storageService: AZURE_STORAGE_1);
            var copy = await _hiarc.CopyFile(source.Key, storageService: AZURE_STORAGE_1); 
            Assert.NotEqual(copy.Key, source.Key);

            var azure2Copy = await _hiarc.CopyFile(source.Key, storageService: AZURE_STORAGE_2);
            Assert.NotEqual(azure2Copy.Key, source.Key);

            var awsCopy = await _hiarc.CopyFile(source.Key, storageService: AWS_EAST_STORAGE);
            Assert.NotEqual(awsCopy.Key, source.Key);

            var googleCopy = await _hiarc.CopyFile(source.Key, storageService: GOOGLE_EAST_STORAGE);
            Assert.NotEqual(googleCopy.Key, source.Key);

            await _hiarc.DeleteFile(source.Key); 
            await _hiarc.DeleteFile(copy.Key);
            await _hiarc.DeleteFile(azure2Copy.Key);
            await _hiarc.DeleteFile(awsCopy.Key);
            await _hiarc.DeleteFile(googleCopy.Key);   
        }

        [Fact]
        public async void CopyFileGoogle()
        {
            var source = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY), storageService: GOOGLE_EAST_STORAGE);
            var copy = await _hiarc.CopyFile(source.Key, storageService: GOOGLE_EAST_STORAGE); 
            Assert.NotEqual(copy.Key, source.Key);

            var googleWestCopy = await _hiarc.CopyFile(source.Key, storageService: GOOGLE_WEST_STORAGE); 
            Assert.NotEqual(googleWestCopy.Key, source.Key);

            var azureCopy = await _hiarc.CopyFile(source.Key, storageService: AZURE_STORAGE_1);
            Assert.NotEqual(azureCopy.Key, source.Key);

            var awsCopy = await _hiarc.CopyFile(source.Key, storageService: AWS_EAST_STORAGE);
            Assert.NotEqual(awsCopy.Key, source.Key);

            await _hiarc.DeleteFile(source.Key); 
            await _hiarc.DeleteFile(copy.Key);
            await _hiarc.DeleteFile(googleWestCopy.Key);
            await _hiarc.DeleteFile(azureCopy.Key);
            await _hiarc.DeleteFile(awsCopy.Key);
        }

        [Fact]
        public async void ComplexDelete()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY));
            await _hiarc.AddNewVersionToFile(f1.Key, _hiarc.BuildPath(TEST_FILE_TINY));
            
            var c1 = await _hiarc.CreateCollection();
            await _hiarc.AddFileToCollection(c1.Key, f1.Key);

            await _hiarc.DeleteFile(f1.Key);
            await Assert.ThrowsAnyAsync<Exception>(async () => await _hiarc.GetFile(f1.Key));
        }

        [Fact]
        public async void GetFileCollections()
        {
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            
            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();

            await _hiarc.AddFileToCollection(c1.Key, f1.Key);
            await _hiarc.AddFileToCollection(c2.Key, f1.Key);
            await _hiarc.AddFileToCollection(c3.Key, f1.Key);

            var collections = await _hiarc.GetCollectionsForFile(f1.Key);
            Assert.Equal(3, collections.Count);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void UserCanAccessFileIdentityGroupRoot()
        {
            var u1 = await _hiarc.CreateUser();

            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();

            await _hiarc.AddChildCollection(c1.Key, c2.Key);
            await _hiarc.AddChildCollection(c2.Key, c3.Key);

            await _hiarc.AddUserToCollection(c1.Key, u1.Key, AccessLevel.READ_ONLY);
            
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            await _hiarc.AddFileToCollection(c3.Key, f1.Key); //the root collection

            var fetchedFile = await _hiarc.GetFile(f1.Key, u1.Key);
            Assert.Equal(f1, fetchedFile, new EntityComparer());

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void UserCanAccessFileIdentityGroupParent()
        {
            var u1 = await _hiarc.CreateUser();

            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();

            await _hiarc.AddChildCollection(c1.Key, c2.Key);
            await _hiarc.AddChildCollection(c2.Key, c3.Key);

            await _hiarc.AddUserToCollection(c1.Key, u1.Key, AccessLevel.READ_ONLY);
            
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            await _hiarc.AddFileToCollection(c1.Key, f1.Key); //a parent collection

            var fetchedFile = await _hiarc.GetFile(f1.Key, u1.Key);
            Assert.Equal(f1, fetchedFile, new EntityComparer());

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void UserCanAccessFileOtherGroupRoot()
        {
            var u1 = await _hiarc.CreateUser();

            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();

            await _hiarc.AddChildCollection(c1.Key, c2.Key);
            await _hiarc.AddChildCollection(c2.Key, c3.Key);

            var g1 = await _hiarc.CreateGroup();
            await _hiarc.AddUserToGroup(g1.Key,u1.Key);
            await _hiarc.AddGroupToCollection(c1.Key,g1.Key, AccessLevel.READ_ONLY);
            
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            await _hiarc.AddFileToCollection(c3.Key, f1.Key); //the root collection

            var fetchedFile = await _hiarc.GetFile(f1.Key, u1.Key);
            Assert.Equal(f1, fetchedFile, new EntityComparer());

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void UserCanAccessFileOtherGroupParent()
        {
            var u1 = await _hiarc.CreateUser();

            var c1 = await _hiarc.CreateCollection();
            var c2 = await _hiarc.CreateCollection();
            var c3 = await _hiarc.CreateCollection();

            await _hiarc.AddChildCollection(c1.Key, c2.Key);
            await _hiarc.AddChildCollection(c2.Key, c3.Key);

            var g1 = await _hiarc.CreateGroup();
            await _hiarc.AddUserToGroup(g1.Key,u1.Key);
            await _hiarc.AddGroupToCollection(c1.Key, g1.Key, AccessLevel.READ_ONLY);
            
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY));
            await _hiarc.AddFileToCollection(c1.Key, f1.Key); //a parent collection

            var fetchedFile = await _hiarc.GetFile(f1.Key, u1.Key);
            Assert.Equal(f1, fetchedFile, new EntityComparer());

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void CreateFileWithMetadata()
        {
            var md = TestMetadata;
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY), md);
            var fetchedFile = await _hiarc.GetFile(f1.Key);
            
            Assert.Equal(f1, fetchedFile, new EntityComparer());
            AssertMetadata(md, fetchedFile.Metadata);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void UpdateMetadata()
        {
            var md = TestMetadata;
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY), md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", "support" },
                { "quotaCarrying", false },
                { "targetRate", 7.271 },
                { "level", 2 },
                { "startDate", DateTime.Parse("2020-02-25T22:33:50.134Z").ToUniversalTime() }
            };

            var request = new UpdateFileRequest
            {
                Metadata = updatedMD
            };

            var updatedFile = await _hiarc.UpdateFile(f1.Key, request);
            AssertMetadata(updatedMD, updatedFile.Metadata);

            await _hiarc.DeleteFile(f1.Key);
        }

        [Fact]
        public async void NullOutMetadata()
        {
            var md = TestMetadata;
            var f1 = await _hiarc.CreateFile(_hiarc.BuildPath(TEST_FILE_TINY), md);
            
            var updatedMD = new Dictionary<string, object>
            {
                { "department", null },
                { "quotaCarrying", null }
            };

            var request = new UpdateFileRequest
            {
                Metadata = updatedMD
            };

            var updatedFile = await _hiarc.UpdateFile(f1.Key, request);
            Assert.Equal(3, updatedFile.Metadata.Keys.Count);

            updatedMD = new Dictionary<string, object>
            {
                { "targetRate", null },
                { "level", null },
                { "startDate", null }
            };

            request = new UpdateFileRequest
            {
                Metadata = updatedMD
            };

            updatedFile = await _hiarc.UpdateFile(f1.Key, request);
            Assert.Null(updatedFile.Metadata);

            await _hiarc.DeleteFile(f1.Key);
        }
    }
}
