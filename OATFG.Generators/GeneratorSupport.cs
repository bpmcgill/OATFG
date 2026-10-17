using System.Text.RegularExpressions;
using OATFG.CSXUnitGenerator.DataModels;

namespace OATFG.Generators;

/// <summary>Shared helpers for the non-C# suite generators.</summary>
public static class GeneratorSupport
{
    /// <summary>Lowercase HTTP method as used by the transformer ("get", "post", ...).</summary>
    public static string EndpointMethod(Endpoint endpoint) => endpoint.Method?.ToLowerInvariant() ?? "get";

    /// <summary>camelCase → snake_case, used for Python test names.</summary>
    public static string ToSnakeCase(string input)
    {
        string[] parts = Regex.Split(input, @"(?<!^)(?=[A-Z])");
        return string.Join("_", parts).ToLowerInvariant();
    }

    /// <summary>Samples a path variable or query parameter by its schema type.</summary>
    public static string SampleValue(string type) => type?.ToLowerInvariant() switch
    {
        "integer" or "number" => "1",
        "boolean" => "true",
        _ => "test"
    };

    /// <summary>The endpoint path with path variables replaced by sample values, e.g. "/users/1".</summary>
    public static string PathWithSamples(Endpoint endpoint)
    {
        string path = endpoint.EndpointPath;
        foreach (var pathVariable in endpoint.PathVariables ?? [])
        {
            path = path.Replace("{" + pathVariable.Name + "}", SampleValue(pathVariable.Type));
        }
        return path;
    }

    /// <summary>"name=value&name2=value2" for the endpoint's query parameters, or null.</summary>
    public static string? QueryString(Endpoint endpoint)
    {
        var parameters = endpoint.QueryParameters ?? [];
        if (parameters.Count == 0)
        {
            return null;
        }
        return string.Join("&", parameters.Select(p => $"{p.Name}={SampleValue(p.Type)}"));
    }

    /// <summary>A URL suffix ready for use in generated code, e.g. "/users/1?verbose=true".</summary>
    public static string UrlSuffix(Endpoint endpoint)
    {
        string path = PathWithSamples(endpoint);
        string? query = QueryString(endpoint);
        return query is null ? path : $"{path}?{query}";
    }

    /// <summary>Expected status codes from the spec, sorted; empty when none were declared.</summary>
    public static IReadOnlyList<int> StatusCodes(Endpoint endpoint)
        => (endpoint.Responses ?? [])
        .Where(r => r.StatusCode > 0)
        .Select(r => r.StatusCode)
        .Distinct()
        .Order()
        .ToArray();

    /// <summary>Project name safe to use in folder/package names.</summary>
    public static string SafeProjectName(API api)
        => (api.ProjectName ?? api.SolutionFolder ?? "generated").Trim();
}