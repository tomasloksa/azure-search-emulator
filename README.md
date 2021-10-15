
# Azure Search Emulator

Containerized Azure Cognitive Search Emulator for **development purposes**.
If your project is using Azure Cognitive Search, which cannot be containerized, this tool is a good replacement for local development.

It is insecure and should never be used in production! It even has a localhost signed https certificate bundled with it. 

It consists of a simple .NET 5.0 Web API, which translates and forwards Azure Search Queries to Solr.

Has been tested with Solr 7 and 8.10 (latest).


## Installation

The best way to use this tool is do just download it from [Docker Hub](https://hub.docker.com/repository/docker/tomee/azure-search-emulator) and add it to your docker compose.

Example from `docker-compose.yml`:
```
  searchqueryservice:
    image: tomee/azure-search-emulator
    ports:
      - "8000:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443
      - ASPNETCORE_Kestrel__Certificates__Default__Password=password
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/srv/cert/testcert.pfx
    volumes:
      - "./indexes:/srv/data"
    restart: on-failure
    depends_on:
      - solr
```

Currently the only way to use this tool, is to precreate Solr cores. This took me a lot of time to figure out, but it (sadly) is the best option. 
This is beacuse solr API doesn't support creating multiple configurations for core schemas (indexes) and they have to be created manually. 

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

By default, the service expects Solr container to listen on http://solr:8983.
If for any reason you need to change te container name or port, Solr URL can be configured trough `SEARCH_URL` Environment variable

The localhost https certificate bundled with the service should be valid until *10/2031*. If you want to use another one, it can also be set in docker-compose.


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

+ Basic search functions (fulltext search, top, skip, filter, ordering), using both: *GET* and *POST* requests 
+ Document indexing operations (add, update)

## Demo

An example can be found in the `demo` folder. It includes an example *docker-compose* and a `EshopDemo.Api` project, which 
uses the *Azure.Search.Documents* library for indexing and searching documents.

  
## Contributing

Contributions are welcome.
If there is any missing feature that you would like to be added, or maybe you found a bug, feel free to open up a PR, or contact me.
