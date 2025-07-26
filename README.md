# **SS12000 C\# Client Library**

This is a C\# client library designed to simplify interaction with the SS12000 API, a standard for information exchange between school administration processes based on OpenAPI 3.x. The library utilizes HttpClient for efficient HTTP communication and System.Text.Json for JSON serialization/deserialization, providing a structured and asynchronous approach to interact with **all** the API's defined endpoints.

You can download your own personal copy of the SS12000 standard for free from here: [sis.se](https://www.sis.se/standarder/kpenstandard/forkopta-standarder/informationshantering-inom-utbildningssektorn/).

### **Important**

The SS12000 does not require the server to support all of the endpoints. You need to actually look at the server documentation to see which endpoints that are actually available with each service. Adding some sort of discovery service is beyond the scope of this small library in my humble opinion.

All dates are in the RFC 3339 format, we're not cavemen here. 

## **Table of Contents**

- [**SS12000 C# Client Library**](#ss12000-c-client-library)
    - [**Important**](#important)
  - [**Table of Contents**](#table-of-contents)
  - [**Installation**](#installation)
  - [**Usage**](#usage)
    - [**Initializing the Client**](#initializing-the-client)
    - [**Fetching Organizations**](#fetching-organizations)
    - [**Fetching Persons**](#fetching-persons)
    - [**Fetch ...**](#fetch-)
    - [**Webhooks (Subscriptions)**](#webhooks-subscriptions)
  - [**API Reference**](#api-reference)
  - [**Webhook Receiver (ASP.NET Core Example)**](#webhook-receiver-aspnet-core-example)
  - [**Contribute**](#contribute)
  - [**License**](#license)

## **Installation**

1. Add the nuget package:
```
dotnet add package SS12000.Client --version 0.1.0
```
or start from scratch:
1. **Create a .NET Project:** If you don't have one, create a new .NET project (e.g., Console App, Web API).  
```
dotnet new console -n MySS12000App  
cd MySS12000App
```

2. **Add the Client File:** Place the SS12000Client.cs file directly into your project directory (e.g., MySS12000App/SS12000Client.cs).  
3. **Add Necessary Usings:** Ensure your project file (.csproj) includes the necessary framework references. The client uses System.Web for HttpUtility, which might require adding a package reference if you're not targeting a full .NET Framework. For .NET Core / .NET 5+, System.Web.HttpUtility is available via the System.Web.HttpUtility NuGet package.  
```
   <!-- In your .csproj file -->  
   <ItemGroup\>  
       <PackageReference Include="System.Text.Json" Version="6.0.0" /> <!-- Or newer -->  
       <PackageReference Include="System.Web.HttpUtility" Version="1.0.0" /> <!-- Or newer, if not implicitly available -->  
   </ItemGroup\>
```

   The package has only two real dependencies: Microsoft.AspNetCore.WebUtilities and System.Text.Json. Add them manually if they don't resolve automatically. 

## **Usage**

### **Initializing the Client**

To start using the client, create an instance of SS12000Client. It's recommended to use using statement for proper disposal of the underlying HttpClient.  

```
using SS12000.Client;  
using System;  
using System.Collections.Generic;  
using System.Text.Json;  
using System.Threading.Tasks;

public class Program  
{  
    public static async Task Main(string[] args)  
    {  
        const string baseUrl = "https://some.server.se/v2.0"; // Replace with your test server URL  
        const string authToken = "YOUR_JWT_TOKEN_HERE";       // Replace with your actual JWT token

        // It's recommended to use HttpClientFactory in ASP.NET Core for managing HttpClient instances.  
        // For a simple console app, instantiating directly is fine, but ensure proper disposal.  
        using (var client = new SS12000Client(baseUrl, authToken))  
        {  
            await GetOrganizationData(client);  
            await GetPersonData(client);  
            await ManageSubscriptions(client);  
        }  
    }

    // ... (methods below)  
}
```
### **Fetching Organizations**

You can retrieve a list of organizations or a specific organization by its ID. Parameters are passed as a Dictionary\<string, object\>.  
```
public static async Task GetOrganizationData(SS12000Client client)  
{  
    try  
    {  
        Console.WriteLine("\nFetching organizations...");  
        var organizations = await client.GetOrganisationsAsync(new Dictionary<string, object> { { "limit", 2 } });  
        Console.WriteLine("Fetched organizations:\n" + JsonSerializer.Serialize(organizations, new JsonSerializerOptions { WriteIndented = true }));

        if (organizations.TryGetProperty("data", out var orgsArray) && orgsArray.ValueKind == JsonValueKind.Array && orgsArray.GetArrayLength() > 0)  
        {  
            var firstOrgId = orgsArray[0].GetProperty("id").GetString();  
            Console.WriteLine($"\nFetching organization with ID: {firstOrgId}...");  
            var orgById = await client.GetOrganisationByIdAsync(firstOrgId, true); // expandReferenceNames = true  
            Console.WriteLine("Fetched organization by ID:\n" + JsonSerializer.Serialize(orgById, new JsonSerializerOptions { WriteIndented = true }));  
        }  
    }  
    catch (HttpRequestException e)  
    {  
        Console.WriteLine($"An HTTP request error occurred fetching organization data: {e.Message}");  
    }  
    catch (Exception e)  
    {  
        Console.WriteLine($"An unexpected error occurred fetching organization data: {e.Message}");  
    }  
}
```
### **Fetching Persons**

Similarly, you can fetch persons and expand related data such as duties.  

```
public static async Task GetPersonData(SS12000Client client)  
{  
    try  
    {  
        Console.WriteLine("\nFetching persons...");  
        var persons = await client.GetPersonsAsync(new Dictionary<string, object> { { "limit", 2 }, { "expand", new List<string> { "duties" } } });  
        Console.WriteLine("Fetched persons:\n" + JsonSerializer.Serialize(persons, new JsonSerializerOptions { WriteIndented = true }));

        if (persons.TryGetProperty("data", out var personsArray) && personsArray.ValueKind == JsonValueKind.Array && personsArray.GetArrayLength() > 0)  
        {  
            var firstPersonId = personsArray[0].GetProperty("id").GetString();  
            Console.WriteLine($"\nFetching person with ID: {firstPersonId}...");  
            var personById = await client.GetPersonByIdAsync(firstPersonId, new List<string> { "duties", "responsibleFor" }, true);  
            Console.WriteLine("Fetched person by ID:\n" + JsonSerializer.Serialize(personById, new JsonSerializerOptions { WriteIndented = true }));  
        }  
    }  
    catch (HttpRequestException e)  
    {  
        Console.WriteLine($"An HTTP request error occurred fetching person data: {e.Message}");  
    }  
    catch (Exception e)  
    {  
        Console.WriteLine($"An unexpected error occurred fetching person data: {e.Message}");  
    }  
}
```

### **Fetch ...**

Check the API reference below to see all available nodes. 

### **Webhooks (Subscriptions)**

The client provides asynchronous methods to manage subscriptions (webhooks).  

```
public static async Task ManageSubscriptions(SS12000Client client)  
{  
    try  
    {  
        Console.WriteLine("\nFetching subscriptions...");  
        var subscriptions = await client.GetSubscriptionsAsync();  
        Console.WriteLine("Fetched subscriptions:\n" + JsonSerializer.Serialize(subscriptions, new JsonSerializerOptions { WriteIndented = true }));

        // Example: Create a subscription (requires a publicly accessible webhook URL)  
        // Console.WriteLine("\nCreating a subscription...");  
        // var newSubscription = await client.CreateSubscriptionAsync(new  
        // {  
        //     name = "My CSharp Test Subscription",  
        //     target = "http://your-public-webhook-url.com/ss12000-webhook", // Replace with your public URL  
        //     resourceTypes = new\[\] { new { resource = "Person" }, new { resource = "Activity" } }  
        // });  
        // Console.WriteLine("Created subscription:\n" \+ JsonSerializer.Serialize(newSubscription, new JsonSerializerOptions { WriteIndented = true }));

        // Example: Delete a subscription  
        // if (subscriptions.TryGetProperty("data", out var subsArray) && subsArray.ValueKind == JsonValueKind.Array && subsArray.GetArrayLength() > 0)  
        // {  
        //     var subToDeleteId = subsArray[0].GetProperty("id").GetString();  
        //     Console.WriteLine($"\nDeleting subscription with ID: {subToDeleteId}...");  
        //     await client.DeleteSubscriptionAsync(subToDeleteId);  
        //     Console.WriteLine("Subscription deleted successfully.");  
        // }  
    }  
    catch (HttpRequestException e)  
    {  
        Console.WriteLine($"An HTTP request error occurred managing subscriptions: {e.Message}");  
    }  
    catch (Exception e)  
    {  
        Console.WriteLine($"An unexpected error occurred managing subscriptions: {e.Message}");  
    }  
}
```

## **API Reference**

The SS12000Client class is designed to expose asynchronous methods for all SS12000 API endpoints. All methods return Task<JsonElement> for data retrieval or Task for operations without content (e.g., DELETE). Parameters are typically passed as Dictionary<string, object> for query parameters or object for JSON bodies.  
Here is a list of the primary resource paths defined in the OpenAPI specification, along with their corresponding client methods:

* /organisations  
  * GetOrganisationsAsync(Dictionary\<string, object\> queryParams)  
  * LookupOrganisationsAsync(object body, bool expandReferenceNames)  
  * GetOrganisationByIdAsync(string orgId, bool expandReferenceNames)  
* /persons  
  * GetPersonsAsync(Dictionary\<string, object\> queryParams)  
  * LookupPersonsAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetPersonByIdAsync(string personId, List\<string\> expand, bool expandReferenceNames)  
* /placements  
  * GetPlacementsAsync(Dictionary\<string, object\> queryParams)  
  * LookupPlacementsAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetPlacementByIdAsync(string placementId, List\<string\> expand, bool expandReferenceNames)  
* /duties  
  * GetDutiesAsync(Dictionary\<string, object\> queryParams)  
  * LookupDutiesAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetDutyByIdAsync(string dutyId, List\<string\> expand, bool expandReferenceNames)  
* /groups  
  * GetGroupsAsync(Dictionary\<string, object\> queryParams)  
  * LookupGroupsAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetGroupByIdAsync(string groupId, List\<string\> expand, bool expandReferenceNames)  
* /programmes  
  * GetProgrammesAsync(Dictionary\<string, object\> queryParams)  
  * LookupProgrammesAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetProgrammeByIdAsync(string programmeId, List\<string\> expand, bool expandReferenceNames)  
* /studyplans  
  * GetStudyPlansAsync(Dictionary\<string, object\> queryParams)  
  * LookupStudyPlansAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetStudyPlanByIdAsync(string studyPlanId, List\<string\> expand, bool expandReferenceNames)  
* /syllabuses  
  * GetSyllabusesAsync(Dictionary\<string, object\> queryParams)  
  * LookupSyllabusesAsync(object body, bool expandReferenceNames)  
  * GetSyllabusByIdAsync(string syllabusId, bool expandReferenceNames)  
* /schoolUnitOfferings  
  * GetSchoolUnitOfferingsAsync(Dictionary\<string, object\> queryParams)  
  * LookupSchoolUnitOfferingsAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetSchoolUnitOfferingByIdAsync(string offeringId, List\<string\> expand, bool expandReferenceNames)  
* /activities  
  * GetActivitiesAsync(Dictionary\<string, object\> queryParams)  
  * LookupActivitiesAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetActivityByIdAsync(string activityId, List\<string\> expand, bool expandReferenceNames)  
* /calendarEvents  
  * GetCalendarEventsAsync(Dictionary\<string, object\> queryParams)  
  * LookupCalendarEventsAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetCalendarEventByIdAsync(string eventId, List\<string\> expand, bool expandReferenceNames)  
* /attendances  
  * GetAttendancesAsync(Dictionary\<string, object\> queryParams)  
  * LookupAttendancesAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetAttendanceByIdAsync(string attendanceId, List\<string\> expand, bool expandReferenceNames)  
  * DeleteAttendanceAsync(string attendanceId)  
* /attendanceEvents  
  * GetAttendanceEventsAsync(Dictionary\<string, object\> queryParams)  
  * LookupAttendanceEventsAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetAttendanceEventByIdAsync(string eventId, List\<string\> expand, bool expandReferenceNames)  
* /attendanceSchedules  
  * GetAttendanceSchedulesAsync(Dictionary\<string, object\> queryParams)  
  * LookupAttendanceSchedulesAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetAttendanceScheduleByIdAsync(string scheduleId, List\<string\> expand, bool expandReferenceNames)  
* /grades  
  * GetGradesAsync(Dictionary\<string, object\> queryParams)  
  * LookupGradesAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetGradeByIdAsync(string gradeId, List\<string\> expand, bool expandReferenceNames)  
* /aggregatedAttendance  
  * GetAggregatedAttendancesAsync(Dictionary\<string, object\> queryParams)  
  * LookupAggregatedAttendancesAsync(object body, List\<string\> expand, bool expandReferenceNames)  
  * GetAggregatedAttendanceByIdAsync(string attendanceId, List\<string\> expand, bool expandReferenceNames)  
* /resources  
  * GetResourcesAsync(Dictionary\<string, object\> queryParams)  
  * LookupResourcesAsync(object body, bool expandReferenceNames)  
  * GetResourceByIdAsync(string resourceId, bool expandReferenceNames)  
* /rooms  
  * GetRoomsAsync(Dictionary\<string, object\> queryParams)  
  * LookupRoomsAsync(object body, bool expandReferenceNames)  
  * GetRoomByIdAsync(string roomId, bool expandReferenceNames)  
* /subscriptions  
  * GetSubscriptionsAsync(Dictionary\<string, object\> queryParams)  
  * CreateSubscriptionAsync(object body)  
  * DeleteSubscriptionAsync(string subscriptionId)  
  * GetSubscriptionByIdAsync(string subscriptionId)  
  * UpdateSubscriptionAsync(string subscriptionId, object body)  
* /deletedEntities  
  * GetDeletedEntitiesAsync(Dictionary\<string, object\> queryParams)  
* /log  
  * GetLogAsync(Dictionary\<string, object\> queryParams)  
* /statistics  
  * GetStatisticsAsync(Dictionary\<string, object\> queryParams)

Detailed information on available parameters can be found in the XML documentation comments within SS12000Client.cs.

The .yaml file can be downloaded from the SS12000 site over at [sis.se](https://www.sis.se/standardutveckling/tksidor/tk400499/sistk450/ss-12000/). 

## **Webhook Receiver (ASP.NET Core Example)**

To receive webhooks in a C\# application, you would typically set up an ASP.NET Core Web API endpoint. This example demonstrates a basic controller for receiving SS12000 notifications.  
This is just an example and is not part of the client library. It just shows how you could implement a receiver server for the webhooks. The code below is not production ready code, it's just a thought experiment that will point you in a direction toward a simple solution. 

```
// Save this in a separate file, e.g., \`Controllers/WebhookController.cs\`  
using Microsoft.AspNetCore.Mvc;  
using System.Text.Json;  
using System.Threading.Tasks;

namespace MySS12000App.Controllers  
{  
    \[ApiController\]  
    \[Route("\[controller\]")\]  
    public class WebhookController : ControllerBase  
    {  
        // You might inject SS12000Client here if you need to make follow-up API calls  
        // private readonly SS12000Client \_ss12000Client;

        // public WebhookController(SS12000Client ss12000Client)  
        // {  
        //     \_ss12000Client \= ss12000Client;  
        // }

        /// \<summary\>  
        /// Webhook endpoint for SS12000 notifications.  
        /// \</summary\>  
        \[HttpPost("ss12000-webhook")\]  
        public async Task\<IActionResult\> ReceiveSS12000Webhook()  
        {  
            Console.WriteLine("Received a webhook from SS12000\!");  
            foreach (var header in Request.Headers)  
            {  
                Console.WriteLine($"Header: {header.Key} \= {string.Join(", ", header.Value)}");  
            }

            try  
            {  
                using (var reader \= new System.IO.StreamReader(Request.Body))  
                {  
                    var jsonBody \= await reader.ReadToEndAsync();  
                    Console.WriteLine("Body:\\n" \+ JsonSerializer.Serialize(JsonDocument.Parse(jsonBody).RootElement, new JsonSerializerOptions { WriteIndented \= true }));

                    // Here you can implement your logic to handle the webhook message.  
                    // E.g., save the information to a database, trigger an update, etc.

                    using (JsonDocument doc \= JsonDocument.Parse(jsonBody))  
                    {  
                        if (doc.RootElement.TryGetProperty("modifiedEntites", out JsonElement modifiedEntitiesElement) && modifiedEntitiesElement.ValueKind \== JsonValueKind.Array)  
                        {  
                            foreach (var resourceType in modifiedEntitiesElement.EnumerateArray())  
                            {  
                                Console.WriteLine($"Changes for resource type: {resourceType.GetString()}");  
                                // You can call the SS12000Client here to fetch updated information  
                                // depending on the resource type.  
                                // Example: if (resourceType.GetString() \== "Person") { await \_ss12000Client.GetPersonsAsync(...); }  
                            }  
                        }

                        if (doc.RootElement.TryGetProperty("deletedEntities", out JsonElement deletedEntitiesElement) && deletedEntitiesElement.ValueKind \== JsonValueKind.Array)  
                        {  
                            Console.WriteLine("There are deleted entities to fetch from /deletedEntities.");  
                            // Call client.GetDeletedEntitiesAsync(...) to fetch the deleted IDs.  
                        }  
                    }

                    return Ok("Webhook received successfully\!");  
                }  
            }  
            catch (JsonException ex)  
            {  
                Console.WriteLine($"Error parsing JSON webhook body: {ex.Message}");  
                return BadRequest("Invalid JSON body.");  
            }  
            catch (Exception ex)  
            {  
                Console.WriteLine($"Error processing webhook: {ex.Message}");  
                return StatusCode(500, $"Internal server error: {ex.Message}");  
            }  
        }  
    }  
}
```

To enable this webhook endpoint in your ASP.NET Core application, ensure you have:

1. **Program.cs (or Startup.cs in older versions):**  

```
   // In Program.cs  
   var builder \= WebApplication.CreateBuilder(args);

   // Add services to the container.  
   builder.Services.AddControllers();  
   // Optional: Register HttpClient and SS12000Client for dependency injection  
   // builder.Services.AddHttpClient();  
   // builder.Services.AddScoped\<SS12000Client\>(sp \=\>  
   // {  
   //     var httpClient \= sp.GetRequiredService\<HttpClient\>();  
   //     var baseUrl \= builder.Configuration\["SS12000:BaseUrl"\]; // Get from appsettings.json  
   //     var authToken \= builder.Configuration\["SS12000:AuthToken"\]; // Get from appsettings.json  
   //     return new SS12000Client(baseUrl, authToken, httpClient);  
   // });

   var app \= builder.Build();

   // Configure the HTTP request pipeline.  
   app.UseRouting();  
   app.UseAuthorization(); // If you have authentication/authorization  
   app.MapControllers(); // Maps controller endpoints

   app.Run();
```

2. **appsettings.json (Optional, for configuration):**  
```
   {  
     "Logging": {  
       "LogLevel": {  
         "Default": "Information",  
         "Microsoft.AspNetCore": "Warning"  
       }  
     },  
     "AllowedHosts": "\*",  
     "SS12000": {  
       "BaseUrl": "https://some.server.se/v2.0",  
       "AuthToken": "YOUR\_JWT\_TOKEN\_HERE"  
     }  
   }
```

3. **Run the application:** dotnet run. Your webhook endpoint will typically be accessible at http://localhost:<port>/webhook/ss12000-webhook. Remember that for the SS12000 API to reach your webhook, it must be publicly accessible (e.g., through a reverse proxy or tunneling service like ngrok).

## **Contribute**

Contributions are welcome! If you want to add, improve, optimize or just change things just send in a pull request and I will have a look. Found a bug and don't know how to fix it? Create an issue!

## **License**

This project is licensed under the MIT License.