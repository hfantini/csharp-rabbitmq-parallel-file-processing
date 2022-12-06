# PARALLEL FILE PROCESSING
### STUDY (C# + RabbitMQ)

Creation Date: AUGUST/2021

<br/>

The objective of this application is to do a study based on parallel file processing in a horizontal way. It is achieved through the use of stream processing software, more specifically, RabbitMQ. Using the combination of 4 binaries that read and write in streams, we can find all occurrences of 'the' strings across all .txt files found in a file system.

<br/>

To make things easy, docker-compose.yml in the root allows the creation of a local RabbitMQ instance through the docker engine. Everything is set to start working as soon as the container is deployed. Use build.bat to compile the binaries and run.bat to start with a predefined search configuration.