# Compulsory for DLS

For running and building the assignment please follow the below steps:

docker-compose build
docker-compose up

Enron emails must be in ./maildir
Cleaned emails will be placed in ./cleaned_mails

For executing the c4 model diagram, please follow the below steps:

c4-pre model is located in ./c4-pre folder. 
c4-post model is located in ./c4-post folder.

Both can be executed by running docker-compose up --build from the respective folders.

WEB-UI: http://localhost:3001

RabbitMQ: http://localhost:15672

Zipkin (Tracing): http://localhost:9411

Seq (Logging) : http://localhost:5341

Prometheus:  http://localhost:9090

Grafana:  http://localhost:3000

All monitoring and logging services are integrated with the application and default usernames 
and passwords have been used (guest/guest, admin/admin etc.)


