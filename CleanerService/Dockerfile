# Use the .NET 9.0 SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy the solution file and restore dependencies
COPY *.sln ./
RUN ls -al /app # Add this line to list files and directories

COPY ./Shared/ ./Shared/ # Copy the entire directory
COPY ./CleanerService/CleanerService.csproj ./CleanerService/
RUN dotnet restore

# Copy the rest of the application files
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use the .NET 9.0 runtime image for the final image
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out ./

# Set the entry point for the application
ENTRYPOINT ["dotnet", "CleanerService.dll"]