CREATE (u:User { key: 'admin' })
-[:BELONGS_TO { createdAt: datetime(), identity: true }]
->(g:Group { key:'identity:admin', identity: true })
-[:CREATED_BY]->(u)
RETURN u