# RaceDay — Part 3

## Project overview

Part 3 adds an ASP.NET Core MVC front end to the existing RaceDay .NET 8 Web API. The MVC application sends all data requests to the Part 2 API; it does not access SQL Server or Entity Framework directly. The API remains responsible for authentication, authorisation, validation, persistence and Azure Blob Storage operations.

The website provides separate Organiser and Participant navigation and pages. Organisers can manage their events, categories, enrolments and results. Participants can browse upcoming events, inspect event details and categories, enrol, track their enrolments, view published results and update their profiles.

## Solution structure

- `RaceDay .Api/RaceDay .Api/` — the Part 2 API, extended with Azure Blob Storage image operations and image reference fields.
- `RaceDay .Api/RaceDay.Api.Tests/` — the existing Part 2 API integration tests.
- `RaceDay.Mvc/` — the Part 3 ASP.NET Core MVC application, Razor views, view models and API client.
- `RaceDay.sln` — opens the API, test project and MVC project together.
- `Dockerfile` — builds and publishes the MVC application as a container image.
- `.github/workflows/dotnet.yml` — restores and builds the complete solution and runs the API tests.

## Prerequisites

Install Visual Studio 2022 with the ASP.NET and web development workload, the .NET 8 SDK, SQL Server or SQL Server Express, and Docker Desktop for the container step. An Azure Storage account is required for real event-banner and profile-picture uploads.

## 1. Configure SQL Server

Open `RaceDay .Api/RaceDay .Api/appsettings.json` and update `ConnectionStrings:RaceDayConnection` to point to the SQL Server instance available on your computer. The value included in this package is retained from the Part 2 project and may need to be changed on another computer.

In Visual Studio, open `RaceDay.sln`. Set `RaceDay .Api` as the startup project, open **Tools → NuGet Package Manager → Package Manager Console**, choose `RaceDay .Api` as the Default project, then run:

```powershell
Update-Database
```

This applies the existing Part 2 migrations and the Part 3 `AddAzureBlobStorageImageReferences` migration, which adds the blob-name columns to `Events` and `Users`.

## 2. Configure private application settings

Do not commit a live Azure connection string or the Organiser invitation code to the repository. The API project already has a User Secrets ID. From the repository root, set the values using PowerShell:

```powershell
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "YOUR_AZURE_STORAGE_CONNECTION_STRING" --project "RaceDay .Api/RaceDay .Api/RaceDay .Api.csproj"
dotnet user-secrets set "Registration:OrganiserInviteCode" "YOUR_ORGANISER_INVITATION_CODE" --project "RaceDay .Api/RaceDay .Api/RaceDay .Api.csproj"
```

The API creates private containers named `event-banners` and `profile-pictures` by default. Container names can be changed through the API configuration. Uploads accept JPEG, PNG and WebP image files up to 5 MB. The API returns time-limited read URLs for displaying stored images.

Participant registration does not need an invitation code. Organiser registration requires the configured invitation code.

## 3. Restore, build and test

Run these commands from the repository root:

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release --verbosity normal
```

The test project uses an in-memory database. It does not require the local SQL Server database for its test data.

## 4. Run locally in Visual Studio

1. Open `RaceDay.sln`.
2. Set the `RaceDay .Api` and `RaceDay.Mvc` projects as multiple startup projects, with both configured to Start.
3. Confirm that the API is listening on `http://localhost:5221/` (or change `ApiSettings:BaseUrl` in `RaceDay.Mvc/appsettings.json` to the API's actual HTTP address).
4. Run the solution and open the MVC URL shown by Visual Studio. The MVC project's included launch profile uses `http://localhost:5240` and `https://localhost:7240`.
5. Register a Participant account to test participant screens. Use the configured invitation code when registering an Organiser account.

## 5. Run the MVC application in Docker

The MVC container calls the API over HTTP. Start the API on a host interface that the container can reach. From the repository root, use:

```powershell
dotnet run --project "RaceDay .Api/RaceDay .Api/RaceDay .Api.csproj" -- --urls "http://0.0.0.0:5221"
```

Build the MVC image from the repository root:

```powershell
docker build --tag raceday-mvc:part3 .
```

Then run the MVC container with this single Docker run command (Docker Desktop provides `host.docker.internal`):

```powershell
docker run --name raceday-mvc --publish 8080:8080 --env ASPNETCORE_URLS=http://+:8080 --env ApiSettings__BaseUrl=http://host.docker.internal:5221/ raceday-mvc:part3
```

Open `http://localhost:8080`. The API and SQL Server must remain available on the host while the MVC container is running. On a native Linux Docker Engine that does not provide `host.docker.internal` automatically, add `--add-host=host.docker.internal:host-gateway` to the same `docker run` command.

## 6. GitHub Actions

The workflow in `.github/workflows/dotnet.yml` runs on pushes, pull requests and manual dispatch. It restores and builds `RaceDay.sln`, which includes both the API and MVC projects, then runs the API integration tests. After pushing the Part 3 changes, check the Actions tab and keep a genuine screenshot of a successful Part 3 workflow run for the submission README/evidence requirement.
<img width="1439" height="157" alt="image" src="https://github.com/user-attachments/assets/eaa7b5ca-9ec7-474e-93b2-92c61e839c73" />


## 7. Demonstration video and submission checklist

Replace the placeholder below with the URL of your own unlisted YouTube demonstration after recording the running application, MVC structure, Azure Blob Storage flow and Docker run.

**Unlisted YouTube demonstration:** 
https://youtu.be/7AnmeJeoliE

Before submitting, make sure that you have:

- applied the new database migration to the database used for the demonstration;
- configured the Azure Storage connection string outside source control and successfully uploaded/displayed an event banner and a participant profile picture;
- demonstrated login, logout, role-specific navigation, event/category management, participant enrolment, enrolment tracking and published results;
- built and run the MVC Docker container while the API is reachable;
- made at least 20 meaningful Part 3 commits in the existing GitHub repository (Part 2 commits do not replace this Part 3 requirement);
- pushed the Part 3 changes to the existing GitHub repository and captured the successful Part 3 GitHub Actions run;
- replaced the YouTube URL placeholder and included the genuine CI screenshot required by the question paper.

Follow the course's required disclosure process for any tools used during development.
