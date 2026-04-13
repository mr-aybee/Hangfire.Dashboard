namespace Hangfire.Dashboard.Contracts;

/// <summary>
/// Standard API response wrapper with data payload.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Gets or sets a value indicating whether the request was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the response data payload.</summary>
    public T? Data { get; set; }

    /// <summary>Gets or sets the error message when <see cref="Success"/> is false.</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Standard API response wrapper without data payload.
/// </summary>
public class ApiResponse
{
    /// <summary>Gets or sets a value indicating whether the request was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the error message when <see cref="Success"/> is false.</summary>
    public string? Error { get; set; }

    /// <summary>Creates a successful response with data.</summary>
    public static ApiResponse<T> Ok<T>(T data) => new() { Success = true, Data = data };

    /// <summary>Creates a successful response without data.</summary>
    public static ApiResponse Ok() => new() { Success = true };

    /// <summary>Creates a failed response with an error message.</summary>
    public static ApiResponse Fail(string error) => new() { Success = false, Error = error };

    /// <summary>Creates a failed response with data type and error message.</summary>
    public static ApiResponse<T> Fail<T>(string error) => new() { Success = false, Error = error };
}
