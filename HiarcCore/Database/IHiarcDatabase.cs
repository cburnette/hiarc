using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hiarc.Core.Models;
using Hiarc.Core.Models.Requests;

namespace Hiarc.Core.Database
{
    public interface IHiarcDatabase
    {
        Task InitDatabase(string adminKey);
        Task ResetDatabase(string adminKey);

        Task<User> CreateUser(CreateUserRequest request);
        Task<User> UpdateUser(string key, UpdateUserRequest request);
        Task<User> GetUser(string key);
        Task<List<User>> GetAllUsers();
        Task DeleteUser(string key);
        Task<List<User>> FindUsers(FindUsersRequest request);
        Task<List<Group>> GetGroupsForUser(string key);
        Task<bool> IsValidUserKey(string key);

        Task<Group> GetGroup(string key);
        Task<List<Group>> GetAllGroups();
        Task<Group> CreateGroup(CreateGroupRequest request, string createdBy);
        Task<Group> UpdateGroup(string key, UpdateGroupRequest request);
        Task DeleteGroup(string key);
        Task<List<Group>> FindGroups(FindGroupsRequest request);
        Task AddUserToGroup(string key, string userKey);

        Task<File> GetFile(string key);
        Task<List<FileVersion>> GetFileVersions(string key);
        Task<FileVersion> GetLatestVersionForFile(string key);
        Task<List<RetentionPolicyApplication>> GetRetentionPolicyApplicationsForFile(string key);
        Task<File> CreateFile(CreateFileRequest request, string createdBy, string versionStorageIdentifier);
        Task<File> UpdateFile(string key, UpdateFileRequest request);
        Task DeleteFile(string key);
        Task<List<Collection>> GetCollectionsForFile(string key);
        Task<File> AddVersionToFile(string key, string storageService, string versionStorageIdentifier, string createdBy);
        Task AddUserToFile(string key, AddUserToFileRequest request);
        Task AddGroupToFile(string key, AddGroupToFileRequest request);
        Task AddRetentionPolicyToFile(string key, AddRetentionPolicyToFileRequest request);
        Task AddClassificationToFile(string key, AddClassificationToFileRequest request);
        Task<bool> UserCanAccessFile(string userKey, string fileKey, List<string> accessLevels);
        Task<List<string>> UserCanAccessFiles(string userKey, List<string> fileKeys, List<string> accessLevels);

        Task<Collection> GetCollection(string key);
        Task<List<Collection>> GetAllCollections();
        Task<Collection> CreateCollection(CreateCollectionRequest request, string createdBy);
        Task<Collection> UpdateCollection(string key, UpdateCollectionRequest request);
        Task DeleteCollection(string key);
        Task<List<Collection>> FindCollections(FindCollectionsRequest request);
        Task<List<File>> GetFilesForCollection(string key);
        Task<List<Collection>> GetChildCollectionsForCollection(string key);
        Task<CollectionItems> GetItemsForCollection(string key);
        Task RemoveFileFromCollection(string key, string fileKey);
        Task AddChildToCollection(string key, string childKey);
        Task AddUserToCollection(string key, AddUserToCollectionRequest request);
        Task AddGroupToCollection(string key, AddGroupToCollectionRequest request);
        Task AddFileToCollection(string key, AddFileToCollectionRequest request);
        Task<bool> UserCanAccessCollection(string userKey, string collectionKey, List<string> accessLevels);
        Task<List<string>> UserCanAccessCollections(string userKey, List<string> collectionKeys, List<string> accessLevels);

        Task<RetentionPolicy> GetRetentionPolicy(string key);
        Task<RetentionPolicy> CreateRetentionPolicy(CreateRetentionPolicyRequest request);
        Task<RetentionPolicy> UpdateRetentionPolicy(string key, UpdateRetentionPolicyRequest request);
        Task<List<RetentionPolicy>> GetAllRetentionPolicies();
        Task<List<RetentionPolicy>> FindRetentionPolicies(FindRetentionPoliciesRequest request);

        Task<LegalHold> GetLegalHold(string key);
        Task<LegalHold> CreateLegalHold(CreateLegalHoldRequest request);

        Task<Classification> GetClassification(string key);
        Task<Classification> CreateClassification(CreateClassificationRequest request);
        Task<Classification> UpdateClassification(string key, UpdateClassificationRequest request);
        Task<List<Classification>> GetAllClassifications();
        Task<List<Classification>> FindClassifications(FindClassificationsRequest request);
    }
}