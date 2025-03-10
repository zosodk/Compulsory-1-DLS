workspace {
    model {
        user = person "User" "Searches and downloads emails from the Enron dataset."

        system = softwareSystem "Enron Search System" "A system that allows users to search and retrieve emails."

        user -> system "Uses"

        //  C2 - Container Diagram
        webUI = container system "Web UI" "Angular" "User interface for searching and downloading emails."
        searchAPI = container system "Search API" "C#" "Provides a REST API for searching indexed emails."
        indexer = container system "Indexer Service" "C#" "Indexes emails and stores them in PostgreSQL."
        cleaner = container system "Cleaner Service" "C#" "Processes and cleans raw emails before indexing."
        db = container system "PostgreSQL Database" "PostgreSQL" "Stores indexed email data."
        mq = container system "RabbitMQ" "Message Queue" "Handles communication between services."
        monitoring = container system "Monitoring (Seq + Zipkin)" "Monitoring" "Logs and traces system behavior."

        //  Relationships
        user -> webUI "Uses"
        webUI -> searchAPI "Sends search queries"
        searchAPI -> db "Fetches search results"
        cleaner -> mq "Sends cleaned emails"
        indexer -> mq "Receives cleaned emails"
        indexer -> db "Indexes cleaned emails"
        searchAPI -> monitoring "Sends logs"
        indexer -> monitoring "Sends logs"
        cleaner -> monitoring "Sends logs"

        // C3 - Component Diagram (SearchAPI)
        controller = component searchAPI "SearchController" "ASP.NET Core" "Handles search requests and queries the database."
        searchService = component searchAPI "SearchService" "Business logic for searching emails."
        repository = component searchAPI "EmailRepository" "Manages database interactions."
        dbContext = component searchAPI "PostgreSQL DbContext" "Entity Framework Core" "Handles database operations."
        logging = component searchAPI "Serilog" "Logging Library" "Logs events to Seq for monitoring."

        // C3 - Relationships
        searchAPI -> controller "Handles API Requests"
        controller -> searchService "Calls"
        searchService -> repository "Queries email data"
        repository -> dbContext "Uses"
        controller -> logging "Logs API Calls"

        //  C4 - Code-Level Breakdown of `SearchService`
        searchAlgorithm = component searchService "SearchAlgorithm" "Implements full-text search logic."
        cache = component searchService "CacheLayer" "Stores frequent search queries."

        // C4 - Relationships
        searchService -> searchAlgorithm "Uses"
        searchService -> cache "Checks for cached queries"
    }

   views {
       //  C1 - System Context Diagram
       systemContext system "SystemContext" {
           include *
           autolayout lr
       }
   
       //  C2 - Container Diagram
       container system "Container" {
           include *
           autolayout lr
       }
   
      // C3 - Component Diagram (SearchAPI)
              component searchAPI "SearchAPIComponent" {
                  include controller
                  include searchService
                  include repository
                  include dbContext
                  include logging
                  autolayout lr
              }
      
              // C4 - Code-Level Diagram (SearchService)
              component searchService "SearchServiceCodeLevel" {
                  include searchAlgorithm
                  include cache
                  autolayout lr
              }
      
              theme default
          }


}

//commands: docker pull structurizr/lite
// docker run -it --rm -p 8080:8080 -v structurizr.dsl:/usr/local/structurizr structurizr/lite

