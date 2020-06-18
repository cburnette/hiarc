:begin
CREATE CONSTRAINT ON (node:User) ASSERT (node.key) IS UNIQUE;
CREATE CONSTRAINT ON (node:Group) ASSERT (node.key) IS UNIQUE;
CREATE CONSTRAINT ON (node:Collection) ASSERT (node.key) IS UNIQUE;
CREATE CONSTRAINT ON (node:File) ASSERT (node.key) IS UNIQUE;
CREATE CONSTRAINT ON (node:RetentionPolicy) ASSERT (node.key) IS UNIQUE;
CREATE CONSTRAINT ON (node:Classification) ASSERT (node.key) IS UNIQUE;
CALL db.index.fulltext.createNodeIndex('classificationNameDescription',['Classification'],['name','description']);
CALL db.index.fulltext.createNodeIndex('collectionNameDescription',['Collection'],['name','description']);
CALL db.index.fulltext.createNodeIndex('fileNameDescription',['File'],['name','description']);
CALL db.index.fulltext.createNodeIndex('groupNameDescription',['Group'],['name','description']);
CALL db.index.fulltext.createNodeIndex('retentionPolicyNameDescription',['RetentionPolicy'],['name','description']);
CALL db.index.fulltext.createNodeIndex('userNameDescription',['User'],['name','description']);
:commit
CALL db.awaitIndexes(300);
