Dashing is a simple to use mini ORM built on top of [Dapper](https://github.com/StackExchange/dapper-dot-net). 
It aims to be a strongly typed data access layer that is built with productivity and performance in mind.

Features
-----------------

* Convention over Configuration
* Sql-like strongly typed query syntax
* Eager loading of relationships
* Change Tracking
* Crud Operations
* Schema Generation/Migrations
* Multiple Database Support (SQL Server/MySql right now)
* In-memory engine for testing

Examples
------------------

Update changed properties only

    var post = await session.GetAsync<Post>(123);
    post.Title = "New Title";
    await session.SaveAsync(post); // update [Posts] set [Title] = @P1 where [PostId] = @P2

Eager fetching of related entities

    var posts = await session.Query<Post>().
                .FetchMany(p => p.Comments).ThenFetch(c => c.Author)
                .SingleOrDefaultAsync(p => p.PostId == 123);

Bulk update entity

    await session.UpdateAsync<Post>(p => p.IsArchived = true, p => p.Author.UserId == 3);
    // update [Posts] set [IsArchived] = @P1 where [AuthorId] = @P2

Migrate database to match latest code

    ./dash migrate -a "<path to assembly>" -t "<configuration type full name>" -c "<connection string>" 

Who uses Dashing?
---------------------------------

Dashing has been developed over the last 4 years at Polylytics and is in use at nearly all of our clients. It's used to execute millions of queries every week.

Feature requests (and voting for them) available at [Feathub](http://feathub.com/Polylytics/dashing)

[![Join the chat at https://gitter.im/Polylytics/dashing](https://badges.gitter.im/Polylytics/dashing.svg)](https://gitter.im/Polylytics/dashing?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) 
