
# Azure Search Emulator

Containerized Azure Cognitive Search Emulator for **development purposes**.

It consists of a simple .NET 5.0 Web API, which translates and forwards Azure Search Queries to Solr.

Has been tested with Solr 7 and 8.10 (latest).


## Installation

Currently the only way to use this is to precreate Solr cores. This took me a lot of time tu figure out, but it (sadly) is the best option. 
This is beacuse solr API doesn't support creating multiple core schemas (indexes), it uses the same configuration for all of them. 

Example from **docker-compose.yml**, which can be found in the `demo` folder
```
services:
  solr:
    image: solr:7
    container_name: SolrService
    ports:
     - "8983:8983"
    volumes:
     - data:/var/solr
    entrypoint:
     - bash
     - "-c"
     - "precreate-core catalog-internal-index; precreate-core invoicingindex; exec solr -f"
```

If for any reason you need to change te container name or port, solr URL can be configured trough `SEARCH_URL` Environment variable

#### Index files should: 
+ use the basic Azure Search index format (example in `demo/indexes`)
+ be named `index.json` and placed in separate folders
+ be mounted to `/srv/data` folder

If you also want to pre-populate the index with som data, just add a `mockData.json` file with the data
next to the `index.json` file.

Due to another limitation in Solr API, the primary key cannot be changed via API - [Issue](https://issues.apache.org/jira/browse/SOLR-7242),
and therefore has been set to `id`, but `Id` is also supported, due to camelCasing of property names. 

Whole index creation process uses *ILogger* for logging the status, which can be read off the container's log.
## Features

Currently supports:

+ Basic search functions (fulltext search, top, skip, filter, orderBy), using both: *GET* and *POST* requests 
+ Document indexing operations (add, update)

## Demo

An example can be found in the `demo` folder. It includes an example *docker-compose* and a `EshopDemo.Api` project, which 
uses the *Azure.Search.Documents* library for indexing and searching documents.

  
## Contributing

Contributions are welcome.
If there is any missing feature that you would like to be added, or you found a bug, feel free to open up a PR, or contact me.

