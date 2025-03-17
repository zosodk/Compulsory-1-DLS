workspace {
    model {
        user = person "User" "Searches and downloads emails from the Enron dataset."

        system = softwareSystem "Enron Search System" "A system that allows users to search and retrieve emails." {
            
            // C2 - Container Diagram
            webUI = container "Web UI" "Angular" "User interface for searching and downloading emails."
            searchAPI = container "Search API" "C#" "Provides a REST API for searching indexed emails." {
                
                // C3 - Component Diagram (SearchAPI)
                searchController = component "SearchController" "ASP.NET Core" "Handles search requests and queries the database."
                searchService = component "SearchService" "C#" "Business logic for searching emails."
                searchAlgorithm = component "SearchAlgorithm" "C#" "Implements full-text search logic."
                cacheLayer = component "CacheLayer" "Redis" "Stores frequent search queries."
                emailRepository = component "EmailRepository" "C#" "Manages database interactions."
                dbContext = component "PostgreSQL DbContext" "Entity Framework Core" "Handles database operations."
                logging = component "Serilog" "Logging Library" "Logs events to Seq for monitoring."

                // C3 - Relationships
                searchController -> searchService "Calls"
                searchService -> searchAlgorithm "Uses"
                searchService -> cacheLayer "Checks for cached queries"
                searchService -> emailRepository "Queries email data"
                emailRepository -> dbContext "Uses"
                searchController -> logging "Logs API Calls"
            }

            indexer = container "Indexer Service" "C#" "Indexes emails and stores them in PostgreSQL." {
                
                // C3 - Component Diagram (IndexerService)
                mailIndexer = component "MailIndexer" "C#" "Processes and indexes emails."
                dbContext = component "PostgreSQL DbContext" "Entity Framework Core" "Handles database operations."
                logging = component "Serilog" "Logging Library" "Logs events to Seq for monitoring."

                // C3 - Relationships
                mailIndexer -> dbContext "Uses"
                mailIndexer -> logging "Logs events"
            }

            cleaner = container "Cleaner Service" "C#" "Processes and cleans raw emails before indexing." {
                
                // C3 - Component Diagram (CleanerService)
                mailCleaner = component "MailCleaner" "C#" "Cleans raw emails."
                logging = component "Serilog" "Logging Library" "Logs events to Seq for monitoring."

                // C3 - Relationships
                mailCleaner -> logging "Logs events"
            }

            db = container "PostgreSQL Database" "PostgreSQL" "Stores indexed email data."
            mq = container "RabbitMQ" "Message Queue" "Handles communication between services."
            monitoring = container "Monitoring (Seq + Zipkin)" "Monitoring" "Logs and traces system behavior."

            // Relationships
            user -> webUI "Uses"
            webUI -> searchAPI "Sends search queries"
            searchAPI -> db "Fetches search results"
            cleaner -> mq "Sends cleaned emails"
            indexer -> mq "Receives cleaned emails"
            indexer -> db "Indexes cleaned emails"
            searchAPI -> monitoring "Sends logs"
            indexer -> monitoring "Sends logs"
            cleaner -> monitoring "Sends logs"
        }
    }

    views {
        // C1 - System Context Diagram
        systemContext system {
            include *
            autolayout lr
        }
    
        // C2 - Container Diagram
        container system {
            include *
            autolayout lr
        }
    
        // C3 - Component Diagram (SearchAPI)
        component searchAPI {
            include searchController
            include searchService
            include searchAlgorithm
            include cacheLayer
            include emailRepository
            include dbContext
            include logging
            autolayout lr
        }

        // C3 - Component Diagram (IndexerService)
        component indexer {
            include mailIndexer
            include dbContext
            include logging
            autolayout lr
        }

        // C3 - Component Diagram (CleanerService)
        component cleaner {
            include mailCleaner
            include logging
            autolayout lr
        }
    
        theme default
    }
}
