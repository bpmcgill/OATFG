# OATFG (OpenAPI Automated Testing Framework Generator)
----
The goal of this project is to provide a simple way to generate a testing framework for an OpenAPI specification. The framework will be generated in a way that it can be used to test the API endpoints and the responses. Outputs test suites in C# / xUnit (.NET 10 LTS), Python (pytest), Robot Framework, and JavaScript (node:test), each linted with its own toolchain, plus a desktop UI (WPF + MVVM Toolkit) with four cyberpunk themes. Tested with OpenAPI 3.0.x (3.0.0–3.0.4), 3.1.x (3.1.0–3.1.2), and 3.2.x (3.2.0–3.2.1) specification files (JSON and YAML).


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
   +--{CSXProjectName}.sln                 (Solution File)
   +--|Core                                (Core Project Folder)
   |  +--{CSXCoreProjectName}.csproj       (C# Project File)
   |  +--|Client
   |  |  +--AccessClient.cs
   |  +--|Data
   |  |  +--Responses.cs
   |  +--|Endpoints
   |     +--EndpointBase.cs
   |     +--{Endpoint1}.cs
   |     +--{Endpoint2}.cs
   |     +-- ...
   |     +--{EndpointN}.cs
   +--|Tests                               (Test Project Folder)
      +--{CSXTestProjectName}.csproj       (C# Project File)
      +--appsettings.json
      +--|TestData                         (Test Data Folder)
         +--|RequestBodies                 (Request Body Folder)
```

The generated framework is a .NET 10 (LTS) solution and is buildable out of the box. `{CSXProjectName}.sln` links the `{CSXCoreProjectName}` class library with the `{CSXTestProjectName}` xUnit test project. Open the solution or run:

```
dotnet test {CSXProjectName}.sln
```

`Tests/appsettings.json` is seeded with `APIHost`, taken from the OpenAPI document's server URL; `AccessClient` reads it when building request URLs. No test methods are generated yet — add your own `[Fact]` methods to the test project using the endpoint fixtures.

## Desktop UI

`OATFG.UI` is a WPF app (CommunityToolkit.Mvvm): pick a spec, name the project, choose environment/authentication, select output languages, and generate. Every generated file is linted in place and the pass/fail result is listed next to the file. The API host defaults to the spec's server URL and can be overridden per suite.

Four cyberpunk themes are built in and switch live (no restart): **Cyberpunk Dark**, **Cyberpunk Light**, **Cyberpunk Dark Pastel**, **Cyberpunk Light Pastel**. The neon palette (blue, purple, red, green, pink, orange) shifts to pastel tones for the pastel themes and deepens for light backgrounds.

## Output languages

Beyond C# / xUnit, the generator emits ready-to-run suites from the same endpoint model:

| Language | Stack | Location under the output root | Lint gate |
|---|---|---|---|
| C# / xUnit | .NET 10 (LTS) | `{Project}/` (full solution) | solution build + tests |
| Python | pytest + requests | `python/{Project}/` | `python -m py_compile` |
| Robot Framework | RequestsLibrary | `robot/{Project}/` | `robot --dryrun` |
| JavaScript | node:test + fetch (zero dependencies) | `javascript/{Project}/` | `node --check` |

Each suite contains one test per endpoint that calls the endpoint with sample values and asserts an expected status code from the spec. Python and JavaScript read `OATFG_BASE_URL` (falling back to the spec's server URL); Robot uses the `${BASE_URL}` variable in each `.robot` file.

## Tests

The repository ships a full test suite in `OATFG.Tests`:

* **Unit — utilities**: string manipulation (PascalCase, file-safe names, casing, special-character cleanup) and file/folder helpers.
* **Unit — transformer**: spec parsing across every published OpenAPI version (3.0.0 – 3.2.1, JSON and YAML), endpoint/metadata extraction, path and query parameters, `$ref` request and response bodies, server fallback, environment, project naming, and authentication configuration.
* **Unit — generator**: emitted solution structure — project files, `EndpointBase`, endpoint fixtures, `AccessClient`, `appsettings.json`, and the solution file.
* **Unit — generators**: Python / Robot / JavaScript suite emission (per-endpoint tests, sample values, status assertions, package files).
* **Integration — linting**: every generated Python, JavaScript, and Robot file must pass its real toolchain linter (`py_compile`, `node --check`, `robot --dryrun`); tests skip when a toolchain is absent.
* **End-to-end**: generates a framework from the Pet Store spec (GET/POST/PUT/PATCH/DELETE/OPTIONS, path/query parameters, request bodies), serves the spec with a live HTTP server, then builds and runs the generated solution with `dotnet test`.

Run everything from the repository root:

```
dotnet test
```

## License

Released under a dual license (see [LICENSE](LICENSE)): **free for open-source projects** with attribution, **paid for commercial use** — commercial use requires a separate license from the copyright holder. CI runs on GitHub Actions (`.github/workflows/ci.yml`).