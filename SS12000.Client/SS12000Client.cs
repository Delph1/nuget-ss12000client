// SS12000Client.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace SS12000.Client
{
    /// <summary>
    /// SS12000 API Client.
    /// This library provides functions to interact with the SS12000 API
    /// based on the provided OpenAPI specification.
    /// It includes basic HTTP calls and Bearer Token authentication handling.
    /// </summary>
    public class SS12000Client : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SS12000Client"/> class.
        /// </summary>
        /// <param name="baseUrl">Base URL for the SS12000 API (e.g., "https://some.server.se/v2.0").</param>
        /// <param name="authToken">JWT Bearer Token for authentication.</param>
        /// <param name="httpClient">Optional custom HttpClient instance. If not provided, a new one will be created.</param>
        public SS12000Client(string baseUrl, string authToken = null, HttpClient httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl), "Base URL is mandatory for SS12000Client.");
            }

            // Add HTTPS check for baseUrl
            if (!baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Warning: Base URL does not use HTTPS. All communication should occur over HTTPS " +
                                  "in production environments to ensure security.");
            }

            if (string.IsNullOrWhiteSpace(authToken))
            {
                Console.WriteLine("Warning: Authentication token is missing. Calls may fail if the API requires authentication.");
            }

            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = httpClient ?? new HttpClient();

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }

        /// <summary>
        /// Performs a generic HTTP request to the API.
        /// </summary>
        /// <param name="method">HTTP method (GET, POST, DELETE, PATCH).</param>
        /// <param name="path">API path (e.g., "/organisations").</param>
        /// <param name="queryParams">Query parameters.</param>
        /// <param name="jsonContent">JSON request body.</param>
        /// <returns>Response from the API.</returns>
        /// <exception cref="HttpRequestException">If the request fails.</exception>
        private async Task<T> RequestAsync<T>(HttpMethod method, string path, Dictionary<string, object> queryParams = null, object jsonContent = null)
        {
            // Use Uri constructor for robust path combining
            string requestUri = new Uri(new Uri(_baseUrl), path).ToString();

            if (queryParams != null)
            {
                // QueryHelpers.AddQueryString requires the full URI string
                foreach (var param in queryParams)
                {
                    if (param.Value == null) continue;

                    if (param.Value is IEnumerable<string> stringList)
                    {
                        foreach (var item in stringList)
                        {
                            requestUri = QueryHelpers.AddQueryString(requestUri, param.Key, item);
                        }
                    }
                    else if (param.Value is bool boolValue)
                    {
                        requestUri = QueryHelpers.AddQueryString(requestUri, param.Key, boolValue.ToString().ToLowerInvariant());
                    }
                    else if (param.Value is not null)
                    {
                        requestUri = QueryHelpers.AddQueryString(requestUri, param.Key, param.Value.ToString());
                    }
                }
            }

            var request = new HttpRequestMessage(method, requestUri);

            if (jsonContent != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(jsonContent), Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = null; // Declare response here

            try
            {
                response = await _httpClient.SendAsync(request); // Assign response here
                response.EnsureSuccessStatusCode(); // Throws HttpRequestException for 4xx or 5xx responses

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default(T); // For DELETE or other 204 responses
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error during {method} call to {requestUri}: {e.Message}");
                // Access the HttpResponseMessage from the captured 'response' variable
                if (response != null)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"API Error Response: {errorContent}");
                }
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unexpected error occurred: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs a generic HTTP request to the API without expecting a return value.
        /// </summary>
        /// <param name="method">HTTP method (DELETE).</param>
        /// <param name="path">API path.</param>
        /// <param name="queryParams">Query parameters.</param>
        /// <param name="jsonContent">JSON request body.</param>
        /// <exception cref="HttpRequestException">If the request fails.</exception>
        private async Task RequestNoContentAsync(HttpMethod method, string path, Dictionary<string, object> queryParams = null, object jsonContent = null)
        {
            // Use Uri constructor for robust path combining
            string requestUri = new Uri(new Uri(_baseUrl), path).ToString();

            if (queryParams != null)
            {
                // QueryHelpers.AddQueryString requires the full URI string
                foreach (var param in queryParams)
                {
                    if (param.Value == null) continue;

                    if (param.Value is IEnumerable<string> stringList)
                    {
                        foreach (var item in stringList)
                        {
                            requestUri = QueryHelpers.AddQueryString(requestUri, param.Key, item);
                        }
                    }
                    else if (param.Value is bool boolValue)
                    {
                        requestUri = QueryHelpers.AddQueryString(requestUri, param.Key, boolValue.ToString().ToLowerInvariant());
                    }
                    else if (param.Value is not null)
                    {
                        requestUri = QueryHelpers.AddQueryString(requestUri, param.Key, param.Value.ToString());
                    }
                }
            }

            var request = new HttpRequestMessage(method, requestUri);

            if (jsonContent != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(jsonContent), Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = null; // Declare response here

            try
            {
                response = await _httpClient.SendAsync(request); // Assign response here
                response.EnsureSuccessStatusCode(); // Throws HttpRequestException for 4xx or 5xx responses
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error during {method} call to {requestUri}: {e.Message}");
                // Access the HttpResponseMessage from the captured 'response' variable
                if (response != null)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"API Error Response: {errorContent}");
                }
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unexpected error occurred: {e.Message}");
                throw;
            }
        }

        // --- Organisation Endpoints ---

        /// <summary>
        /// Get a list of organizations.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <param name="parent">Filter by parent organization IDs.</param>
        /// <param name="schoolUnitCode">Filter by school unit codes.</param>
        /// <param name="organisationCode">Filter by organization codes.</param>
        /// <param name="municipalityCode">Filter by municipality code.</param>
        /// <param name="type">Filter by organization type.</param>
        /// <param name="schoolTypes">Filter by school types.</param>
        /// <param name="startDate.onOrBefore">Filter by start date on or before.</param>
        /// <param name="startDate.onOrAfter">Filter by start date on or after.</param>
        /// <param name="endDate.onOrBefore">Filter by end date on or before.</param>
        /// <param name="endDate.onOrAfter">Filter by end date on or after.</param>
        /// <param name="meta.created.before">Filter by metadata created before.</param>
        /// <param name="meta.created.after">Filter by metadata created after.</param>
        /// <param name="meta.modified.before">Filter by metadata modified before.</param>
        /// <param name="meta.modified.after">Filter by metadata modified after.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <param name="sortkey">Sort key for the results.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        /// <param name="pageToken">Token for pagination.</param>
        /// <returns>A list of organizations.</returns>
        public async Task<JsonElement> GetOrganisationsAsync(
            IEnumerable<string> parent = null,
            IEnumerable<string> schoolUnitCode = null,
            IEnumerable<string> organisationCode = null,
            string municipalityCode = null,
            IEnumerable<string> type = null,
            IEnumerable<string> schoolTypes = null,
            DateTime? startDateOnOrBefore = null,
            DateTime? startDateOnOrAfter = null,
            DateTime? endDateOnOrBefore = null,
            DateTime? endDateOnOrAfter = null,
            DateTime? metaCreatedBefore = null,
            DateTime? metaCreatedAfter = null,
            DateTime? metaModifiedBefore = null,
            DateTime? metaModifiedAfter = null,
            bool? expandReferenceNames = null,
            string sortkey = null,
            int? limit = null,
            string pageToken = null)
        {
            var queryParams = new Dictionary<string, object>();

            if (parent != null) queryParams.Add("parent", parent);
            if (schoolUnitCode != null) queryParams.Add("schoolUnitCode", schoolUnitCode);
            if (organisationCode != null) queryParams.Add("organisationCode", organisationCode);
            if (!string.IsNullOrEmpty(municipalityCode)) queryParams.Add("municipalityCode", municipalityCode);
            if (type != null) queryParams.Add("type", type);
            if (schoolTypes != null) queryParams.Add("schoolTypes", schoolTypes);

            // Dates (RFC3339 date for start/end dates)
            if (startDateOnOrBefore.HasValue) queryParams.Add("startDate.onOrBefore", startDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (startDateOnOrAfter.HasValue)  queryParams.Add("startDate.onOrAfter", startDateOnOrAfter.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrBefore.HasValue)   queryParams.Add("endDate.onOrBefore", endDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrAfter.HasValue)    queryParams.Add("endDate.onOrAfter", endDateOnOrAfter.Value.ToString("yyyy-MM-dd"));

            // Meta timestamps (RFC3339 date-time)
            if (metaCreatedBefore.HasValue)  queryParams.Add("meta.created.before", metaCreatedBefore.Value.ToString("o"));
            if (metaCreatedAfter.HasValue)   queryParams.Add("meta.created.after", metaCreatedAfter.Value.ToString("o"));
            if (metaModifiedBefore.HasValue) queryParams.Add("meta.modified.before", metaModifiedBefore.Value.ToString("o"));
            if (metaModifiedAfter.HasValue)  queryParams.Add("meta.modified.after", metaModifiedAfter.Value.ToString("o"));

            if (expandReferenceNames.HasValue) queryParams.Add("expandReferenceNames", expandReferenceNames.Value);
            if (!string.IsNullOrEmpty(sortkey)) queryParams.Add("sortkey", sortkey);
            if (limit.HasValue) queryParams.Add("limit", limit.Value);
            if (!string.IsNullOrEmpty(pageToken)) queryParams.Add("pageToken", pageToken);


            return await RequestAsync<JsonElement>(HttpMethod.Get, "/organisations", queryParams);
        }

        /// <summary>
        /// Get multiple organizations based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of organizations.</returns>
        public async Task<JsonElement> LookupOrganisationsAsync(object body, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/organisations/lookup", queryParams, body);
        }

        /// <summary>
        /// Get an organization by ID.
        /// </summary>
        /// <param name="orgId">ID of the organization.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The organization object.</returns>
        public async Task<JsonElement> GetOrganisationByIdAsync(string orgId, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/organisations/{orgId}", queryParams);
        }

        // --- Person Endpoints ---

        /// <summary>
        /// Get a list of persons.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <param name="nameContains">Filter by name contains.</param>
        /// <param name="civicNo">Filter by civic number.</param>
        /// <param name="eduPersonPrincipalName">Filter by eduPersonPrincipalName.</param>
        /// <param name="identifierValue">Filter by identifier value.</param>
        /// <param name="identifierContext">Filter by identifier context.</param>
        /// <param name="relationshipEntityType">Filter by relationship entity type.</param>
        /// <param name="relationOrganisation">Filter by relation organisation.</param>
        /// <param name="relationshipStartOnOrBefore">Filter by relationship start date on or before.</param>
        /// <param name="relationshipStartOnOrAfter">Filter by relationship start date on or after.</param>
        /// <param name="relationshipEndOnOrBefore">Filter by relationship end date on or before.</param>
        /// <param name="relationshipEndOnOrAfter">Filter by relationship end date on or after.</param>
        /// <param name="metaCreatedBefore">Filter by metadata created before.</param>
        /// <param name="metaCreatedAfter">Filter by metadata created after.</param>
        /// <param name="metaModifiedBefore">Filter by metadata modified before.</param>
        /// <param name="metaModifiedAfter">Filter by metadata modified after.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <param name="sortkey">Sort key for the results.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        /// <param name="pageToken">Token for pagination.</param>
        /// <returns>A list of persons.</returns>
        public async Task<JsonElement> GetPersonsAsync(
            string nameContains = null,
            string civicNo = null,
            string eduPersonPrincipalName = null,
            string identifierValue = null,
            string identifierContext = null,
            string relationshipEntityType = null,
            string relationshipOrganisation = null,
            DateTime? relationshipStartOnOrBefore = null,
            DateTime? relationshipStartOnOrAfter = null,
            DateTime? relationshipEndOnOrBefore = null,
            DateTime? relationshipEndOnOrAfter = null,
            DateTime? metaCreatedBefore = null,
            DateTime? metaCreatedAfter = null,
            DateTime? metaModifiedBefore = null,
            DateTime? metaModifiedAfter = null,
            List<string> expand = null,
            bool? expandReferenceNames = null,
            string sortkey = null,
            int? limit = null,
            string pageToken = null)
        {
            var queryParams = new Dictionary<string, object>();

            if (nameContains != null) queryParams.Add("name", nameContains);
            if (!string.IsNullOrEmpty(civicNo)) queryParams.Add("civicNo", civicNo);
            if (!string.IsNullOrEmpty(eduPersonPrincipalName)) queryParams.Add("eduPersonPrincipalName", eduPersonPrincipalName);
            if (!string.IsNullOrEmpty(identifierValue)) queryParams.Add("identifiers.value", identifierValue);
            if (!string.IsNullOrEmpty(identifierContext)) queryParams.Add("identifiers.context", identifierContext);

            if (!string.IsNullOrEmpty(relationshipEntityType)) queryParams.Add("relationship.entity.type", relationshipEntityType);
            if (!string.IsNullOrEmpty(relationshipOrganisation)) queryParams.Add("relation.organisation", relationshipOrganisation);

            // Relation date filters (RFC3339 date)
            if (relationshipStartOnOrBefore.HasValue) queryParams.Add("relationship.startDate.onOrBefore", relationshipStartOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (relationshipStartOnOrAfter.HasValue)  queryParams.Add("relationship.startDate.onOrAfter", relationshipStartOnOrAfter.Value.ToString("yyyy-MM-dd"));
            if (relationshipEndOnOrBefore.HasValue)   queryParams.Add("relationship.endDate.onOrBefore", relationshipEndOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (relationshipEndOnOrAfter.HasValue)    queryParams.Add("relationship.endDate.onOrAfter", relationshipEndOnOrAfter.Value.ToString("yyyy-MM-dd"));

            // Meta timestamps (RFC3339 date-time)
            if (metaCreatedBefore.HasValue)  queryParams.Add("meta.created.before", metaCreatedBefore.Value.ToString("o"));
            if (metaCreatedAfter.HasValue)   queryParams.Add("meta.created.after", metaCreatedAfter.Value.ToString("o"));
            if (metaModifiedBefore.HasValue) queryParams.Add("meta.modified.before", metaModifiedBefore.Value.ToString("o"));
            if (metaModifiedAfter.HasValue)  queryParams.Add("meta.modified.after", metaModifiedAfter.Value.ToString("o"));

            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames.HasValue) queryParams.Add("expandReferenceNames", expandReferenceNames.Value);
            if (!string.IsNullOrEmpty(sortkey)) queryParams.Add("sortkey", sortkey);
            if (limit.HasValue) queryParams.Add("limit", limit.Value);
            if (!string.IsNullOrEmpty(pageToken)) queryParams.Add("pageToken", pageToken);

            return await RequestAsync<JsonElement>(HttpMethod.Get, "/persons", queryParams);
        }

        /// <summary>
        /// Get multiple persons based on a list of IDs or civic numbers.
        /// </summary>
        /// <param name="body">Request body with IDs or civic numbers.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of persons.</returns>
        public async Task<JsonElement> LookupPersonsAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/persons/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a person by person ID.
        /// </summary>
        /// <param name="personId">ID of the person.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The person object.</returns>
        public async Task<JsonElement> GetPersonByIdAsync(string personId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/persons/{personId}", queryParams);
        }

        // --- Placements Endpoints ---

        /// <summary>
        /// Get a list of placements.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <param name="organisation">Filter by organization ID.</param>
        /// <param name="group">Filter by group ID.</param>
        /// <param name="startDateOnOrBefore">Filter by start date on or before.</param>
        /// <param name="startDateOnOrAfter">Filter by start date on or after.</param>
        /// <param name="endDateOnOrBefore">Filter by end date on or before.</param>
        /// <param name="endDateOnOrAfter">Filter by end date on or after.</param>
        /// <param name="child">Filter by child ID.</param>
        /// <param name="owner">Filter by owner ID.</param>
        /// <param name="metaCreatedBefore">Filter by metadata created before.</param>
        /// <param name="metaCreatedAfter">Filter by metadata created after.</param>
        /// <param name="metaModifiedBefore">Filter by metadata modified before.</param>
        /// <param name="metaModifiedAfter">Filter by metadata modified after.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <param name="sortkey">Sort key for the results.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        /// <param name="pageToken">Token for pagination.</param>
        /// <returns>A list of placements.</returns>
        public async Task<JsonElement> GetPlacementsAsync(
            string organisation = null,
            string group = null,
            DateTime? startDateOnOrBefore = null,
            DateTime? startDateOnOrAfter = null,
            DateTime? endDateOnOrBefore = null,
            DateTime? endDateOnOrAfter = null,
            string child = null,
            string owner = null,
            DateTime? metaCreatedBefore = null,
            DateTime? metaCreatedAfter = null,
            DateTime? metaModifiedBefore = null,
            DateTime? metaModifiedAfter = null,
            List<string> expand = null,
            bool? expandReferenceNames = null,
            string sortkey = null,
            int? limit = null,
            string pageToken = null)
        {
            var queryParams = new Dictionary<string, object>();

            if (organisation != null) queryParams.Add("organisation", organisation);
            if (group != null) queryParams.Add("group", group);

            // Date filters (date-only)
            if (startDateOnOrBefore.HasValue) queryParams.Add("startDate.onOrBefore", startDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (startDateOnOrAfter.HasValue)  queryParams.Add("startDate.onOrAfter",  startDateOnOrAfter.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrBefore.HasValue)   queryParams.Add("endDate.onOrBefore",   endDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrAfter.HasValue)    queryParams.Add("endDate.onOrAfter",    endDateOnOrAfter.Value.ToString("yyyy-MM-dd"));

            if (child != null) queryParams.Add("child", child);
            if (owner != null) queryParams.Add("owner", owner);

            // Meta timestamps (RFC3339 / ISO 8601)
            if (metaCreatedBefore.HasValue) queryParams.Add("meta.created.before", metaCreatedBefore.Value.ToString("o"));
            if (metaCreatedAfter.HasValue)   queryParams.Add("meta.created.after",  metaCreatedAfter.Value.ToString("o"));
            if (metaModifiedBefore.HasValue) queryParams.Add("meta.modified.before", metaModifiedBefore.Value.ToString("o"));
            if (metaModifiedAfter.HasValue)  queryParams.Add("meta.modified.after",  metaModifiedAfter.Value.ToString("o"));

            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames.HasValue) queryParams.Add("expandReferenceNames", expandReferenceNames.Value);
            if (!string.IsNullOrEmpty(sortkey)) queryParams.Add("sortkey", sortkey);
            if (limit.HasValue) queryParams.Add("limit", limit.Value);
            if (!string.IsNullOrEmpty(pageToken)) queryParams.Add("pageToken", pageToken);

            return await RequestAsync<JsonElement>(HttpMethod.Get, "/placements", queryParams);
        }

        /// <summary>
        /// Get multiple placements based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of placements.</returns>
        public async Task<JsonElement> LookupPlacementsAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/placements/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a placement by ID.
        /// </summary>
        /// <param name="placementId">ID of the placement.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The placement object.</returns>
        public async Task<JsonElement> GetPlacementByIdAsync(string placementId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/placements/{placementId}", queryParams);
        }

        // --- Duties Endpoints ---

        /// <summary>
        /// Get a list of duties.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <param name="person">Filter by person IDs.</param>
        /// <param name="organisation">Filter by organization IDs.</param>
        /// <param name="dutyRole">Filter by duty role IDs.</param>
        /// <param name="startDateOnOrBefore">Filter by start date on or before.</param>
        /// <param name="startDateOnOrAfter">Filter by start date on or after.</param>
        /// <param name="endDateOnOrBefore">Filter by end date on or before.</param>
        /// <param name="endDateOnOrAfter">Filter by end date on or after.</param>
        /// <param name="metaCreatedBefore">Filter by metadata created before.</param>
        /// <param name="metaCreatedAfter">Filter by metadata created after.</param>
        /// <param name="metaModifiedBefore">Filter by metadata modified before.</param>
        /// <param name="metaModifiedAfter">Filter by metadata modified after.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <param name="sortkey">Sort key for the results.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        /// <param name="pageToken">Token for pagination.</param>
        /// <returns>A list of duties.</returns>
        public async Task<JsonElement> GetDutiesAsync(
            IEnumerable<string> person = null,
            IEnumerable<string> organisation = null,
            IEnumerable<string> dutyRole = null,
            DateTime? startDateOnOrBefore = null,
            DateTime? startDateOnOrAfter = null,
            DateTime? endDateOnOrBefore = null,
            DateTime? endDateOnOrAfter = null,
            DateTime? metaCreatedBefore = null,
            DateTime? metaCreatedAfter = null,
            DateTime? metaModifiedBefore = null,
            DateTime? metaModifiedAfter = null,
            List<string> expand = null,
            bool? expandReferenceNames = null,
            string sortkey = null,
            int? limit = null,
            string pageToken = null)
        {
            var queryParams = new Dictionary<string, object>();

            if (person != null) queryParams.Add("person", person);
            if (organisation != null) queryParams.Add("organisation", organisation);
            if (dutyRole != null) queryParams.Add("dutyRole", dutyRole);

            // Date filters (date-only)
            if (startDateOnOrBefore.HasValue) queryParams.Add("startDate.onOrBefore", startDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (startDateOnOrAfter.HasValue)  queryParams.Add("startDate.onOrAfter",  startDateOnOrAfter.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrBefore.HasValue)   queryParams.Add("endDate.onOrBefore",   endDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrAfter.HasValue)    queryParams.Add("endDate.onOrAfter",    endDateOnOrAfter.Value.ToString("yyyy-MM-dd"));

            // Meta timestamps (RFC3339 / ISO 8601)
            if (metaCreatedBefore.HasValue)  queryParams.Add("meta.created.before", metaCreatedBefore.Value.ToString("o"));
            if (metaCreatedAfter.HasValue)   queryParams.Add("meta.created.after",  metaCreatedAfter.Value.ToString("o"));
            if (metaModifiedBefore.HasValue) queryParams.Add("meta.modified.before", metaModifiedBefore.Value.ToString("o"));
            if (metaModifiedAfter.HasValue)  queryParams.Add("meta.modified.after",  metaModifiedAfter.Value.ToString("o"));

            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames.HasValue) queryParams.Add("expandReferenceNames", expandReferenceNames.Value);
            if (!string.IsNullOrEmpty(sortkey)) queryParams.Add("sortkey", sortkey);
            if (limit.HasValue) queryParams.Add("limit", limit.Value);
            if (!string.IsNullOrEmpty(pageToken)) queryParams.Add("pageToken", pageToken);

            return await RequestAsync<JsonElement>(HttpMethod.Get, "/duties", queryParams);
        }

        /// <summary>
        /// Get multiple duties based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of duties.</returns>
        public async Task<JsonElement> LookupDutiesAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/duties/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a duty by ID.
        /// </summary>
        /// <param name="dutyId">ID of the duty.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The duty object.</returns>
        public async Task<JsonElement> GetDutyByIdAsync(string dutyId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/duties/{dutyId}", queryParams);
        }

        // --- Groups Endpoints ---

        /// <summary>
        /// Get a list of groups.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <param name="groupType">Filter by group type.</param>
        /// <param name="organisation">Filter by organization IDs.</param>
        /// <param name="schoolTypes">Filter by school types.</param>
        /// <param name="startDateOnOrBefore">Filter by start date on or before.</param>
        /// <param name="startDateOnOrAfter">Filter by start date on or after.</param>
        /// <param name="endDateOnOrBefore">Filter by end date on or before.</param>
        /// <param name="endDateOnOrAfter">Filter by end date on or after.</param>
        /// <param name="metaCreatedBefore">Filter by metadata created before.</param>
        /// <param name="metaCreatedAfter">Filter by metadata created after.</param>
        /// <param name="metaModifiedBefore">Filter by metadata modified before.</param>
        /// <param name="metaModifiedAfter">Filter by metadata modified after.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <param name="sortkey">Sort key for the results.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        /// <param name="pageToken">Token for pagination.</param>
        /// <returns>A list of groups.</returns>
        public async Task<JsonElement> GetGroupsAsync(
            IEnumerable<string> groupType = null,
            IEnumerable<string> organisation = null,
            IEnumerable<string> schoolTypes = null,
            DateTime? startDateOnOrBefore = null,
            DateTime? startDateOnOrAfter = null,
            DateTime? endDateOnOrBefore = null,
            DateTime? endDateOnOrAfter = null,
            DateTime? metaCreatedBefore = null,
            DateTime? metaCreatedAfter = null,
            DateTime? metaModifiedBefore = null,
            DateTime? metaModifiedAfter = null,
            List<string> expand = null,
            bool? expandReferenceNames = null,
            string sortkey = null,
            int? limit = null,
            string pageToken = null)
        {
            var queryParams = new Dictionary<string, object>();

            if (groupType != null) queryParams.Add("groupType", groupType);
            if (organisation != null) queryParams.Add("organisation", organisation);
            if (schoolTypes != null) queryParams.Add("schoolTypes", schoolTypes);

            // Date filters (date-only)
            if (startDateOnOrBefore.HasValue) queryParams.Add("startDate.onOrBefore", startDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (startDateOnOrAfter.HasValue)  queryParams.Add("startDate.onOrAfter",  startDateOnOrAfter.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrBefore.HasValue)   queryParams.Add("endDate.onOrBefore",   endDateOnOrBefore.Value.ToString("yyyy-MM-dd"));
            if (endDateOnOrAfter.HasValue)    queryParams.Add("endDate.onOrAfter",    endDateOnOrAfter.Value.ToString("yyyy-MM-dd"));

            // Meta timestamps (RFC3339 / ISO 8601)
            if (metaCreatedBefore.HasValue)  queryParams.Add("meta.created.before", metaCreatedBefore.Value.ToString("o"));
            if (metaCreatedAfter.HasValue)   queryParams.Add("meta.created.after",  metaCreatedAfter.Value.ToString("o"));
            if (metaModifiedBefore.HasValue) queryParams.Add("meta.modified.before", metaModifiedBefore.Value.ToString("o"));
            if (metaModifiedAfter.HasValue)  queryParams.Add("meta.modified.after",  metaModifiedAfter.Value.ToString("o"));

            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames.HasValue) queryParams.Add("expandReferenceNames", expandReferenceNames.Value);
            if (!string.IsNullOrEmpty(sortkey)) queryParams.Add("sortkey", sortkey);
            if (limit.HasValue) queryParams.Add("limit", limit.Value);
            if (!string.IsNullOrEmpty(pageToken)) queryParams.Add("pageToken", pageToken);

            return await RequestAsync<JsonElement>(HttpMethod.Get, "/groups", queryParams);
        }

        /// <summary>
        /// Get multiple groups based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of groups.</returns>
        public async Task<JsonElement> LookupGroupsAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/groups/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a group by ID.
        /// </summary>
        /// <param name="groupId">ID of the group.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The group object.</returns>
        public async Task<JsonElement> GetGroupByIdAsync(string groupId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/groups/{groupId}", queryParams);
        }

        // --- Programmes Endpoints ---

        /// <summary>
        /// Get a list of programmes.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <param name="schoolType">Filter by school type.</param>
        /// <param name="code">Filter by programme code.</param>
        /// <param name="parentProgramme">Filter by parent programme ID.</param>
        /// <param name="metaCreatedBefore">Filter by metadata created before.</param>
        /// <param name="metaCreatedAfter">Filter by metadata created after.</param>
        /// <param name="metaModifiedBefore">Filter by metadata modified before.</param>
        /// <param name="metaModifiedAfter">Filter by metadata modified after.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <param name="sortkey">Sort key for the results.</param>
        /// <param name="limit">Maximum number of results to return.</param>
        /// <param name="pageToken">Token for pagination.</param>
        /// <returns>A list of programmes.</returns>
        public async Task<JsonElement> GetProgrammesAsync(
            IEnumerable<string> schoolType = null,
            string code = null,
            string parentProgramme = null,
            DateTime? metaCreatedBefore = null,
            DateTime? metaCreatedAfter = null,
            DateTime? metaModifiedBefore = null,
            DateTime? metaModifiedAfter = null,
            bool? expandReferenceNames = null,
            string sortkey = null,
            int? limit = null,
            string pageToken = null)
        {
            var queryParams = new Dictionary<string, object>();

            if (schoolType != null) queryParams.Add("schoolType", schoolType);
            if (code != null) queryParams.Add("code", code);
            if (parentProgramme != null) queryParams.Add("parentProgramme", parentProgramme);

            // Meta timestamps (RFC3339 / ISO 8601)
            if (metaCreatedBefore.HasValue)  queryParams.Add("meta.created.before", metaCreatedBefore.Value.ToString("o"));
            if (metaCreatedAfter.HasValue)   queryParams.Add("meta.created.after",  metaCreatedAfter.Value.ToString("o"));
            if (metaModifiedBefore.HasValue) queryParams.Add("meta.modified.before", metaModifiedBefore.Value.ToString("o"));
            if (metaModifiedAfter.HasValue)  queryParams.Add("meta.modified.after",  metaModifiedAfter.Value.ToString("o"));

            if (expandReferenceNames.HasValue) queryParams.Add("expandReferenceNames", expandReferenceNames.Value);
            if (!string.IsNullOrEmpty(sortkey)) queryParams.Add("sortkey", sortkey);
            if (limit.HasValue) queryParams.Add("limit", limit.Value);
            if (!string.IsNullOrEmpty(pageToken)) queryParams.Add("pageToken", pageToken);

            return await RequestAsync<JsonElement>(HttpMethod.Get, "/programmes", queryParams);
        }

        /// <summary>
        /// Get multiple programmes based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of programmes.</returns>
        public async Task<JsonElement> LookupProgrammesAsync(object body, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/programmes/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a programme by ID.
        /// </summary>
        /// <param name="programmeId">ID of the programme.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The programme object.</returns>
        public async Task<JsonElement> GetProgrammeByIdAsync(string programmeId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/programmes/{programmeId}", queryParams);
        }

        // --- StudyPlans Endpoints ---

        /// <summary>
        /// Get a list of study plans.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of study plans.</returns>
        public async Task<JsonElement> GetStudyPlansAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/studyplans", queryParams);
        }

        /// <summary>
        /// Get multiple study plans based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of study plans.</returns>
        public async Task<JsonElement> LookupStudyPlansAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/studyplans/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a study plan by ID.
        /// </summary>
        /// <param name="studyPlanId">ID of the study plan.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The study plan object.</returns>
        public async Task<JsonElement> GetStudyPlanByIdAsync(string studyPlanId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/studyplans/{studyPlanId}", queryParams);
        }

        // --- Syllabuses Endpoints ---

        /// <summary>
        /// Get a list of syllabuses.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of syllabuses.</returns>
        public async Task<JsonElement> GetSyllabusesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/syllabuses", queryParams);
        }

        /// <summary>
        /// Get multiple syllabuses based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of syllabuses.</returns>
        public async Task<JsonElement> LookupSyllabusesAsync(object body, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/syllabuses/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a syllabus by ID.
        /// </summary>
        /// <param name="syllabusId">ID of the syllabus.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The syllabus object.</returns>
        public async Task<JsonElement> GetSyllabusByIdAsync(string syllabusId, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/syllabuses/{syllabusId}", queryParams);
        }

        // --- SchoolUnitOfferings Endpoints ---

        /// <summary>
        /// Get a list of school unit offerings.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of school unit offerings.</returns>
        public async Task<JsonElement> GetSchoolUnitOfferingsAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/schoolUnitOfferings", queryParams);
        }

        /// <summary>
        /// Get multiple school unit offerings based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of school unit offerings.</returns>
        public async Task<JsonElement> LookupSchoolUnitOfferingsAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/schoolUnitOfferings/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a school unit offering by ID.
        /// </summary>
        /// <param name="offeringId">ID of the school unit offering.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The school unit offering object.</returns>
        public async Task<JsonElement> GetSchoolUnitOfferingByIdAsync(string offeringId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/schoolUnitOfferings/{offeringId}", queryParams);
        }

        // --- Activities Endpoints ---

        /// <summary>
        /// Get a list of activities.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of activities.</returns>
        public async Task<JsonElement> GetActivitiesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/activities", queryParams);
        }

        /// <summary>
        /// Get multiple activities based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of activities.</returns>
        public async Task<JsonElement> LookupActivitiesAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/activities/lookup", queryParams, body);
        }

        /// <summary>
        /// Get an activity by ID.
        /// </summary>
        /// <param name="activityId">ID of the activity.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The activity object.</returns>
        public async Task<JsonElement> GetActivityByIdAsync(string activityId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/activities/{activityId}", queryParams);
        }

        // --- CalendarEvents Endpoints ---

        /// <summary>
        /// Get a list of calendar events.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of calendar events.</returns>
        public async Task<JsonElement> GetCalendarEventsAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/calendarEvents", queryParams);
        }

        /// <summary>
        /// Get multiple calendar events based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of calendar events.</returns>
        public async Task<JsonElement> LookupCalendarEventsAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/calendarEvents/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a calendar event by ID.
        /// </summary>
        /// <param name="eventId">ID of the calendar event.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The calendar event object.</returns>
        public async Task<JsonElement> GetCalendarEventByIdAsync(string eventId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/calendarEvents/{eventId}", queryParams);
        }

        // --- Attendances Endpoints ---

        /// <summary>
        /// Get a list of attendances.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of attendances.</returns>
        public async Task<JsonElement> GetAttendancesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/attendances", queryParams);
        }

        /// <summary>
        /// Get multiple attendances based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of attendances.</returns>
        public async Task<JsonElement> LookupAttendancesAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/attendances/lookup", queryParams, body);
        }

        /// <summary>
        /// Get an attendance by ID.
        /// </summary>
        /// <param name="attendanceId">ID of the attendance.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The attendance object.</returns>
        public async Task<JsonElement> GetAttendanceByIdAsync(string attendanceId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/attendances/{attendanceId}", queryParams);
        }

        /// <summary>
        /// Delete an attendance by ID.
        /// </summary>
        /// <param name="attendanceId">ID of the attendance to delete.</param>
        public async Task DeleteAttendanceAsync(string attendanceId)
        {
            await RequestNoContentAsync(HttpMethod.Delete, $"/attendances/{attendanceId}");
        }

        // --- AttendanceEvents Endpoints ---

        /// <summary>
        /// Get a list of attendance events.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of attendance events.</returns>
        public async Task<JsonElement> GetAttendanceEventsAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/attendanceEvents", queryParams);
        }

        /// <summary>
        /// Get multiple attendance events based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of attendance events.</returns>
        public async Task<JsonElement> LookupAttendanceEventsAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/attendanceEvents/lookup", queryParams, body);
        }

        /// <summary>
        /// Get an attendance event by ID.
        /// </summary>
        /// <param name="eventId">ID of the attendance event.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The attendance event object.</returns>
        public async Task<JsonElement> GetAttendanceEventByIdAsync(string eventId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/attendanceEvents/{eventId}", queryParams);
        }

        // --- AttendanceSchedules Endpoints ---

        /// <summary>
        /// Get a list of attendance schedules.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of attendance schedules.</returns>
        public async Task<JsonElement> GetAttendanceSchedulesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/attendanceSchedules", queryParams);
        }

        /// <summary>
        /// Get multiple attendance schedules based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of attendance schedules.</returns>
        public async Task<JsonElement> LookupAttendanceSchedulesAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/attendanceSchedules/lookup", queryParams, body);
        }

        /// <summary>
        /// Get an attendance schedule by ID.
        /// </summary>
        /// <param name="scheduleId">ID of the attendance schedule.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The attendance schedule object.</returns>
        public async Task<JsonElement> GetAttendanceScheduleByIdAsync(string scheduleId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/attendanceSchedules/{scheduleId}", queryParams);
        }

        // --- Grades Endpoints ---

        /// <summary>
        /// Get a list of grades.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of grades.</returns>
        public async Task<JsonElement> GetGradesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/grades", queryParams);
        }

        /// <summary>
        /// Get multiple grades based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of grades.</returns>
        public async Task<JsonElement> LookupGradesAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/grades/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a grade by ID.
        /// </summary>
        /// <param name="gradeId">ID of the grade.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The grade object.</returns>
        public async Task<JsonElement> GetGradeByIdAsync(string gradeId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/grades/{gradeId}", queryParams);
        }

        // --- AggregatedAttendance Endpoints ---

        /// <summary>
        /// Get a list of aggregated attendances.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of aggregated attendances.</returns>
        public async Task<JsonElement> GetAggregatedAttendancesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/aggregatedAttendance", queryParams);
        }

        /// <summary>
        /// Get multiple aggregated attendances based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of aggregated attendances.</returns>
        public async Task<JsonElement> LookupAggregatedAttendancesAsync(object body, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/aggregatedAttendance/lookup", queryParams, body);
        }

        /// <summary>
        /// Get an aggregated attendance by ID.
        /// </summary>
        /// <param name="attendanceId">ID of the aggregated attendance.</param>
        /// <param name="expand">Describes if expanded data should be fetched.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The aggregated attendance object.</returns>
        public async Task<JsonElement> GetAggregatedAttendanceByIdAsync(string attendanceId, List<string> expand = null, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expand != null) queryParams.Add("expand", expand);
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/aggregatedAttendance/{attendanceId}", queryParams);
        }

        // --- Resources Endpoints ---

        /// <summary>
        /// Get a list of resources.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of resources.</returns>
        public async Task<JsonElement> GetResourcesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/resources", queryParams);
        }

        /// <summary>
        /// Get multiple resources based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of resources.</returns>
        public async Task<JsonElement> LookupResourcesAsync(object body, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/resources/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a resource by ID.
        /// </summary>
        /// <param name="resourceId">ID of the resource.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The resource object.</returns>
        public async Task<JsonElement> GetResourceByIdAsync(string resourceId, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/resources/{resourceId}", queryParams);
        }

        // --- Rooms Endpoints ---

        /// <summary>
        /// Get a list of rooms.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of rooms.</returns>
        public async Task<JsonElement> GetRoomsAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/rooms", queryParams);
        }

        /// <summary>
        /// Get multiple rooms based on a list of IDs.
        /// </summary>
        /// <param name="body">Request body with IDs.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>A list of rooms.</returns>
        public async Task<JsonElement> LookupRoomsAsync(object body, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/rooms/lookup", queryParams, body);
        }

        /// <summary>
        /// Get a room by ID.
        /// </summary>
        /// <param name="roomId">ID of the room.</param>
        /// <param name="expandReferenceNames">Return `displayName` for all referenced objects.</param>
        /// <returns>The room object.</returns>
        public async Task<JsonElement> GetRoomByIdAsync(string roomId, bool expandReferenceNames = false)
        {
            var queryParams = new Dictionary<string, object>();
            if (expandReferenceNames) queryParams.Add("expandReferenceNames", true);
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/rooms/{roomId}", queryParams);
        }

        // --- Subscriptions (Webhooks) Endpoints ---

        /// <summary>
        /// Get a list of subscriptions.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of subscriptions.</returns>
        public async Task<JsonElement> GetSubscriptionsAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/subscriptions", queryParams);
        }

        /// <summary>
        /// Create a subscription.
        /// </summary>
        /// <param name="body">Request body with subscription details.</param>
        /// <returns>The created subscription object.</returns>
        public async Task<JsonElement> CreateSubscriptionAsync(object body)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Post, "/subscriptions", jsonContent: body);
        }

        /// <summary>
        /// Delete a subscription.
        /// </summary>
        /// <param name="subscriptionId">ID of the subscription to delete.</param>
        public async Task DeleteSubscriptionAsync(string subscriptionId)
        {
            await RequestNoContentAsync(HttpMethod.Delete, $"/subscriptions/{subscriptionId}");
        }

        /// <summary>
        /// Get a subscription by ID.
        /// </summary>
        /// <param name="subscriptionId">ID of the subscription.</param>
        /// <returns>The subscription object.</returns>
        public async Task<JsonElement> GetSubscriptionByIdAsync(string subscriptionId)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, $"/subscriptions/{subscriptionId}");
        }

        /// <summary>
        /// Update the expire time of a subscription by ID.
        /// </summary>
        /// <param name="subscriptionId">ID of the subscription to update.</param>
        /// <param name="body">Request body with expiry timestamp.</param>
        /// <returns>The updated subscription object.</returns>
        public async Task<JsonElement> UpdateSubscriptionAsync(string subscriptionId, object body)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Patch, $"/subscriptions/{subscriptionId}", jsonContent: body);
        }

        // --- DeletedEntities Endpoint ---

        /// <summary>
        /// Get a list of deleted entities.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of deleted entities.</returns>
        public async Task<JsonElement> GetDeletedEntitiesAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/deletedEntities", queryParams);
        }

        // --- Log Endpoint ---

        /// <summary>
        /// Get a list of log entries.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of log entries.</returns>
        public async Task<JsonElement> GetLogAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/log", queryParams);
        }

        // --- Statistics Endpoint ---

        /// <summary>
        /// Get a list of statistics.
        /// </summary>
        /// <param name="queryParams">Filter parameters.</param>
        /// <returns>A list of statistics.</returns>
        public async Task<JsonElement> GetStatisticsAsync(Dictionary<string, object> queryParams = null)
        {
            return await RequestAsync<JsonElement>(HttpMethod.Get, "/statistics", queryParams);
        }

        /// <summary>
        /// Disposes the HttpClient instance if it was created internally.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects)
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                }
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            // Set large fields to null
            _disposed = true;
        }

        // // Example Usage (for testing purposes, not part of the library itself)
        // public static async Task Main(string[] args)
        // {
        //     // Replace with your actual test server URL and JWT token
        //     const string baseUrl = "https://some.server.se/v2.0";
        //     const string authToken = "YOUR_JWT_TOKEN_HERE";

        //     using (var client = new SS12000Client(baseUrl, authToken))
        //     {
        //         try
        //         {
        //             // Example: Get Organizations
        //             Console.WriteLine("\nFetching organizations...");
        //             var organizations = await client.GetOrganisationsAsync(new Dictionary<string, object> { { "limit", 2 } });
        //             Console.WriteLine("Fetched organizations:\n" + JsonSerializer.Serialize(organizations, new JsonSerializerOptions { WriteIndented = true }));

        //             if (organizations.TryGetProperty("data", out var orgsArray) && orgsArray.ValueKind == JsonValueKind.Array && orgsArray.GetArrayLength() > 0)
        //             {
        //                 var firstOrgId = orgsArray[0].GetProperty("id").GetString();
        //                 Console.WriteLine($"\nFetching organization with ID: {firstOrgId}...");
        //                 var orgById = await client.GetOrganisationByIdAsync(firstOrgId, true); // expandReferenceNames = true
        //                 Console.WriteLine("Fetched organization by ID:\n" + JsonSerializer.Serialize(orgById, new JsonSerializerOptions { WriteIndented = true }));
        //             }

        //             // Example: Get Persons
        //             Console.WriteLine("\nFetching persons...");
        //             var persons = await client.GetPersonsAsync(new Dictionary<string, object> { { "limit", 2 }, { "expand", new List<string> { "duties" } } });
        //             Console.WriteLine("Fetched persons:\n" + JsonSerializer.Serialize(persons, new JsonSerializerOptions { WriteIndented = true }));

        //             if (persons.TryGetProperty("data", out var personsArray) && personsArray.ValueKind == JsonValueKind.Array && personsArray.GetArrayLength() > 0)
        //             {
        //                 var firstPersonId = personsArray[0].GetProperty("id").GetString();
        //                 Console.WriteLine($"\nFetching person with ID: {firstPersonId}...");
        //                 var personById = await client.GetPersonByIdAsync(firstPersonId, new List<string> { "duties", "responsibleFor" }, true);
        //                 Console.WriteLine("Fetched person by ID:\n" + JsonSerializer.Serialize(personById, new JsonSerializerOptions { WriteIndented = true }));
        //             }

        //             // Example: Manage Subscriptions (Webhooks)
        //             Console.WriteLine("\nFetching subscriptions...");
        //             var subscriptions = await client.GetSubscriptionsAsync();
        //             Console.WriteLine("Fetched subscriptions:\n" + JsonSerializer.Serialize(subscriptions, new JsonSerializerOptions { WriteIndented = true }));

        //             // Example: Create a subscription (requires a publicly accessible webhook URL)
        //             // Console.WriteLine("\nCreating a subscription...");
        //             // var newSubscription = await client.CreateSubscriptionAsync(new
        //             // {
        //             //     name = "My CSharp Test Subscription",
        //             //     target = "http://your-public-webhook-url.com/ss12000-webhook", // Replace with your public URL
        //             //     resourceTypes = new[] { new { resource = "Person" }, new { resource = "Activity" } }
        //             // });
        //             // Console.WriteLine("Created subscription:\n" + JsonSerializer.Serialize(newSubscription, new JsonSerializerOptions { WriteIndented = true }));

        //             // Example: Delete a subscription
        //             // if (subscriptions.TryGetProperty("data", out var subsArray) && subsArray.ValueKind == JsonValueKind.Array && subsArray.GetArrayLength() > 0)
        //             // {
        //             //     var subToDeleteId = subsArray[0].GetProperty("id").GetString();
        //             //     Console.WriteLine($"\nDeleting subscription with ID: {subToDeleteId}...");
        //             //     await client.DeleteSubscriptionAsync(subToDeleteId);
        //             //     Console.WriteLine("Subscription deleted successfully.");
        //             // }
        //         }
        //         catch (HttpRequestException e)
        //         {
        //             Console.WriteLine($"An HTTP request error occurred: {e.Message}");
        //         }
        //         catch (Exception e)
        //         {
        //             Console.WriteLine($"An unexpected error occurred: {e.Message}");
        //         }
        //     }
        // }
    }
}
