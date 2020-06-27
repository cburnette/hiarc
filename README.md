# Hiarc

> :warning: Hiarc is currently pre-release software. If you are interested in trying it out please email <try@hiarcdb.com>
> and we'll be happy to help you get it up and running.

Hiarc is an API that orchestrates all of your enterprise content requirements across cloud environments.  It leverages a powerful graph database to scale up to billions of files and hundreds of millions of users with <100ms response times. By sitting between your application code and your cloud infrastructure, Hiarc acts as an organizational and control layer, providing key pieces of functionality required in complex Enterprise workflows

*With Hiarc, you bring your own cloud resources.*

Whether it's AWS, Azure, or Google, Hiarc supports it (with more platforms to come).  By leveraging the cloud infrastructure you already use, Hiarc leaves critical data under your control.  Every file and every event resides in your infrastructure, with Hiarc providing the connective tissue around permissions, organization, access, and scale.

### Supported Cloud Infrastructure
* Storage Services
  * AWS
  * Azure
  * Google Cloud
  * IPFS
* Event Services
  * Amazon Kinesis
  * Azure Service Bus
  * Google PubSub
  * Webhooks

### Why Use Hiarc?
* **Elevate Your Cloud** - Hiarc is designed as a modern API layer that sits on top of your existing cloud infrastructure.  Amazon, Microsoft, and Google have spend billions perfecting simple, robust file storage systems. However, these systems lack granular control, hierarchical permissions, complex metadata, per-file retention policies and other features needed to facilitate Enterprise workflows.  Hiarc gives you those features while letting you own the underlying cloud infrastructure. You don't need to expose your critical data to another company.

* **Replace Legacy ECM** - If you're like many Enterprises, you've relied on legacy ECM solutions like Documentum, FileNet, and OpenText to support your critical workflows.  These solutions are expensive, complicated, proprietary, and not cloud-friendly.  Hiarc is an open source, community built solution that can help you leave behind these legacy systems with their complexity and high costs.

* **Avoid Vendor Lock-in** - With Hiarc you can start out with one cloud provider and easily migrate to another.  The ability to seamlessly switch cloud storage providers gives you incredible leverage.  Simply add a new storage service and migrate your files or just put new versions of files there as they are created.  

* **Go Multi-Cloud** - For mission critical workflows it can be imperative to replicate data across multiple cloud data centers.  Hiarc supports adding as many storage services across AWS, Azure, and Google Cloud as you need.  Because the definition of files, their metadata and versions is controlled by Hiarc and not the cloud vendors, you can leverage as many cloud data centers as you require.

* **Massive Scalability** - Hiarc leverages a powerful, industry-leading graph database that supports billions of objects and is able to perform queries in milliseconds that would take SQL and NoSQL databases minutes because of their recursive nature.  Coupled with the incredible scale of cloud content services, Hiarc can power your largest workflows.


### Key Concepts
* **A Key for Everything** - Every object in Hiarc is uniquely identified by an application specified key.  Similar to key-value stores like Redis or Memcached, Hiarc supports efficient object lookup by letting you construct keys using data you already have. For example, you could create a collection for collaborating between a financial advisor and a client.  The key could be ```"/portal/{advisorId}/{clientId}"```.  When you need the list of files in the collection you simply ask Hiarc using the key you know how to construct.  No need to store IDs in yet another database.

* **Metadata Everywhere** - Every object in Hiarc supports setting metadata values and querying based on those values.  Metadata is such an important part of complex workflows that it should be ubiquitous.

* **Direct Upload/Download** - With Hiarc you have two ways you can upload and download files.  Some scenarios require that all files should flow through your Hiarc servers, and that is supported.  For other scenarios it is more efficient to allow clients to upload and download directly to your cloud storage buckets using time limited, pre-signed URLs.  Hiarc also fully supports this model and based on configuration simply does it automatically.

* **Multi-Cloud by Default** - Hiarc seamlessly supports AWS, Azure, and Google cloud storage.  You can configure as many storage services as you need, and you can easily migrate and version files across them.  This helps prevent vendor lock-in, and it gives you the flexibility needed for complex workflows.  For example, you can easily move files that haven't been accessed in 90 days to your cheaper storage buckets without your application code needing to care.

* **Waterfall and ACL** - For many scenarios it is convenient to structure your content so that permissions flow down from parent collections to child collections.  If a user has read access to a parent collection any files below that should be accessible.  This is how folders work on your computer's filesystem. For other use cases this paradigm falls down.  Sometimes you need exact control as to who can access what.  Hiarc supports both modalities, allowing you to construct the optimal solution.

* **Events are First Class** - Increasingly, it's critical to understand exactly what is happening in every system you control as it's happening.  This facilitates the composition of disparate systems into a more cohesive solution. Hiarc fully supports this model by providing built-in support for multiple major event services.  Events like 'File Uploaded', 'Retention Policy Applied', and 'User Created' are instantly sent to as many supported services as you configure. 

* **Retention and Disposition** - Document retention is a feature required in many Enterprise workflows.  Hiarc supports per-file retention policies that can be extended but never shortened. Once a file has a policy applied it can never be removed, even by the admin.  Even if a file moves across cloud providers the policy remains.  Additionally, Hiarc support file disposition when a policy expires, enforcing the deletion of all versions of the file across cloud storage containers.

### Key Objects
* **Users** - Your application almost certainly already has the concept of users, but Hiarc extends that concept into your cloud storage bucket.  Hiarc users typically map one-to-one with your application users, and they provide the context necessary to determine how a user can interact with files.

* **Groups** - Hiarc groups are a collection of users and they can be given different levels of permission to access content.  While Hiarc supports single-user collaboration, groups are the primary mechanism for determining who can access what.

* **Files** - The primary piece of data managed by Hiarc is a file.  Files are immutable but they have new versions over time as the data changes. Files are stored securely in the cloud storage bucket of your choosing.  You can migrate files between storage buckets, and you can even have different versions of a file located in different buckets.

* **Collections** - Files can be assembled into collections, like how you would use a folder on a file system.  The key difference is that Hiarc collections can have more than one parent and a file can be placed in more than one collection.  You can create hierarchies of collections by organizing them into parent and child relationships.  Permissions can flow down in waterfall style, or you can specify distinct ACLs depending on your exact requirements.

* **Retention Policies** - A key piece of highly regulated workflows, retention policies can be applied to files, ensuring that the file cannot be deleted until a certain period of time has gone by.  Retention policies, once applied, can never be removed, and the retention period can be extended but never shortened.  If a file has multiple policies applied, Hiarc will calculate the longest retention period and enforce it.

* **Classifications** - To make things even easier, Hiarc provides classifications which can be applied to files or collections.  A classification can specify retention policies that should be applied to a specific file, or to any file that ever becomes a member of a designated collection.  Even if the file is later removed from the collection the classification remains.  Classifications can be explicitly removed, but any retention policies applied will remain.
