# OATFG (OpenAPI Automated Testing Framework Generator)
----
The goal of this project is to provide a simple way to generate a testing framework for an OpenAPI specification. The framework will be generated in a way that it can be used to test the API endpoints and the responses. Initial supported output will be C# / XUnit .NET8. Robot Framework is also minimally supported. This has only been tested with OpenAPI v3 and newer files.


## How it works

### C# / XUnit
The tool will take an OpenAPI specification file and generate a C# project with the following structure:
* A file containing a fixture class for each endpoint / path in the OpenAPI specification. This class will contain:
    * A private property for the endpoint url in each class. This property will take in the path values as arguments and return the endpoint path. 
      * Each property will have the following documentation:
          * The HTTP method
          * The description of the endpoint
          * The expected response codes
          * If the endpoint requires a request body / if not this will not appear
          * Path parameters (if any)
```csharp
// Method: GET
// Description: Get a list of all the users
// Expected Response Codes: 200, 400, 401, 403, 404, 500
// Requires Request Body: true
private string users(string id, string name)
    => $"/users/{id}/{name}";

// -- OR --

// Method: GET
// Description: Get a list of all the users
// Expected Response Codes: 200, 400, 401, 403, 404, 500
private string users(string id, string name)
    => $"/users/{id}/{name}";
```

* A corresponding method for the path named Request. This method will take in the path variables, query variables (if any), and if the endpoint requires a request body, a path to the request body json file.

```csharp
/// <summary>
/// Sends a GET request to the /users/{id}/{name} endpoint
/// </summary>
/// <param name="id">The id of the user</param>
/// <param name="name">The name of the user</param>
/// <param name="requestBodyPath">The path to the request body json file</param>
public async Task<HttpResponseMessage> Request(string id, string name, string requestBodyPath = null)
{
    requestBodyPath ?? = $"{basePathToBodies}/users.json";
    CreateRequestWithBody(HttpMethod.Get, users(id, name), requestBodyPath)
}

// -- OR --

/// <summary>
/// Sends a GET request to the /users/{id}/{name} endpoint
/// </summary>
/// <param name="id">The id of the user</param>
/// <param name="name">The name of the user</param>

public async Task<HttpResponseMessage> Request(string id, string name)
	=> CreateRequest(HttpMethod.Get, users(id, name))
```

* A Base Fixture class will be created for the endpoint fixtures to inherit from. It will contain the following methods:
   * A private method to send the request to the endpoint. This method will take in the HTTP method, the endpoint path, and the path to the request body json file (if any). It will return the response from the endpoint.
   * A method to create a request. This method will take in the HTTP method, and a string for the endpoint path. If the endpoint has query parameters, the string will be modified to include them. It will call the private method with the request and it will return the EndpointRequest object.
   * A method to create a request with a body. This method will take in the HTTP method, a string for the endpoint path, and a string for the path to the request body json file. It will call the private method with the request and it will return the EndpointRequest object.
   * A method to return the contents of the request body file. This method will take in a string for the path to the request body json file. It will return the contents of the file as a string.

### Folder Structure
```
|[CSXOutputFolder]                         (Output Folder)
+--|[{CSXProjectName}]                     (Solution Folder)
   +--|[{CSXCoreProjectName}]              (Core Project Folder)
   |  +--{CSXCoreProjectName}.csproj
   |  +--EndpointBase.cs
   |  +--{Endpoint1}.cs
   |  +--{Endpoint2}.cs
   |  +-- ...
   |  +--{EndpointN}.cs
   +--|[{CSXTestProjectName}]              (Test Project Folder))  
   |  +--{CSXTestProjectName}.csproj
   |  +--|[TestData]                       (Test Data Folder)
   |  |  +--|[RequestBodies]               (Request Body Folder)
   |  |  |  +--{Tag1}RequestBody.json
   |  |  |  +--{Tag2}RequestBody.json
   |  |  |  +--{TagN}RequestBody.json
   |  |  +--{Tag1}TestData.json
   |  |  +--{Tag2}TestData.json
   |  |  +--{TagN}TestData.json
   |  +--{Tag1}Tests.cs
   |  +--{Tag2}Tests.cs
   |  +--{TagN}Tests.cs
   +--|[XunitExtensions]                   (XUnit Extensions Folder)
   |  +--XunitExtensions.csproj
   |  +--XunitExtensions.cs
   +--CSXProjectName.sln
```