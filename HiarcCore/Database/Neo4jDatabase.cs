using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Hiarc.Core.Models;
using Hiarc.Core.Models.Requests;
using Hiarc.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using System.Text.Json;
using Hiarc.Core.Events;
using Hiarc.Core.Storage;

namespace Hiarc.Core.Database
{
    public class Neo4jDatabase : IHiarcDatabase
    {
        private static readonly List<string> ENTITY_PROPERTIES = new List<string>() { "key", "name", "description", "createdAt", "modifiedAt" };
        private static readonly List<string> VALID_BOOLEAN_OPERATORS = new List<string>() { "AND", "OR", "XOR", "NOT", "AND NOT", "OR NOT", "XOR NOT" };
        private static readonly List<string> VALID_PREDICATE_OPERATORS = new List<string>() { "=", "<>", "<", "<=", ">", ">=", "STARTS WITH", "ENDS WITH", "CONTAINS" };
        private static readonly List<string> VALID_PARENS_OPERATORS = new List<string>() { "(", ")" };
        private static readonly StringComparer CASE_INSENSITIVE_COMPARER = StringComparer.OrdinalIgnoreCase;

        private const string METADATA_PREFIX = "hiarcMeta_";

        private const string LABEL_FILE = "File"; 
        private const string LABEL_FILE_VERSION = "FileVersion";
        private const string LABEL_COLLECTION = "Collection";
        private const string LABEL_USER = "User";
        private const string LABEL_GROUP = "Group";
        private const string LABEL_RETENTION_POLICY = "RetentionPolicy";
        private const string LABEL_CLASSIFICATION = "Classification";

        private const string RELATIONSHIP_BELONGS_TO = "BELONGS_TO";
        private const string RELATIONSHIP_CONTAINS = "CONTAINS";
        private const string RELATIONSHIP_CHILD_OF = "CHILD_OF";
        private const string RELATIONSHIP_CREATED_BY = "CREATED_BY";
        private const string RELATIONSHIP_HAS_CLASSIFICATION = "HAS_CLASSIFICATION";
        private const string RELATIONSHIP_HAS_RETENTION_POLICY = "HAS_RETENTION_POLICY";
        private const string RELATIONSHIP_HAS_VERSION = "HAS_VERSION";

        private readonly HiarcSettings _hiarcSettings;
        private readonly ILogger<Neo4jDatabase> _logger;
        private readonly IEventServiceProvider _eventService;
        private readonly IStorageServiceProvider _storageServiceProvider;
        private readonly IDriver _neo4j;

        public Neo4jDatabase(   IOptions<HiarcSettings> hiarchSettings, 
                                IEventServiceProvider eventService, 
                                IStorageServiceProvider storageServiceProvider, 
                                ILogger<Neo4jDatabase> logger)
        {
            _hiarcSettings = hiarchSettings.Value;
            _eventService = eventService;
            _storageServiceProvider = storageServiceProvider;
            _logger = logger;
            _neo4j = GraphDatabase.Driver(_hiarcSettings.Database.Uri, 
                                          AuthTokens.Basic(_hiarcSettings.Database.Username, _hiarcSettings.Database.Password));
        }

        ~Neo4jDatabase()
        {
            _neo4j.CloseAsync().RunSynchronously();
        }

        public async Task InitDatabase(string adminKey)
        {
            var session = _neo4j.AsyncSession();
            await session.RunAsync($"CREATE CONSTRAINT ON (u:{LABEL_USER}) ASSERT u.key IS UNIQUE");
            await session.CloseAsync();

            session = _neo4j.AsyncSession();
            await session.RunAsync($"CREATE CONSTRAINT ON (g:{LABEL_GROUP}) ASSERT g.key IS UNIQUE");
            await session.CloseAsync();

            session = _neo4j.AsyncSession();
            await session.RunAsync($"CREATE CONSTRAINT ON (c:{LABEL_COLLECTION}) ASSERT c.key IS UNIQUE");
            await session.CloseAsync();

            session = _neo4j.AsyncSession();
            await session.RunAsync($"CREATE CONSTRAINT ON (f:{LABEL_FILE}) ASSERT f.key IS UNIQUE");
            await session.CloseAsync();

            session = _neo4j.AsyncSession();
            await session.RunAsync($"CREATE CONSTRAINT ON (f:{LABEL_CLASSIFICATION}) ASSERT f.key IS UNIQUE");
            await session.CloseAsync();

            session = _neo4j.AsyncSession();
            await session.RunAsync($"CREATE CONSTRAINT ON (f:{LABEL_RETENTION_POLICY}) ASSERT f.key IS UNIQUE");
            await session.CloseAsync();
            
            session = _neo4j.AsyncSession();
            await session.RunAsync($"CALL db.index.fulltext.createNodeIndex('userNameDescription',['{LABEL_USER}'],['name', 'description'])");
            await session.RunAsync($"CALL db.index.fulltext.createNodeIndex('groupNameDescription',['{LABEL_GROUP}'],['name', 'description'])");
            await session.RunAsync($"CALL db.index.fulltext.createNodeIndex('fileNameDescription',['{LABEL_FILE}'],['name', 'description'])");
            await session.RunAsync($"CALL db.index.fulltext.createNodeIndex('collectionNameDescription',['{LABEL_COLLECTION}'],['name', 'description'])");
            await session.RunAsync($"CALL db.index.fulltext.createNodeIndex('classificationNameDescription',['{LABEL_CLASSIFICATION}'],['name', 'description'])");
            await session.RunAsync($"CALL db.index.fulltext.createNodeIndex('retentionPolicyNameDescription',['{LABEL_RETENTION_POLICY}'],['name', 'description'])");
            await session.CloseAsync();

            session = _neo4j.AsyncSession();
            var createAdminRequest = new CreateUserRequest() { Key = adminKey };
            await CreateUser(createAdminRequest);
            await session.CloseAsync();

            _logger.LogInformation("Initialized Hiarc database");
        }

        public async Task ResetDatabase(string adminKey)
        {
            var session = _neo4j.AsyncSession();
            await session.RunAsync("MATCH (n) DETACH DELETE n");
            await session.CloseAsync();

            session = _neo4j.AsyncSession();
            var createAdminRequest = new CreateUserRequest() { Key = adminKey };
            await CreateUser(createAdminRequest);
            await session.CloseAsync();

            _logger.LogInformation("Reset Hiarc database");
        }

        public async Task<User> CreateUser(CreateUserRequest request)
        {
            var session = _neo4j.AsyncSession();

            var propertyQueryParts = BuildEntityQueryParts(request, false);
            var propertyQuery = string.Join(", ", propertyQueryParts);
            var query = $@" CREATE (u:{LABEL_USER} {{ {propertyQuery} }})
                            -[:{RELATIONSHIP_BELONGS_TO} {{ createdAt: datetime(), identity: true }}]
                            ->(g:{LABEL_GROUP} {{ key:'identity:{request.Key}', identity: true }})
                            -[:{RELATIONSHIP_CREATED_BY}]->(u)
                            RETURN u";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }
             
            var user = record["u"].As<INode>();
            var newUser = UserFromNode(user);

            await _eventService.SendUserCreatedEvent(newUser);

            return newUser;
        }

        public async Task<User> UpdateUser(string key, UpdateUserRequest request)
        {
            var session = _neo4j.AsyncSession();

            var setQueryParts = BuildEntityQueryParts(request, true);
            var setQuery = string.Join(", ", setQueryParts);
            var query = $@" MATCH (u:{LABEL_USER} {{key: '{key}'}})
                            SET u += {{ {setQuery} }}
                            RETURN u";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var user = record["u"].As<INode>();
            var theUser = UserFromNode(user);

            await _eventService.SendUserUpdatedEvent(theUser);

            return theUser;
        }

        public async Task<User> GetUser(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $"MATCH (u:{LABEL_USER} {{key: '{key}'}}) RETURN u";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var user = record["u"].As<INode>();
            var theUser = UserFromNode(user);

            return theUser;
        }

        public async Task<List<User>> GetAllUsers()
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (u:{LABEL_USER}) 
                            WHERE NOT (u.key = '{Admin.DEFAULT_ADMIN_NAME}')
                            RETURN u";
            _logger.LogDebug($"Executing query: {query}");

            var allUsers = new List<User>();
            try
            {
                var result = await session.RunAsync(query);
     
                await result.ForEachAsync((r) =>
                {
                    var user = r["u"].As<INode>();
                    var theUser = UserFromNode(user);
                    allUsers.Add(theUser);
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return allUsers;
        }

        public async Task DeleteUser(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (u:{LABEL_USER} {{ key: '{key}' }})-[:{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP} {{ identity: true }})
                            DETACH DELETE u
                            DETACH DELETE g";
            _logger.LogDebug($"Executing query: {query}");

            try
            {
                await session.RunAsync(query);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public async Task<List<User>> FindUsers(FindUsersRequest request)
        {
            if (request.Query == null)
            {
                return new List<User>();
            }

            var session = _neo4j.AsyncSession();
            var whereClause = BuildWhereClause(request.Query, "u");
            var query = $@"MATCH (u:{LABEL_USER}) WHERE {whereClause} RETURN u";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var foundUsers = new List<User>();
            try
            {
                await result.ForEachAsync((r) =>
                {
                    var user = r["u"].As<INode>();
                    var theUser = UserFromNode(user);
                    foundUsers.Add(theUser);
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return foundUsers;
        }

        public async Task<List<Group>> GetGroupsForUser(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (u:{LABEL_USER} {{key: '{key}'}})-[:{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP})-[:{RELATIONSHIP_CREATED_BY}]->(createdBy:{LABEL_USER})
                            WHERE NOT EXISTS (g.identity)
                            RETURN g,createdBy";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var allGroups = new List<Group>();
            try
            {
                await result.ForEachAsync((r) =>
                {
                    var group = r["g"].As<INode>();
                    var createdByKey = r["createdBy"].As<INode>()["key"].As<string>();
                    var newGroup = GroupFromNode(group, createdByKey);
                    allGroups.Add(newGroup);
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return allGroups;
        }

        public async Task<bool> IsValidUserKey(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@"MATCH (u:{LABEL_USER} {{key: '{key}'}}) RETURN COUNT(u) = 1 AS isValid";
            _logger.LogDebug($"Executing query: {query}");

            bool isValid;
            try
            {
                var result = await session.RunAsync(query);
                var record = await result.SingleAsync();
                isValid = record["isValid"].As<bool>();
            }
            finally
            {
                await session.CloseAsync();
            }

            return isValid;
        }

        public async Task<Group> GetGroup(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (g:{LABEL_GROUP} {{key: '{key}'}})-[:{RELATIONSHIP_CREATED_BY}]->(u:{LABEL_USER})
                            RETURN g,u";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var group = record["g"].As<INode>();
            var createdByKey = record["u"].As<INode>()["key"].As<string>();
            var theGroup = GroupFromNode(group, createdByKey);

            return theGroup;
        }

        public async Task<List<Group>> GetAllGroups()
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (g:{LABEL_GROUP})-[:{RELATIONSHIP_CREATED_BY}]->(u:{LABEL_USER})
                            WHERE NOT EXISTS (g.identity)
                            RETURN g,u";
            _logger.LogDebug($"Executing query: {query}");

            var allGroups = new List<Group>();
            try
            {
                var result = await session.RunAsync(query);
                await result.ForEachAsync((r) =>
                {   
                    var createdByKey = r["u"].As<INode>()["key"].As<string>();
                    var group = r["g"].As<INode>();
                    var theGroup = GroupFromNode(group, createdByKey);
                    allGroups.Add(theGroup);
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return allGroups;
        }

        public async Task<Group> CreateGroup(CreateGroupRequest request, string createdBy)
        {
            var session = _neo4j.AsyncSession();

            var propertyQueryParts = BuildEntityQueryParts(request, false);
            var propertyQuery = string.Join(", ", propertyQueryParts);
            var query = $@" MATCH (u:{LABEL_USER} {{key: '{createdBy}'}})
                            CREATE (g:{LABEL_GROUP} {{ {propertyQuery} }})
                            CREATE (u)<-[:{RELATIONSHIP_CREATED_BY}]-(g)
                            RETURN g,u";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var group = record["g"].As<INode>();
            var createdByKey = record["u"].As<INode>()["key"].As<string>();
            var newGroup = GroupFromNode(group, createdByKey);

            await _eventService.SendGroupCreatedEvent(newGroup);

            return newGroup;
        }

        public async Task<Group> UpdateGroup(string key, UpdateGroupRequest request)
        {
            var session = _neo4j.AsyncSession();

            var setQueryParts = BuildEntityQueryParts(request, true);
            var setQuery = string.Join(", ", setQueryParts);
            var query = $@" MATCH (g:{LABEL_GROUP} {{key: '{key}'}})-[:{RELATIONSHIP_CREATED_BY}]->(u:{LABEL_USER})
                            SET g += {{ {setQuery} }}
                            RETURN g,u";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var group = record["g"].As<INode>();
            var createdByKey = record["u"].As<INode>()["key"].As<string>();
            var theGroup = GroupFromNode(group, createdByKey);

            //await _eventService.SendGroupUpdatedEvent(theGroup);

            return theGroup;
        }

        public async Task DeleteGroup(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (g:{LABEL_GROUP} {{ key: '{key}' }})
                            DETACH DELETE g";
            _logger.LogDebug($"Executing query: {query}");

            try
            {
                await session.RunAsync(query);
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        public async Task<List<Group>> FindGroups(FindGroupsRequest request)
        {
            if (request.Query == null)
            {
                return new List<Group>();
            }

            var session = _neo4j.AsyncSession();
            var whereClause = BuildWhereClause(request.Query, "g");
            var query = $@" MATCH (g:{LABEL_GROUP})-[:{RELATIONSHIP_CREATED_BY}]->(createdBy:{LABEL_USER}) 
                            WHERE {whereClause} 
                            RETURN g, createdBy";
            _logger.LogDebug($"Executing query: {query}");

            var foundGroups = new List<Group>();
            try
            {
                var result = await session.RunAsync(query);
                await result.ForEachAsync((r) =>
                {
                    var group = r["g"].As<INode>();
                    var createdByKey = r["createdBy"].As<INode>()["key"].As<string>();
                    var theGroup = GroupFromNode(group, createdByKey);
                    foundGroups.Add(theGroup);
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return foundGroups;
        }

        public async Task AddUserToGroup(string key, string userKey)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (g:{LABEL_GROUP} {{key: '{key}'}})-[:{RELATIONSHIP_CREATED_BY}]->(createdBy:{LABEL_USER})
                            MATCH (u:{LABEL_USER} {{key: '{userKey}'}})
                            CREATE (u)-[:{RELATIONSHIP_BELONGS_TO} {{ createdAt: datetime() }}]->(g)
                            RETURN g, u, createdBy";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var group = record["g"].As<INode>();
            var createdByKey = record["createdBy"].As<INode>()["key"].As<string>();
            var theGroup = GroupFromNode(group, createdByKey);

            var user = record["u"].As<INode>();
            var theUser = UserFromNode(user);

            await _eventService.SendAddedUserToGroupEvent(theUser, theGroup);
        }

        public async Task<File> GetFile(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $"MATCH (f:{LABEL_FILE} {{key: '{key}'}}) RETURN f";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var file = record["f"].As<INode>();
            var theFile = FileFromNode(file);

            return theFile;
        }

        public async Task<List<FileVersion>> GetFileVersions(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (f:{LABEL_FILE} {{ key: '{key}' }})-[:{RELATIONSHIP_HAS_VERSION}]->(v:{LABEL_FILE_VERSION}) 
                            RETURN v 
                            ORDER BY v.createdAt ASC";
            _logger.LogDebug($"Executing query: {query}");

            var allFileVersions = new List<FileVersion>();
            try
            {
                var result = await session.RunAsync(query);
                await result.ForEachAsync((r) =>
                {
                    var fileVersion = r["v"].As<INode>();
                    var theFileVersion = FileVersionFromNode(fileVersion);
                    allFileVersions.Add(theFileVersion);
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return allFileVersions;
        }

        public async Task<File> CreateFile(CreateFileRequest request, string createdBy, string versionStorageIdentifier)
        {
            var session = _neo4j.AsyncSession();

            var propertyParts = BuildEntityQueryParts(request, false);
            propertyParts.Add($"versionCount: 1");
            var properties = string.Join(", ", propertyParts);
            var query = $@" CREATE (f:{LABEL_FILE} {{ {properties} }}) 
                            CREATE (v:{LABEL_FILE_VERSION} {{ storageService: '{request.StorageService}', storageId: '{versionStorageIdentifier}', createdAt: datetime(), createdBy: '{createdBy}' }})
                            CREATE (f)-[:{RELATIONSHIP_HAS_VERSION} {{ createdAt: datetime() }}]->(v)
                            CREATE (c:{LABEL_COLLECTION} {{key: 'identity:{request.Key}', identity: true}})-[:{RELATIONSHIP_CONTAINS} {{ createdAt: datetime(), identity: true }}]->(f)
                            WITH f, c
                            MATCH (u:{LABEL_USER} {{key: '{createdBy}'}})-[{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP} {{ identity:true }})
                            CREATE (g)-[:{AccessLevel.CO_OWNER} {{ createdAt: datetime() }}]->(c)
                            CREATE (u)<-[:{RELATIONSHIP_CREATED_BY}]-(f)
                            RETURN f";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var file = record["f"].As<INode>();
            var newFile = FileFromNode(file);

            await _eventService.SendFileCreatedEvent(newFile);
            
            return newFile;
        }

        public async Task<File> UpdateFile(string key, UpdateFileRequest request)
        {
            var session = _neo4j.AsyncSession();

            var setQueryParts = this.BuildEntityQueryParts(request, true);
            var setQuery = string.Join(", ", setQueryParts);
            var query = $@" MATCH (f:{LABEL_FILE} {{key: '{key}'}})
                            SET f += {{ {setQuery} }}
                            RETURN f";
            _logger.LogDebug($"Executing query: {query}");

            IRecord record;
            try
            {
                var result = await session.RunAsync(query);
                record = await result.SingleAsync();
            }
            finally
            {
                await session.CloseAsync();
            }

            var file = record["f"].As<INode>();
            var theFile = FileFromNode(file);

            //await _eventService.SendRetentionPolicyUpdatedEvent(theRetentionPolicy);

            return theFile;
        }

        public async Task DeleteFile(string key)
        {
            File theFile = await this.GetFile(key);

            // check retention policy applications to see if deletion is prevented
            var retentionPolicyApplications = await this.GetRetentionPolicyApplicationsForFile(key);

            var now = DateTime.UtcNow;
            var preventDelete = retentionPolicyApplications.Any(rpa => rpa.ExpiresAt > now);
            if (preventDelete)
            {
                throw new InvalidOperationException($"Attempted to delete file with active retention policy: FileKey='{theFile.Key}'");
            }

            var allFileVersions = await this.GetFileVersions(key);
            
            
            foreach (var fileVersionInfo in allFileVersions)
            {
                var storageService = _storageServiceProvider.Service(fileVersionInfo.StorageService);
                var storageId = fileVersionInfo.StorageId;
                await storageService.DeleteFile(storageId);

                var sessionForLoop = _neo4j.AsyncSession();
                try
                {
                    var versionQuery = $@"  MATCH (f:{LABEL_FILE} {{ key: '{key}' }})-[:{RELATIONSHIP_HAS_VERSION}]->(v:{LABEL_FILE_VERSION} {{ storageId: '{storageId}' }})
                                            DETACH DELETE v";
                    _logger.LogDebug($"Executing query: {versionQuery}");
                    await sessionForLoop.RunAsync(versionQuery);

                    _logger.LogDebug($"Deleted File Version: FileKey='{theFile.Key}', StorageService='{storageService.Name}', StorageId='{storageId}'");
                }
                finally
                {
                    await sessionForLoop.CloseAsync();
                }
            }

            var query = $@" MATCH (ic:{LABEL_COLLECTION} {{identity: true}})-[:{RELATIONSHIP_CONTAINS}]->(f:{LABEL_FILE} {{ key: '{key}' }})
                            MATCH (f:{LABEL_FILE} {{ key: '{key}' }})
                            DETACH DELETE ic
                            DETACH DELETE f";
            _logger.LogDebug($"Executing query: {query}");

            var session = _neo4j.AsyncSession();
            try
            {
                await session.RunAsync(query);
            }
            finally
            {
                await session.CloseAsync();
            }
            _logger.LogDebug($"Deleted File: FileKey='{theFile.Key}'");   
        }

        public async Task<List<Collection>> GetCollectionsForFile(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (u:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION})-[:{RELATIONSHIP_CONTAINS}]->(f:{LABEL_FILE} {{ key: '{key}' }})
                            WHERE NOT EXISTS (c.identity)
                            RETURN c,u";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var allCollections = new List<Collection>();
            await result.ForEachAsync((r) =>
            {
                var collection = r["c"].As<INode>();
                var createdByKey = r["u"].As<INode>()["key"].As<string>();
                var theCollection = CollectionFromNode(collection, createdByKey);
                allCollections.Add(theCollection);
            });
            await session.CloseAsync();

            return allCollections;
        }

        public async Task<File> AddVersionToFile(string key, string storageService, string versionStorageIdentifier, string createdBy)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (f:{LABEL_FILE} {{ key: '{key}' }})
                            CALL apoc.atomic.add(f,'versionCount',1,5) YIELD newValue
                            WITH f
                            SET f.modifiedAt = datetime()
                            CREATE (v:{LABEL_FILE_VERSION} {{   storageService: '{storageService}', 
                                                                storageId: '{versionStorageIdentifier}', 
                                                                createdAt: datetime(),
                                                                createdBy: '{createdBy}' }})
                            CREATE (f)-[:{RELATIONSHIP_HAS_VERSION} {{ createdAt: datetime() }}]->(v)
                            RETURN f, v";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var file = record["f"].As<INode>();
            var theFile = FileFromNode(file);

            var fileVersion = record["v"].As<INode>();
            var theFileVersion = FileVersionFromNode(fileVersion);

            await _eventService.SendNewVersionOfFileCreatedEvent(theFile, theFileVersion);

            return theFile;
        }

        public async Task<FileVersion> GetLatestVersionForFile(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (f:{LABEL_FILE} {{ key: '{key}' }})-[:{RELATIONSHIP_HAS_VERSION}]->(v:{LABEL_FILE_VERSION}) 
                            RETURN v 
                            ORDER BY v.createdAt DESC 
                            LIMIT 1";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var fileVersion = record["v"].As<INode>();
            var theFileVersion = FileVersionFromNode(fileVersion);

            return theFileVersion;
        }

        public async Task<List<RetentionPolicyApplication>> GetRetentionPolicyApplicationsForFile(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (f:{LABEL_FILE} {{ key: '{key}' }})-[rpr:{RELATIONSHIP_HAS_RETENTION_POLICY}]->(rp:{LABEL_RETENTION_POLICY}) 
                            RETURN rp, rpr
                            ORDER BY rpr.appliedAt ASC";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var retentionPolicyApplications = new List<RetentionPolicyApplication>();
            await result.ForEachAsync((rp) => 
            {
                var retentionPolicy = rp["rp"].As<INode>();
                var retentionPolicyApplication = rp["rpr"].As<IRelationship>();
                var theRetentionPolicyApplication = RetentionPolicyApplicationFromNode(retentionPolicy, retentionPolicyApplication);
                retentionPolicyApplications.Add(theRetentionPolicyApplication);
            });
            await session.CloseAsync();

            return retentionPolicyApplications;
        }

        public async Task AddUserToFile(string key, AddUserToFileRequest request)
        {
            if(!AccessLevel.IsValid(request.AccessLevel))
            {
                throw new ArgumentException($"'{request.AccessLevel}' is not a valid Access Level (did you use all uppercase?)");
            }

            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (c:{LABEL_COLLECTION} {{ identity: true }})-[:{RELATIONSHIP_CONTAINS}]->(f:{LABEL_FILE} {{ key: '{key}' }})
                            MATCH (u:{LABEL_USER} {{ key: '{request.UserKey}' }})-[:{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP} {{ identity: true }})
                            CREATE (g)-[:{request.AccessLevel} {{ createdAt: datetime() }}]->(c)
                            RETURN f";
            _logger.LogDebug($"Executing query: {query}");
            
            await session.RunAsync(query);
            await session.CloseAsync();
        }

        public async Task AddGroupToFile(string key, AddGroupToFileRequest request)
        {
            if(!AccessLevel.IsValid(request.AccessLevel))
            {
                throw new ArgumentException($"'{request.AccessLevel}' is not a valid Access Level (did you use all uppercase?)");
            }

            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (c:{LABEL_COLLECTION} {{ identity: true }})-[:{RELATIONSHIP_CONTAINS}]->(f:{LABEL_FILE} {{ key: '{key}' }})
                            MATCH (g:{LABEL_GROUP} {{ key: '{request.GroupKey}' }})
                            CREATE (g)-[:{request.AccessLevel} {{ createdAt: datetime() }}]->(c)
                            RETURN f";
            _logger.LogDebug($"Executing query: {query}");

            await session.RunAsync(query);
            await session.CloseAsync();
        }

        public async Task AddRetentionPolicyToFile(string key, AddRetentionPolicyToFileRequest request)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (f:{LABEL_FILE} {{ key: '{key}' }})
                            MATCH (rp:{LABEL_RETENTION_POLICY} {{ key: '{request.RetentionPolicyKey}' }})
                            CREATE (f)-[:{RELATIONSHIP_HAS_RETENTION_POLICY} {{ appliedAt: datetime() }}]->(rp)
                            RETURN f";
            _logger.LogDebug($"Executing query: {query}");

            await session.RunAsync(query);
            await session.CloseAsync();
        }

        // Saving for future reference
        // expiresAt: datetime({{ epochMillis: apoc.date.add(apoc.date.currentTimestamp(), 'ms', rp.days, 'd') }}) 

        public async Task AddClassificationToFile(string key, AddClassificationToFileRequest request)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (f:{LABEL_FILE} {{ key: '{key}' }})
                            MATCH (c:{LABEL_CLASSIFICATION} {{ key: '{request.ClassificationKey}' }})
                            CREATE (f)-[:{RELATIONSHIP_HAS_CLASSIFICATION} {{ createdAt: datetime() }}]->(c)
                            RETURN f";
            _logger.LogDebug($"Executing query: {query}");

            await session.RunAsync(query);
            await session.CloseAsync();
        }

        public async Task<bool> UserCanAccessFile(string userKey, string fileKey, List<string> accessLevels)
        {
            var results = await UserCanAccessFiles(userKey, new List<string>{fileKey}, accessLevels);
            return results.Count > 0;
        }

        public async Task<List<string>> UserCanAccessFiles(string userKey, List<string> fileKeys, List<string> accessLevels)
        {
            foreach(string accessLevel in accessLevels)
            {
                if(!AccessLevel.IsValid(accessLevel))
                {
                    throw new ArgumentException($"'{accessLevel}' is not a valid Access Level (did you use all uppercase?)");
                }
            }
            
            var quotedFiles = fileKeys.Select((f) => $"'{f}'");
            var fileList = string.Join(',', quotedFiles);
            var accessList = string.Join('|', accessLevels);

            var session = _neo4j.AsyncSession();

            var query = $@" MATCH (u:{LABEL_USER} {{key: '{userKey}'}})-[:{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP})
                            MATCH (f:{LABEL_FILE})<-[:{RELATIONSHIP_CONTAINS}]-(root:{LABEL_COLLECTION})<-[:{accessList}]-(g) 
                            WHERE f.key IN [{fileList}]
                            RETURN f.key AS fileKey
                            UNION
                            MATCH (u:{LABEL_USER} {{key: '{userKey}'}})-[:{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP})
                            MATCH (f:{LABEL_FILE})<-[:{RELATIONSHIP_CONTAINS}]-(:{LABEL_COLLECTION})-[:{RELATIONSHIP_CHILD_OF}*]->(parent:{LABEL_COLLECTION})<-[:{accessList}]-(g) 
                            WHERE f.key IN [{fileList}]
                            RETURN f.key AS fileKey";

            _logger.LogDebug($"Executing query: {query}");

            var accessResults = new List<string>();
            var result = await session.RunAsync(query);

            await result.ForEachAsync((r) =>
            {
                var fileKey = r["fileKey"].As<string>();
                accessResults.Add(fileKey);
            });
            await session.CloseAsync();

            return accessResults;

            /*
                match (u:User {key: 'user-1'})-[:BELONGS_TO]->(g:Group)
                match (f:File)<-[:CONTAINS]-(root:Collection)<-[:READ_ONLY]-(g) where f.key in ['file-1','file-2']
                return f.key AS allowedFileKeys
                UNION
                match (u:User {key: 'user-1'})-[:BELONGS_TO]->(g:Group)
                match (f:File)<-[:CONTAINS]-(:Collection)-[:CHILD_OF*]->(parent:Collection)<-[:READ_ONLY]-(g) where f.key in ['file-1','file-2']
                return f.key AS allowedFileKeys
            */
        }

        public async Task<Collection> GetCollection(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (u:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION} {{key: '{key}'}}) 
                            RETURN c,u";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var collection = record["c"].As<INode>();
            var createdByKey = record["u"].As<INode>()["key"].As<string>();
            var theCollection = CollectionFromNode(collection, createdByKey);

            return theCollection;
        }

        public async Task<List<Collection>> GetAllCollections()
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (u:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION}) 
                            WHERE NOT EXISTS (c.identity)
                            RETURN c,u";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var allCollections = new List<Collection>();
            await result.ForEachAsync((r) =>
            {
                var collection = r["c"].As<INode>();
                var createdByKey = r["u"].As<INode>()["key"].As<string>();
                var theCollection = CollectionFromNode(collection, createdByKey);
                allCollections.Add(theCollection);
            });
            await session.CloseAsync();

            return allCollections;
        }

        public async Task<Collection> CreateCollection(CreateCollectionRequest request, string createdBy)
        {
            var session = _neo4j.AsyncSession();

            var propertyQueryParts = BuildEntityQueryParts(request, false);
            var propertyQuery = string.Join(", ", propertyQueryParts);
            var query = $@" MATCH (u:{LABEL_USER} {{key: '{createdBy}'}})-[:{RELATIONSHIP_BELONGS_TO}]->(ig:{LABEL_GROUP} {{ identity:true }})
                            CREATE (u)<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION} {{ {propertyQuery} }})<-[:{AccessLevel.CO_OWNER} {{ createdAt: datetime() }}]-(ig)
                            RETURN c, u";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();
            
            var collection = record["c"].As<INode>();
            var createdByKey = record["u"].As<INode>()["key"].As<string>();
            var newCollection = CollectionFromNode(collection, createdByKey);

            await _eventService.SendCollectionCreatedEvent(newCollection);

            return newCollection;
        }

        public async Task<Collection> UpdateCollection(string key, UpdateCollectionRequest request)
        {
            var session = _neo4j.AsyncSession();

            var setQueryParts = BuildEntityQueryParts(request, true);
            var setQuery = string.Join(", ", setQueryParts);
            var query = $@" MATCH (u:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION} {{key: '{key}'}})
                            SET c += {{ {setQuery} }}
                            RETURN c,u";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var collection = record["c"].As<INode>();
            var createdByKey = record["u"].As<INode>()["key"].As<string>();
            var theCollection = CollectionFromNode(collection, createdByKey);

            //await _eventService.SendGroupUpdatedEvent(theGroup);

            return theCollection;
        }

        public async Task DeleteCollection(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (c:{LABEL_COLLECTION} {{ key: '{key}' }})
                            WHERE NOT EXISTS (c.identity)
                            DETACH DELETE c";
            _logger.LogDebug($"Executing query: {query}");

            await session.RunAsync(query);
            await session.CloseAsync();
        }

        public async Task<List<Collection>> FindCollections(FindCollectionsRequest request)
        {
            if (request.Query == null)
            {
                return new List<Collection>();
            }

            var session = _neo4j.AsyncSession();
            var whereClause = BuildWhereClause(request.Query, "c");
            var query = $@" MATCH (u:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION}) 
                            WHERE {whereClause} 
                            RETURN c,u";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var foundCollections = new List<Collection>();
            await result.ForEachAsync((r) =>
            {
                var collection = r["c"].As<INode>();
                var createdByKey = r["u"].As<INode>()["key"].As<string>();
                var theCollection = CollectionFromNode(collection, createdByKey);
                foundCollections.Add(theCollection);
            });
            await session.CloseAsync();

            return foundCollections;
        }

        public async Task<List<File>> GetFilesForCollection(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $"MATCH (c:{LABEL_COLLECTION} {{key: '{key}'}})-[:{RELATIONSHIP_CONTAINS}]->(f:{LABEL_FILE}) RETURN f";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var allFiles = new List<File>();
            await result.ForEachAsync((r) =>
            {
                var file = r["f"].As<INode>();
                var theFile = FileFromNode(file);
                allFiles.Add(theFile);
            });
            await session.CloseAsync();

            return allFiles;
        }

        public async Task<List<Collection>> GetChildCollectionsForCollection(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (c:{LABEL_COLLECTION} {{key: '{key}'}})<-[:{RELATIONSHIP_CHILD_OF}]-(child:{LABEL_COLLECTION})-[:{RELATIONSHIP_CREATED_BY}]->(childCreatedBy:{LABEL_USER})
                            RETURN child, childCreatedBy";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var allChildren = new List<Collection>();
            await result.ForEachAsync((r) =>
            {
                var child = r["child"].As<INode>();
                var createdByKey = r["childCreatedBy"].As<INode>()["key"].As<string>();
                var theChild = CollectionFromNode(child, createdByKey);
                allChildren.Add(theChild);
            });
            await session.CloseAsync();

            return allChildren;
        }

        public async Task<CollectionItems> GetItemsForCollection(string key)
        {
            var files = await this.GetFilesForCollection(key);
            var children = await this.GetChildCollectionsForCollection(key);

            var result = new CollectionItems { ChildCollections=children, Files=files };
            return result;
        }

        public async Task RemoveFileFromCollection(string key, string fileKey)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (c:{LABEL_COLLECTION} {{ key: '{key}' }})-[r:{RELATIONSHIP_CONTAINS}]->(f:{LABEL_FILE} {{ key: '{fileKey}' }})
                            DELETE r";
            _logger.LogDebug($"Executing query: {query}");

            await session.RunAsync(query);
            await session.CloseAsync();
        }

        public async Task AddChildToCollection(string parentKey, string childKey)
        {
            var session = _neo4j.AsyncSession();

            // Check if adding the child collection will create a cycle
            var cycleQuery = $@"MATCH (r:{LABEL_COLLECTION} {{ key: '{parentKey}' }})-[:{RELATIONSHIP_CHILD_OF}*]->(p:{LABEL_COLLECTION} {{ key: '{childKey}' }})
                                RETURN count(p) > 0 AS foundCycle";
            _logger.LogDebug($"Executing query: {cycleQuery}");

            var result = await session.RunAsync(cycleQuery);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var foundCycle = record["foundCycle"].As<bool>();
            if (foundCycle)
            {
                throw new Exception($"Attempt to add collection '{childKey}' as child of collection '{parentKey}' is invalid as it would create a cycle.");
            }
            
            // No cycle found so add the child
            session = _neo4j.AsyncSession();
            var query = $@" MATCH (up:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(parent:{LABEL_COLLECTION} {{key: '{parentKey}'}})
                            MATCH (uc:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(child:{LABEL_COLLECTION} {{key: '{childKey}'}})
                            CREATE (child)-[:{RELATIONSHIP_CHILD_OF} {{ createdAt: datetime() }}]->(parent)
                            RETURN parent, child, up, uc";
            _logger.LogDebug($"Executing query: {query}");

            result = await session.RunAsync(query);
            record = await result.SingleAsync();
            await session.CloseAsync();
            
            var parent = record["parent"].As<INode>();
            var parentCreatedByKey = record["up"].As<INode>()["key"].As<string>();
            var parentCollection = CollectionFromNode(parent, parentCreatedByKey);

            var child = record["child"].As<INode>();
            var childCreatedByKey = record["uc"].As<INode>()["key"].As<string>();
            var childCollection = CollectionFromNode(child, childCreatedByKey);

            await _eventService.SendAddedChildToCollectionEvent(childCollection, parentCollection);
        }

        public async Task AddUserToCollection(string key, AddUserToCollectionRequest request)
        {
            if(!AccessLevel.IsValid(request.AccessLevel))
            {
                throw new ArgumentException($"'{request.AccessLevel}' is not a valid Access Level (did you use all uppercase?)");
            }

            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (collectionCreatedBy:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION} {{key: '{key}'}})
                            MATCH (u:{LABEL_USER} {{key: '{request.UserKey}'}})-[:{RELATIONSHIP_BELONGS_TO}]->(g:Group {{identity: true}})-[:{RELATIONSHIP_CREATED_BY}]->(groupCreatedBy:{LABEL_USER})
                            CREATE (g)-[:{request.AccessLevel} {{ createdAt: datetime() }}]->(c)
                            RETURN c, u, collectionCreatedBy, groupCreatedBy";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var collection = record["c"].As<INode>();
            var collectionCreatedByKey = record["collectionCreatedBy"].As<INode>()["key"].As<string>();
            var theCollection = CollectionFromNode(collection, collectionCreatedByKey);

            var user = record["u"].As<INode>();
            var groupCreatedByKey = record["groupCreatedBy"].As<INode>()["key"].As<string>();
            var theUser = GroupFromNode(user, groupCreatedByKey);

            //await _eventService.SendAddedUserToCollectionEvent(theUser, theCollection);
        }

        public async Task AddGroupToCollection(string key, AddGroupToCollectionRequest request)
        {
            if(!AccessLevel.IsValid(request.AccessLevel))
            {
                throw new ArgumentException($"'{request.AccessLevel}' is not a valid Access Level (did you use all uppercase?)");
            }

            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (collectionCreatedBy:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION} {{key: '{key}'}})
                            MATCH (groupCreatedBy:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(g:{LABEL_GROUP} {{key: '{request.GroupKey}'}})
                            CREATE (g)-[:{request.AccessLevel} {{ createdAt: datetime() }}]->(c)
                            RETURN c, g, collectionCreatedBy, groupCreatedBy";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var collection = record["c"].As<INode>();
            var collectionCreatedByKey = record["collectionCreatedBy"].As<INode>()["key"].As<string>();
            var theCollection = CollectionFromNode(collection, collectionCreatedByKey);

            var group = record["g"].As<INode>();
            var groupCreatedByKey = record["groupCreatedBy"].As<INode>()["key"].As<string>();
            var theGroup = GroupFromNode(group, groupCreatedByKey);

            await _eventService.SendAddedGroupToCollectionEvent(theGroup, theCollection);
        }

        public async Task AddFileToCollection(string key, AddFileToCollectionRequest request)
        {
            var session = _neo4j.AsyncSession();
            var query = $@" MATCH (collectionCreatedBy:{LABEL_USER})<-[:{RELATIONSHIP_CREATED_BY}]-(c:{LABEL_COLLECTION} {{key: '{key}'}})
                            MATCH (f:{LABEL_FILE} {{key: '{request.FileKey}'}})
                            CREATE (c)-[:{RELATIONSHIP_CONTAINS} {{ createdAt: datetime() }}]->(f)
                            RETURN c, f, collectionCreatedBy";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var collection = record["c"].As<INode>();
            var collectionCreatedByKey = record["collectionCreatedBy"].As<INode>()["key"].As<string>();
            var theCollection = CollectionFromNode(collection, collectionCreatedByKey);

            var file = record["f"].As<INode>();
            var theFile = FileFromNode(file);

            await _eventService.SendAddedFileToCollectionEvent(theFile, theCollection);
        }

        public async Task<bool> UserCanAccessCollection(string userKey, string collectionKey, List<string> accessLevels)
        {
            var results = await UserCanAccessCollections(userKey, new List<string>{collectionKey}, accessLevels);
            return results.Count > 0;
        }

        public async Task<List<string>> UserCanAccessCollections(string userKey, List<string> collectionKeys, List<string> accessLevels)
        {
            foreach(string accessLevel in accessLevels)
            {
                if(!AccessLevel.IsValid(accessLevel))
                {
                    throw new ArgumentException($"'{accessLevel}' is not a valid Access Level (did you use all uppercase?)");
                }
            }
            
            var quotedCollections = collectionKeys.Select((c) => $"'{c}'");
            var collectionList = string.Join(',', quotedCollections);
            var accessList = string.Join('|', accessLevels);

            var session = _neo4j.AsyncSession();

            var query = $@" MATCH (u:{LABEL_USER} {{key: '{userKey}'}})-[:{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP})
                            MATCH (root:{LABEL_COLLECTION})<-[:{accessList}]-(g) 
                            WHERE root.key IN [{collectionList}]
                            RETURN root.key AS collectionKey
                            UNION
                            MATCH (u:{LABEL_USER} {{key: '{userKey}'}})-[:{RELATIONSHIP_BELONGS_TO}]->(g:{LABEL_GROUP})
                            MATCH (c:{LABEL_COLLECTION})-[:{RELATIONSHIP_CHILD_OF}*]->(parent:{LABEL_COLLECTION})<-[:{accessList}]-(g) 
                            WHERE c.key IN [{collectionList}]
                            RETURN c.key AS collectionKey";

            _logger.LogDebug($"Executing query: {query}");

            var accessResults = new List<string>();
            var result = await session.RunAsync(query);

            await result.ForEachAsync((r) =>
            {
                var collectionKey = r["collectionKey"].As<string>();
                accessResults.Add(collectionKey);
            });
            await session.CloseAsync();

            return accessResults;
        }

        public async Task<RetentionPolicy> GetRetentionPolicy(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $"MATCH (c:{LABEL_RETENTION_POLICY} {{key: '{key}'}}) RETURN c";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var retentionPolicy = record["c"].As<INode>();
            var theRetentionPolicy = RetentionPolicyFromNode(retentionPolicy);

            return theRetentionPolicy;
        }

        public async Task<RetentionPolicy> CreateRetentionPolicy(CreateRetentionPolicyRequest request)
        {
            var session = _neo4j.AsyncSession();

            var propertyQueryParts = BuildEntityQueryParts(request, false);
            propertyQueryParts.Add($"seconds: {request.Seconds}");
            var propertyQuery = string.Join(", ", propertyQueryParts);
            var query = $@"CREATE (rp:{LABEL_RETENTION_POLICY} {{ {propertyQuery} }}) RETURN rp";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var retentionPolicy = record["rp"].As<INode>();
            var newRetentionPolicy = RetentionPolicyFromNode(retentionPolicy);

            //await _eventService.SendRetentionPolicyCreatedEvent(newGroup);

            return newRetentionPolicy;
        }

        public async Task<RetentionPolicy> UpdateRetentionPolicy(string key, UpdateRetentionPolicyRequest request)
        {
            var session = _neo4j.AsyncSession();

            var setQueryParts = BuildEntityQueryParts(request, true);

            if (request.Seconds.HasValue)
            {
                var existingPolicy = await this.GetRetentionPolicy(key);
                if (existingPolicy.Seconds > request.Seconds.Value)
                {
                    throw new ArgumentException($"The requested retention period of {request.Seconds} seconds is less than the existing retention period of {existingPolicy.Seconds} seconds; Not Allowed. Retention Policy Key: {key}");
                }
                else
                {
                    setQueryParts.Add($"seconds: {request.Seconds}");
                }
            }

            var setQuery = string.Join(", ", setQueryParts);
            var query = $@" MATCH (rp:{LABEL_RETENTION_POLICY} {{key: '{key}'}})
                            SET rp += {{ {setQuery} }}
                            RETURN rp";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var retentionPolicy = record["rp"].As<INode>();
            var theRetentionPolicy = RetentionPolicyFromNode(retentionPolicy);

            //await _eventService.SendRetentionPolicyUpdatedEvent(theRetentionPolicy);

            return theRetentionPolicy;
        }

        public async Task<List<RetentionPolicy>> GetAllRetentionPolicies()
        {
            var session = _neo4j.AsyncSession();
            var query = $" MATCH (rp:{LABEL_RETENTION_POLICY}) RETURN rp";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var allPolicies = new List<RetentionPolicy>();
            await result.ForEachAsync((r) =>
            {
                var policy = r["rp"].As<INode>();
                var thePolicy = RetentionPolicyFromNode(policy);
                allPolicies.Add(thePolicy);
            });
            await session.CloseAsync();

            return allPolicies;
        }

        public async Task<List<RetentionPolicy>> FindRetentionPolicies(FindRetentionPoliciesRequest request)
        {
            if (request.Query == null)
            {
                return new List<RetentionPolicy>();
            }

            var session = _neo4j.AsyncSession();
            var whereClause = BuildWhereClause(request.Query, "rp");
            var query = $@"MATCH (rp:{LABEL_RETENTION_POLICY}) WHERE {whereClause} RETURN rp";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var foundPolicies = new List<RetentionPolicy>();
            await result.ForEachAsync((r) =>
            {
                var policy = r["rp"].As<INode>();
                var thePolicy = RetentionPolicyFromNode(policy);
                foundPolicies.Add(thePolicy);
            });
            await session.CloseAsync();

            return foundPolicies;
        }

        public async Task<Classification> GetClassification(string key)
        {
            var session = _neo4j.AsyncSession();
            var query = $"MATCH (c:{LABEL_CLASSIFICATION} {{key: '{key}'}}) RETURN c";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var classification = record["c"].As<INode>();
            var theClassification = ClassificationFromNode(classification);

            return theClassification;
        }

        public async Task<Classification> CreateClassification(CreateClassificationRequest request)
        {
            var session = _neo4j.AsyncSession();

            var propertyQueryParts = BuildEntityQueryParts(request, false);
            var propertyQuery = string.Join(", ", propertyQueryParts);
            var query = $@"CREATE (c:{LABEL_CLASSIFICATION} {{ {propertyQuery} }}) RETURN c";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var classification = record["c"].As<INode>();
            var newClassification = ClassificationFromNode(classification);

            //await _eventService.SendClassificationCreatedEvent(newGroup);

            return newClassification;
        }

        public async Task<Classification> UpdateClassification(string key, UpdateClassificationRequest request)
        {
            var session = _neo4j.AsyncSession();

            var setQueryParts = BuildEntityQueryParts(request, true);
            var setQuery = string.Join(", ", setQueryParts);
            var query = $@" MATCH (c:{LABEL_CLASSIFICATION} {{key: '{key}'}})
                            SET c += {{ {setQuery} }}
                            RETURN c";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);
            var record = await result.SingleAsync();
            await session.CloseAsync();

            var classification = record["c"].As<INode>();
            var theClassification = ClassificationFromNode(classification);

            //await _eventService.SendGroupUpdatedEvent(theGroup);

            return theClassification;
        }

        public async Task<List<Classification>> FindClassifications(FindClassificationsRequest request)
        {
            if (request.Query == null)
            {
                return new List<Classification>();
            }

            var session = _neo4j.AsyncSession();
            var whereClause = BuildWhereClause(request.Query, "c");
            var query = $@"MATCH (c:{LABEL_CLASSIFICATION}) WHERE {whereClause} RETURN c";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var foundClassifications = new List<Classification>();
            await result.ForEachAsync((r) =>
            {
                var classification = r["c"].As<INode>();
                var theClassification = ClassificationFromNode(classification);
                foundClassifications.Add(theClassification);
            });
            await session.CloseAsync();

            return foundClassifications;
        }

        public async Task<List<Classification>> GetAllClassifications()
        {
            var session = _neo4j.AsyncSession();
            var query = $" MATCH (c:{LABEL_CLASSIFICATION}) RETURN c";
            _logger.LogDebug($"Executing query: {query}");

            var result = await session.RunAsync(query);

            var allClassifications = new List<Classification>();
            await result.ForEachAsync((r) =>
            {
                var classification = r["c"].As<INode>();
                var theClassification = ClassificationFromNode(classification);
                allClassifications.Add(theClassification);
            });
            await session.CloseAsync();

            return allClassifications;
        }

        private User UserFromNode(INode node)
        {
            var u = new User();
            ExtractEntityProperties(u, node);
            return u;
        }

        private Group GroupFromNode(INode node, string createdByKey)
        {
            var g = new Group();
            ExtractEntityProperties(g, node, createdByKey);
            return g;
        }

        private File FileFromNode(INode node)
        {
            var f = new File();
            ExtractEntityProperties(f, node);
            f.VersionCount = node["versionCount"].As<int>();
            return f;
        }

        private FileVersion FileVersionFromNode(INode node)
        {
            var fv = new FileVersion
            {
                StorageService = node["storageService"].As<string>(),
                StorageId = node["storageId"].As<string>(),
                CreatedAt = node["createdAt"].As<DateTimeOffset>(),
                CreatedBy = node["createdBy"].As<string>()
            };
            return fv;
        }

        private Collection CollectionFromNode(INode node, string createdByKey)
        {
            var c = new Collection();
            ExtractEntityProperties(c, node, createdByKey);
            return c;
        }

        private RetentionPolicy RetentionPolicyFromNode(INode node)
        {
            var rp = new RetentionPolicy();
            ExtractEntityProperties(rp, node);
            rp.Seconds = node["seconds"].As<uint>();
            return rp;
        }

        private RetentionPolicyApplication RetentionPolicyApplicationFromNode(INode node, IRelationship rel)
        {
            var rp = this.RetentionPolicyFromNode(node);

            var appliedAt = rel["appliedAt"].As<DateTimeOffset>();
            var expiresAt = appliedAt.AddSeconds(rp.Seconds);

            var rpa = new RetentionPolicyApplication {
                RetentionPolicy = rp,
                AppliedAt = appliedAt,
                ExpiresAt = expiresAt
            };

            return rpa;
        }

        private Classification ClassificationFromNode(INode node)
        {
            var c = new Classification();
            ExtractEntityProperties(c, node);
            return c;
        }

        private IList<string> BuildEntityQueryParts<T>(T request, bool isUpdate) where T:CreateOrUpdateEntityRequest
        {
            var propertyQueryParts = new List<string>();

            if (request.Key != null)
            {
                if (isUpdate)
                {
                    throw new ArgumentException("Key cannot be modified");
                }
                else if (string.IsNullOrWhiteSpace(request.Key))
                {
                    throw new ArgumentException("Value for 'key' cannot be an empty string");
                }
                else
                {
                    propertyQueryParts.Add($"key: '{request.Key}'");
                }         
            }
            
            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    propertyQueryParts.Add($"name: null");
                }
                else
                {
                    propertyQueryParts.Add($"name: '{request.Name}'");
                }      
            }

            if (request.Description != null)
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    propertyQueryParts.Add($"description: null");
                }
                else
                {
                    propertyQueryParts.Add($"description: '{request.Description}'");
                } 
            }

            if (!isUpdate)
            {
                propertyQueryParts.Add("createdAt: datetime()");
            }
            
            propertyQueryParts.Add("modifiedAt: datetime()");

            if (request.Metadata != null)
            {
                ProcessMetadata(request.Metadata, propertyQueryParts);
            }

            return propertyQueryParts;   
        }

        private void ExtractEntityProperties<T>(T entity, INode node, string createdByKey=null) where T:Entity
        {
            entity.CreatedBy = createdByKey;
            var metadata = new Dictionary<string, object>();
            var props = node.Properties;
            foreach (var prop in props)
            {
                switch (prop.Key)
                {
                    case "key":
                        entity.Key = prop.Value.As<string>();
                        break;
                    case "name":
                        entity.Name = prop.Value.As<string>();
                        break;
                    case "description":
                        entity.Description = prop.Value.As<string>();
                        break;
                    case "createdAt":
                        entity.CreatedAt = prop.Value.As<DateTimeOffset>();
                        break;
                    case "modifiedAt":
                        entity.ModifiedAt = prop.Value.As<DateTimeOffset>();
                        break;
                    default:
                        // all other properties that start with METADATA_PREFIX should be put into the Metadata field
                        if (prop.Key.StartsWith(METADATA_PREFIX))
                        {
                            var keyName = prop.Key.Remove(0, METADATA_PREFIX.Length);
                            try
                            {
                                var theDateUtc = prop.Value.As<DateTimeOffset>().UtcDateTime;
                                metadata.Add(keyName, theDateUtc);
                            }
                            catch(InvalidCastException)
                            {
                                metadata.Add(keyName, prop.Value);
                            }
                        }
                        break;
                }
            }

            if (metadata.Count > 0) entity.Metadata = metadata;
        }

        private void ProcessMetadata(Dictionary<string,object> metadata, List<string> queryParts)
        {
            foreach(var item in metadata)
            {
                var value = item.Value;
                if (value == null)
                {
                    queryParts.Add($"{METADATA_PREFIX}{item.Key}: null");
                }
                else
                {
                    JsonElement element = (JsonElement)value;
                    if(element.ValueKind == JsonValueKind.String)
                    {
                        if (element.TryGetDateTime(out var specifiedDateTime))
                        {
                            //special case where a DateTime has been specified and parsed
                            var epochMillis = new DateTimeOffset(specifiedDateTime).ToUnixTimeMilliseconds();
                            queryParts.Add($"{METADATA_PREFIX}{item.Key}: datetime({{ epochMillis: {epochMillis} }})");
                        }
                        else
                        {
                            queryParts.Add($"{METADATA_PREFIX}{item.Key}: '{element}'");
                        }        
                    }
                    else if (element.ValueKind == JsonValueKind.True || 
                             element.ValueKind == JsonValueKind.False ||
                             element.ValueKind == JsonValueKind.Number)
                    {
                        queryParts.Add($"{METADATA_PREFIX}{item.Key}: {element}");
                    }
                }      
            }
        }

        private string BuildWhereClause(List<Dictionary<string,object>> items, string nodeName)
        {
            var whereClauseItems = new List<string>();

            foreach (var item in items)
            {    
                var itemsIgnoreCase = new Dictionary<string, object>(item, CASE_INSENSITIVE_COMPARER);

                string op, queryText = null;
                if (itemsIgnoreCase.Keys.Contains("PROP"))
                {
                    var prop = ((JsonElement)itemsIgnoreCase["PROP"]).ToString();

                    //take account of Metadata properties that have the special prefix
                    if (!ENTITY_PROPERTIES.Contains(prop))
                    {
                        prop = $"{METADATA_PREFIX}{prop}";
                    }

                    if (prop.Any(Char.IsWhiteSpace))
                    {
                        throw new ArgumentException($"The specified property '{prop}' cannot contain whitespace");
                    }

                    op = ((JsonElement)itemsIgnoreCase["OP"]).ToString().ToUpper();
                    if (!VALID_PREDICATE_OPERATORS.Contains(op))
                    {
                        throw new ArgumentException($"The specified boolean operator '{op}' is not valid");
                    }
                    
                    var valueElement = (JsonElement)itemsIgnoreCase["VALUE"];
                    var val = valueElement.ValueKind == JsonValueKind.String ? $"'{valueElement}'" : valueElement.ToString();
                    queryText = $"{nodeName}.{prop} {op.ToUpper()} {val}";
                }
                else if (itemsIgnoreCase.Keys.Contains("BOOL"))
                {
                    op = ((JsonElement)itemsIgnoreCase["BOOL"]).ToString().ToUpper();
                    if (!VALID_BOOLEAN_OPERATORS.Contains(op))
                    {
                        throw new ArgumentException($"The specified boolean operator '{op}' is not valid");
                    }
                    queryText = op;
                }
                else if (itemsIgnoreCase.Keys.Contains("PARENS"))
                {
                    var paren = ((JsonElement)itemsIgnoreCase["PARENS"]).ToString().ToUpper();
                    if (!VALID_PARENS_OPERATORS.Contains(paren))
                    {
                        throw new ArgumentException($"The specified parens '{paren}' is not valid");
                    }

                    queryText = paren;
                }
                else
                {
                    throw new ArgumentException($"The query section specified is not valid");
                }

                whereClauseItems.Add(queryText);
            }

            var whereClause = string.Join(" ", whereClauseItems);
            return whereClause;

             /*
                {
                    "query": [
                        {
                            "prop": "department",
                            "op": "starts with",
                            "value": "sal"
                        },
                        {
                            "bool": "and"
                        },
                        {
                            "parens": "("
                        },
                        {
                            "prop": "targetRate",
                            "op": ">=",
                            "value": 4.22
                        },
                        {
                            "bool": "and"
                        },
                        {
                            "prop": "quotaCarrying",
                            "op": "=",
                            "value": true
                        },
                        {
                            "parens": ")"
                        }
                    ]
                }
            */  
        }
    }
}