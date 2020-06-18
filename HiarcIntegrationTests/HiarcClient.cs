using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Hiarc.Core.Models.Requests;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Diagnostics;
using Hiarc.Core.Models;
using System.Collections.Generic;
using static System.Environment;

namespace HiarcIntegrationTest
{
    public class HiarcClient
    {
        const string API_KEY = "b7kNAC4xoe3QiAnLkplIjmfL3II+OX5EYHNxbwSuy7s=";
        const string API_KEY_HEADER_NAME = "X-Hiarc-Api-Key";
        const string AS_USER_HEADER_NAME = "X-Hiarc-User-Key";
        const string BASE_URI = "http://localhost:5000";

        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        private readonly IHttpClientFactory clientFactory; //https://josefottosson.se/you-are-probably-still-using-httpclient-wrong-and-it-is-destabilizing-your-software/
        private readonly Stopwatch sw;

        private int currFile = 1;
        private int currColl = 1;
        private int currUser = 1;
        private int currGroup = 1;
        private int currRetentionPolicy = 1;
        private int currClassification = 1;

        public HiarcClient()
        {
            var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            clientFactory = serviceProvider.GetService<IHttpClientFactory>();
            
            sw = new Stopwatch();
        }

        public async Task<bool> InitDB(bool logToConsole=true) 
        {
            sw.Restart();
            await Post<EmptyRequest,Empty>(new EmptyRequest(), "admin/database/init");
            sw.Stop();

            if (logToConsole) { Console.WriteLine($"\nInitialized Database: {sw.ElapsedMilliseconds}ms\n####################\n"); };
            return true;
        }

        public async Task<bool> ResetDB(bool logToConsole=true) 
        {
            sw.Restart();
            await Put<EmptyRequest,Empty>(new EmptyRequest(), "admin/database/reset");
            sw.Stop();

            if (logToConsole) { Console.WriteLine($"\nReset Database: {sw.ElapsedMilliseconds}ms\n####################\n"); };
            return true;
        }

        public async Task<UserCredentials> CreateUserCredentials(string key, bool logToConsole=true)
        {
            var request = new CreateUserTokenRequest { Key = key };
            var token = await Post<CreateUserTokenRequest,UserCredentials>(request, "tokens/user"); 
            if (logToConsole) { Console.WriteLine($"Created Token for User '{key}': {ToJson(token)}"); };
            return token;
        }

        public async Task<File> GetFile(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var file = await Get<File>($"files/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Retrieved File: {ToJson(file)}"); };
            return file;
        }

        public async Task<List<FileVersion>> GetFileVersions(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var fileVersions = await Get<List<FileVersion>>($"files/{key}/versions", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Retrieved FileVersions: {ToJson(fileVersions)}"); };
            return fileVersions;
        }

        public async Task<List<RetentionPolicyApplication>> GetFileRetentionPolicies(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var retentionPolicies = await Get<List<RetentionPolicyApplication>>($"files/{key}/retentionpolicies", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Retrieved Retention Policies for File Key '{key}': {ToJson(retentionPolicies)}"); };
            return retentionPolicies;
        }

        public async Task<File> CopyFile(string key, string storageService=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var copyKey = GenerateKey("file");
            var copyFileRequest = new CopyFileRequest() { Key = copyKey, StorageService=storageService };
            var copyOfFile = await Put<CopyFileRequest,File>(copyFileRequest, $"files/{key}/copy", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Created Copy of File: OriginalKey='{key}', CopyKey='{copyOfFile.Key}'"); };
            return copyOfFile;
        }

        public async Task DownloadFile(string key, string pathToSaveTo, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        { 
            sw.Restart();
            using (var s = await Client(asUserKey, bearerToken).GetStreamAsync($"files/{key}/download"))
            using (System.IO.FileStream fs = System.IO.File.Create(pathToSaveTo))
            {
                await s.CopyToAsync(fs);
            }
            sw.Stop();

            if (logToConsole) { Console.WriteLine($"Downloaded File: Key=\"{key}\", Path=\"{pathToSaveTo}\", Elapsed={sw.ElapsedMilliseconds}ms"); };
        }

        public async Task<FileDirectDownload> GetFileDirectDownload(string key, int? expiresInSeconds=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var url = expiresInSeconds == null ? $"files/{key}/directdownloadurl" : $"files/{key}/directdownloadurl?expiresInSeconds={expiresInSeconds.Value}";

            var directDownloadInfo = await Get<FileDirectDownload>(url, asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Retrieved DirectDownloadInfo: {ToJson(directDownloadInfo)}"); };
            return directDownloadInfo;
        }

        public async Task<FileDirectUpload> CreateFileDirectUpload(string storageService=null, int? expiresInSeconds=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var url = expiresInSeconds == null ? $"files/directuploadurl" : $"files/directuploadurl?expiresInSeconds={expiresInSeconds.Value}";

            var request = new CreateDirectUploadUrlRequest { StorageService=storageService };
            var directUploadInfo = await Post<CreateDirectUploadUrlRequest,FileDirectUpload>(request, url, asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Retrieved DirectUploadInfo: {ToJson(directUploadInfo)}"); };
            return directUploadInfo;
        }

        public async Task<File> CreateFile(string filePath, Dictionary<string, object> metadata=null, string storageService=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            using System.IO.Stream fileStream = System.IO.File.OpenRead(filePath);
            sw.Restart();
            var key = GenerateKey("file");
            var fileName = System.IO.Path.GetFileName(filePath);

            var multipart = new MultipartFormDataContent();
            var sc = new StreamContent(fileStream);
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            multipart.Add(sc, "file", fileName);

            var createFileRequest = new CreateFileRequest() { Key = key, Name = fileName, Description = "This is a brand new file", Metadata=metadata, StorageService = storageService };
            var jsonContent = JsonContent(createFileRequest);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            multipart.Add(jsonContent, "request");

            var response = await Client(asUserKey, bearerToken).PostAsync("files", multipart);
            sw.Stop();
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var newFile = JsonSerializer.Deserialize<File>(body, jsonOptions);

                if (logToConsole) { Console.WriteLine($"Created New File: {ToJson(newFile)}, Elapsed={sw.ElapsedMilliseconds}ms"); };
                return newFile;
            }
            else
            {
                try
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<Error>(body, jsonOptions);
                    throw new Exception($"StatusCode: {response.StatusCode}, Message: {error.Message}");
                }
                catch
                {
                    throw new Exception($"Status Code: {response.StatusCode}");
                }
            }
        }

        public async Task<File> UpdateFile(string key, UpdateFileRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var updatedFile = await Put<UpdateFileRequest,File>(request, $"files/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Updated File: {ToJson(updatedFile)}\""); };
            return updatedFile;
        }

        public async Task DeleteFile(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            await Delete($"files/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Deleted File Key: {key}\""); };
        }

        public async Task<File> AttachToExistingFile(string storageService, string storageId, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var fileKey = GenerateKey("file");
            var request = new AttachToExistingFileRequest() { Name=fileKey, StorageService=storageService, StorageId=storageId };

            var newFile = await Put<AttachToExistingFileRequest,File>(request, $"files/{fileKey}/attach", asUserKey, bearerToken);

            if (logToConsole) { Console.WriteLine($"Attached to Existing File: Key='{newFile.Key}', StorageService='{storageService}', StorageId='{storageId}'"); };
            return newFile;
        }

        public async Task<IList<Collection>> GetCollectionsForFile(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var collections = await Get<IList<Collection>>($"files/{key}/collections", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Collections for File '{key}': {ToJson(collections)}"); };
            return collections;
        }

        public async Task<File> AddNewVersionToFile(string key, string filePath, string storageService=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            using System.IO.Stream fileStream = System.IO.File.OpenRead(filePath);
            sw.Restart();
            var fileName = System.IO.Path.GetFileName(filePath);

            var multipart = new MultipartFormDataContent();
            var sc = new StreamContent(fileStream);
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            multipart.Add(sc, "file", fileName);

            var addVersionToFileRequest = new AddVersionToFileRequest() { Key = key, StorageService = storageService };
            var jsonContent = JsonContent(addVersionToFileRequest);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            multipart.Add(jsonContent, "request");

            var response = await Client(asUserKey, bearerToken).PutAsync($"files/{key}/versions", multipart);
            sw.Stop();
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var newFile = JsonSerializer.Deserialize<File>(body, jsonOptions);

                if (logToConsole) { Console.WriteLine($"Created New Version of File: {ToJson(newFile)}, Elapsed={sw.ElapsedMilliseconds}ms"); };
                return newFile;
            }
            else
            {
                try
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<Error>(body, jsonOptions);
                    throw new Exception($"StatusCode: {response.StatusCode}, Message: {error.Message}");
                }
                catch
                {
                    throw new Exception($"Status Code: {response.StatusCode}");
                }
            }
        }

        public async Task AddUserToFile(string userKey, string fileKey, string accessLevel, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var addUserToFileRequest = new AddUserToFileRequest() { UserKey = userKey, AccessLevel=accessLevel };
            await Put<AddUserToFileRequest,Empty>(addUserToFileRequest, $"files/{fileKey}/users", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added user to file: User=\"{userKey}\", File=\"{fileKey}\", AccessLevel=\"{accessLevel}\"");};
        }

        public async Task AddGroupToFile(string groupKey, string fileKey, string accessLevel, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var addGroupToFileRequest = new AddGroupToFileRequest() { GroupKey = groupKey, AccessLevel=accessLevel };
            await Put<AddGroupToFileRequest,Empty>(addGroupToFileRequest, $"files/{fileKey}/groups", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added group to file: Group=\"{groupKey}\", File=\"{fileKey}\", AccessLevel=\"{accessLevel}\"");};
        }

        public async Task AddRetentionPolicyToFile(string retentionPolicyKey, string fileKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var addRetentionPolicyToFileRequest = new AddRetentionPolicyToFileRequest() { RetentionPolicyKey = retentionPolicyKey};
            await Put<AddRetentionPolicyToFileRequest,Empty>(addRetentionPolicyToFileRequest, $"files/{fileKey}/retentionpolicies", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added retention policy to file: RetentionPolicy=\"{retentionPolicyKey}\", File=\"{fileKey}\"");};
        }

        public async Task AddClassificationToFile(string classificationKey, string fileKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var addClassificationToFileRequest = new AddClassificationToFileRequest() { ClassificationKey = classificationKey};
            await Put<AddClassificationToFileRequest,Empty>(addClassificationToFileRequest, $"files/{fileKey}/classifications", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added classification to file: Classification=\"{classificationKey}\", File=\"{fileKey}\"");};
        }

        public async Task<User> CreateUser(Dictionary<string, object> metadata=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var key = GenerateKey("user");
            var createUserRequest = new CreateUserRequest() { Key = key, Name = $"name-{key}", Description = "Lobster taco", Metadata = metadata };
            var newUser = await Post<CreateUserRequest,User>(createUserRequest, "users", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Created New User: {ToJson(newUser)}"); };
            return newUser;
        }

        public async Task<User> UpdateUser(string key, UpdateUserRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var updatedUser = await Put<UpdateUserRequest,User>(request, $"users/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Updated User: {ToJson(updatedUser)}"); };
            return updatedUser;
        }

        public async Task<User> GetUser(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var user = await Get<User>($"users/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched User: {ToJson(user)}"); };
            return user;
        }

        public async Task<User> GetCurrentUser(string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var user = await Get<User>($"users/current", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Current User: {ToJson(user)}"); };
            return user;
        }

        public async Task DeleteUser(string userKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            await Delete($"users/{userKey}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Deleted User Key: {userKey}"); };
        }

        public async Task<List<User>> FindUsers(FindUsersRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var foundUsers = await Post<FindUsersRequest,List<User>>(request, "users/find", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Found {foundUsers.Count} Users: {ToJson(foundUsers)}"); };
            return foundUsers;
        }

        public async Task<List<Group>> GetGroupsForUser(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var groups = await Get<List<Group>>($"users/{key}/groups", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Groups for User: Key=\"{key}\", NumGroups=\"{groups.Count}\""); };
            return groups;
        }

        public async Task<List<Group>> GetGroupsForCurrentUser(string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var groups = await Get<List<Group>>($"users/current/groups", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Groups for Current User: NumGroups=\"{groups.Count}\""); };
            return groups;
        }

        public async Task<IList<User>> GetAllUsers(string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var users = await Get<IList<User>>($"users", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Users: Count={users.Count}"); };
            return users;
        }

        public async Task<Group> CreateGroup(Dictionary<string, object> metadata=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var key = GenerateKey("group");
            var createGroupRequest = new CreateGroupRequest() { Key = key, Name = $"name-{key}", Metadata=metadata };
            var newGroup = await Post<CreateGroupRequest,Group>(createGroupRequest, "groups", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Created New Group: {ToJson(newGroup)}"); };
            return newGroup;
        }

        public async Task<Group> GetGroup(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var group = await Get<Group>($"groups/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Group: {ToJson(group)}"); };
            return group;
        }

        public async Task<Group> UpdateGroup(string key, UpdateGroupRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var updatedGroup = await Put<UpdateGroupRequest,Group>(request, $"groups/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Updated Group: {ToJson(updatedGroup)}"); };
            return updatedGroup;
        }

        public async Task<List<Group>> FindGroups(FindGroupsRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var foundGroups = await Post<FindGroupsRequest,List<Group>>(request, "groups/find", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Found {foundGroups.Count} Groups: {ToJson(foundGroups)}"); };
            return foundGroups;
        }

        public async Task DeleteGroup(string groupKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            await Delete($"groups/{groupKey}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Deleted Group Key: {groupKey}"); };
        }

        public async Task<IList<Group>> GetAllGroups(string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var groups = await Get<IList<Group>>($"groups", asUserKey, bearerToken);
            Console.WriteLine($"Fetched Groups: Count={groups.Count}");
            
            if (logToConsole) 
            { 
                foreach(var group in groups)
                {
                    Console.WriteLine($"Fetched Group: Key=\"{group.Key}\", Name=\"{group.Name}\"");
                }
            };
            return groups;
        }

        public async Task<Empty> AddUserToGroup(string groupKey, string userKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var result = await Put<EmptyRequest,Empty>(new EmptyRequest(), $"groups/{groupKey}/users/{userKey}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added User to Group: User=\"{userKey}\", Group=\"{groupKey}\""); };
            return result;
        }

        public async Task<Collection> CreateCollection(Dictionary<string,object> metadata=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var key = GenerateKey("collection");
            var createCollectionRequest = new CreateCollectionRequest() { Key = key, Name = $"name-{key}", Metadata = metadata };
            var newCollection = await Post<CreateCollectionRequest,Collection>(createCollectionRequest, "collections", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Created New Collection: {ToJson(newCollection)}"); };
            return newCollection;
        }

        public async Task<Collection> UpdateCollection(string key, UpdateCollectionRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var updatedCollection = await Put<UpdateCollectionRequest,Collection>(request, $"collections/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Updated Collection: {ToJson(updatedCollection)}"); };
            return updatedCollection;
        }

        public async Task DeleteCollection(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            await Delete($"collections/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Deleted Collection Key: {key}"); };
        }

        public async Task<List<Collection>> FindCollections(FindCollectionsRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var foundCollections = await Post<FindCollectionsRequest,List<Collection>>(request, "collections/find", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Found {foundCollections.Count} Collections: {ToJson(foundCollections)}"); };
            return foundCollections;
        }

        public async Task<Collection> GetCollection(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var collection = await Get<Collection>($"collections/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Collection: {ToJson(collection)}"); };
            return collection;
        }

        public async Task<IList<Collection>> GetAllCollections(string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var collections = await Get<IList<Collection>>($"collections", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Collections: Count={collections.Count}"); };
            return collections;
        }

        public async Task<List<File>> GetFilesForCollection(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var files = await Get<List<File>>($"collections/{key}/files", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Files for Collection '{key}': {ToJson(files)}"); };
            return files;
        }

        public async Task<List<Collection>> GetChildrenForCollection(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var children = await Get<List<Collection>>($"collections/{key}/children", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Children for Collection '{key}': {ToJson(children)}"); };
            return children;
        }

        public async Task<CollectionItems> GetItemsForCollection(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var items = await Get<CollectionItems>($"collections/{key}/items", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Items for Collection '{key}': Children={ToJson(items.ChildCollections)}, Files={ToJson(items.Files)}"); };
            return items;
        }

        public async Task RemoveFileFromCollection(string key, string fileKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            await Delete($"collections/{key}/files/{fileKey}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Deleted File '{fileKey}' from Collection '{key}'"); };
        }

        public async Task<Empty> AddChildCollection(string parentKey, string childKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var result = await Put<EmptyRequest, Empty>(new EmptyRequest(), $"collections/{parentKey}/children/{childKey}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added Child to Collection: Parent=\"{parentKey}\", Child=\"{childKey}\""); };
            return result;
        }

        public async Task<Empty> AddUserToCollection(string collectionKey, string userKey, string accessLevel, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var request = new AddUserToCollectionRequest() { UserKey = userKey, AccessLevel = accessLevel };
            var result = await Put<AddUserToCollectionRequest, Empty>(request, $"collections/{collectionKey}/users", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added User to Collection: Collection=\"{collectionKey}\", User=\"{userKey}\", AccessLevel=\"{accessLevel}\""); };
            return result;
        }

        public async Task<Empty> AddGroupToCollection(string collectionKey, string groupKey, string accessLevel, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var request = new AddGroupToCollectionRequest() { GroupKey = groupKey, AccessLevel = accessLevel };
            var result = await Put<AddGroupToCollectionRequest, Empty>(request, $"collections/{collectionKey}/groups", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added Group to Collection: Collection=\"{collectionKey}\", Group=\"{groupKey}\", AccessLevel=\"{accessLevel}\""); };
            return result;
        }

        public async Task<Empty> AddFileToCollection(string collectionKey, string fileKey, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var request = new AddFileToCollectionRequest() { FileKey = fileKey };
            var result = await Put<AddFileToCollectionRequest, Empty>(request, $"Collections/{collectionKey}/files", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Added File to Collection: Collection=\"{collectionKey}\", File=\"{fileKey}\""); };
            return result;
        }

        public async Task<RetentionPolicy> GetRetentionPolicy(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var retentionPolicy = await Get<RetentionPolicy>($"retentionpolicies/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Retention Policy: {ToJson(retentionPolicy)}"); };
            return retentionPolicy;
        }

        public async Task<RetentionPolicy> CreateRetentionPolicy(uint seconds, Dictionary<string, object> metadata=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var key = GenerateKey("retentionPolicy");
            var createRetentionPolicyRequest = new CreateRetentionPolicyRequest() { Key = key, Name = $"name-{key}", Description = "Lobster taco retention policy", Metadata = metadata, Seconds=seconds };
            var newRetentionPolicy = await Post<CreateRetentionPolicyRequest,RetentionPolicy>(createRetentionPolicyRequest, "retentionpolicies", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Created New Retention Policy: {ToJson(newRetentionPolicy)}\""); };
            return newRetentionPolicy;
        }

        public async Task<RetentionPolicy> UpdateRetentionPolicy(string key, UpdateRetentionPolicyRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var updatedRetentionPolicy = await Put<UpdateRetentionPolicyRequest,RetentionPolicy>(request, $"retentionpolicies/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Updated Retention Policy: {ToJson(updatedRetentionPolicy)}\""); };
            return updatedRetentionPolicy;
        }

        public async Task<IList<RetentionPolicy>> GetAllRetentionPolicies(string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var policies = await Get<IList<RetentionPolicy>>($"retentionpolicies", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Retention Policies: Count={policies.Count}"); };
            return policies;
        }
        
        public async Task<List<RetentionPolicy>> FindRetentionPolicies(FindRetentionPoliciesRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var foundPolicies = await Post<FindRetentionPoliciesRequest,List<RetentionPolicy>>(request, "retentionpolicies/find", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Found {foundPolicies.Count} Policies: {ToJson(foundPolicies)}"); };
            return foundPolicies;
        }

        public async Task<Classification> GetClassification(string key, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var classification = await Get<Classification>($"classifications/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Classification: {ToJson(classification)}"); };
            return classification;
        }

        public async Task<IList<Classification>> GetAllClassifications(string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var classifications = await Get<IList<Classification>>($"classifications", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Fetched Classifications: Count={classifications.Count}"); };
            return classifications;
        }

        public async Task<Classification> CreateClassification(Dictionary<string, object> metadata=null, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var key = GenerateKey("classification");
            var createClassificationRequest = new CreateClassificationRequest() { Key = key, Name = $"name-{key}", Description = "Lobster taco classification", Metadata = metadata };
            var newClassification = await Post<CreateClassificationRequest,Classification>(createClassificationRequest, "classifications", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Created New Classification: {ToJson(newClassification)}\""); };
            return newClassification;
        }

        public async Task<Classification> UpdateClassification(string key, UpdateClassificationRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var updatedClassification = await Put<UpdateClassificationRequest,Classification>(request, $"classifications/{key}", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Updated Classification: {ToJson(updatedClassification)}\""); };
            return updatedClassification;
        }

        public async Task<List<Classification>> FindClassifications(FindClassificationsRequest request, string asUserKey=null, string bearerToken=null, bool logToConsole=true)
        {
            var foundClassifications = await Post<FindClassificationsRequest,List<Classification>>(request, "classifications/find", asUserKey, bearerToken);
            if (logToConsole) { Console.WriteLine($"Found {foundClassifications.Count} Classifications: {ToJson(foundClassifications)}"); };
            return foundClassifications;
        }

        public async Task<R> Get<R>(string path, string asUserKey=null, string bearerToken=null)
        {
            var response = await Client(asUserKey, bearerToken).GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
            using var contentStream = await response.Content.ReadAsStreamAsync();
            if (response.IsSuccessStatusCode)
            {
                R result = await JsonSerializer.DeserializeAsync<R>(contentStream, jsonOptions);
                return result;
            }
            else
            {
                try
                {
                    var error = await JsonSerializer.DeserializeAsync<Error>(contentStream, jsonOptions);
                    throw new Exception($"StatusCode: {response.StatusCode}, Message: {error.Message}");
                }
                catch
                {
                    throw new Exception($"Status Code: {response.StatusCode}");
                }
            }
        }

        public async Task<R> Post<M,R>(M requestModel, string path, string asUserKey=null, string bearerToken=null)
        {
            var postContent = JsonContent(requestModel);
            var response = await Client(asUserKey, bearerToken).PostAsync(path, postContent);  
            using var contentStream = await response.Content.ReadAsStreamAsync();    
            if (response.IsSuccessStatusCode)
            {
                R result = await JsonSerializer.DeserializeAsync<R>(contentStream, jsonOptions);
                return result;
            }
            else
            {
                try
                {
                    var error = await JsonSerializer.DeserializeAsync<Error>(contentStream, jsonOptions);
                    throw new Exception($"StatusCode: {response.StatusCode}, Message: {error.Message}");
                }
                catch
                {
                    throw new Exception($"Status Code: {response.StatusCode}");
                }
            }
        }

        public async Task<R> Put<M,R>(M requestModel, string path, string asUserKey=null, string bearerToken=null)
        {
            var postContent = JsonContent(requestModel);
            var response = await Client(asUserKey, bearerToken).PutAsync(path, postContent);
            using var contentStream = await response.Content.ReadAsStreamAsync();
            if (response.IsSuccessStatusCode)
            {
                R result = await JsonSerializer.DeserializeAsync<R>(contentStream, jsonOptions);
                return result;
            }
            else
            {
                try
                {
                    var error = await JsonSerializer.DeserializeAsync<Error>(contentStream, jsonOptions);
                    throw new Exception($"StatusCode: {response.StatusCode}, Message: {error.Message}");
                }
                catch
                {
                    throw new Exception($"Status Code: {response.StatusCode}");
                }       
            }
        }

        public async Task Delete(string path, string asUserKey=null, string bearerToken=null)
        {
            var response = await Client(asUserKey, bearerToken).DeleteAsync(path);
            using var contentStream = await response.Content.ReadAsStreamAsync();
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var error = await JsonSerializer.DeserializeAsync<Error>(contentStream, jsonOptions);
                    throw new Exception($"StatusCode: {response.StatusCode}, Message: {error.Message}");
                }         
                catch
                {
                    throw new Exception($"Status Code: {response.StatusCode}");
                }  
            }
        }

        public HttpClient Client(string asUserKey = null, string bearerToken=null)
        {
            var client = clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.BaseAddress = new Uri(BASE_URI);

            if (bearerToken != null)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            }
            else
            {
                client.DefaultRequestHeaders.Add(API_KEY_HEADER_NAME, API_KEY);
                if (asUserKey != null)
                {
                    client.DefaultRequestHeaders.Add(AS_USER_HEADER_NAME, asUserKey);
                }
            }
   
            return client;
        }

        public HttpContent JsonContent<T>(T requestModel)
        {
            var json = JsonSerializer.Serialize<T>(requestModel);
            var httpContent = new StringContent(json);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");     
            return httpContent;
        }

        public string ToJson<T>(T model)
        {
            return JsonSerializer.Serialize(model, jsonOptions);
        }

        public string GenerateKey(string prefix) => prefix switch
        {
            "file" => $"{prefix}-{currFile++}",
            "collection" => $"{prefix}-{currColl++}",
            "user" => $"{prefix}-{currUser++}",
            "group" => $"{prefix}-{currGroup++}",
            "retentionPolicy" => $"{prefix}-{currRetentionPolicy++}",
            "classification" => $"{prefix}-{currClassification++}",
            _ => $"{prefix}-{Guid.NewGuid()}",
        };

        public string BuildPath(string filename) => $"{System.IO.Directory.GetCurrentDirectory()}/../../../TestFiles/{filename}";

    }
}