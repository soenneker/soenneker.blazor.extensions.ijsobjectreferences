using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Soenneker.Blazor.Extensions.IJSObjectReferences;

/// <summary>
/// Extension methods that add operation context to JavaScript invocation failures.
/// </summary>
public static class IJSObjectReferencesExtensions
{
    /// <summary>
    /// Invokes a JavaScript function and deserializes its result using the supplied JSON type metadata,
    /// adding its identifier to script failures.
    /// </summary>
    /// <remarks>
    /// The result passes through a <see cref="JsonElement"/>. The metadata controls result deserialization only;
    /// arguments still use the JS runtime's serialization. Disconnect, cancellation, and JSON deserialization
    /// exceptions propagate unchanged.
    /// </remarks>
    /// <returns>The deserialized result, which may be null.</returns>
    public static ValueTask<T?> InvokeWithContextAsync<T>(this IJSObjectReference module, string identifier, JsonTypeInfo<T> jsonTypeInfo,
        params object?[]? args) =>
        module.InvokeWithContextAsync(identifier, jsonTypeInfo, CancellationToken.None, args);

    /// <summary>
    /// Invokes a JavaScript function using the supplied cancellation token and deserializes its result using
    /// the supplied JSON type metadata, adding its identifier to script failures and retaining the original inner exception.
    /// </summary>
    /// <remarks>
    /// The result passes through a <see cref="JsonElement"/>. The metadata controls result deserialization only;
    /// arguments still use the JS runtime's serialization. Disconnect, cancellation, and JSON deserialization
    /// exceptions propagate unchanged.
    /// </remarks>
    /// <returns>The deserialized result, which may be null.</returns>
    public static async ValueTask<T?> InvokeWithContextAsync<T>(this IJSObjectReference module, string identifier, JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        JsonElement result = await module.InvokeWithContextAsync<JsonElement>(identifier, cancellationToken, args);
        return result.Deserialize(jsonTypeInfo);
    }

    /// <summary>
    /// Invokes a JavaScript function without a return value, adding its identifier to script failures.
    /// </summary>
    /// <remarks>Disconnect and cancellation exceptions propagate unchanged.</remarks>
    public static ValueTask InvokeVoidWithContextAsync(this IJSObjectReference module, string identifier, params object?[]? args) =>
        module.InvokeVoidWithContextAsync(identifier, CancellationToken.None, args);

    /// <summary>
    /// Invokes a JavaScript function without a return value using the supplied cancellation token,
    /// adding its identifier to script failures while preserving the original exception as the inner exception.
    /// </summary>
    /// <remarks>Disconnect and cancellation exceptions propagate unchanged.</remarks>
    public static async ValueTask InvokeVoidWithContextAsync(this IJSObjectReference module, string identifier, CancellationToken cancellationToken,
        params object?[]? args)
    {
        try
        {
            await module.InvokeVoidAsync(identifier, cancellationToken, args);
        }
        catch (JSException exception)
        {
            throw new JSException($"JavaScript operation '{identifier}' failed: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Invokes a JavaScript function and returns its result, adding its identifier to script failures.
    /// </summary>
    /// <remarks>Disconnect and cancellation exceptions propagate unchanged.</remarks>
    public static ValueTask<T> InvokeWithContextAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        this IJSObjectReference module, string identifier, params object?[]? args) =>
        module.InvokeWithContextAsync<T>(identifier, CancellationToken.None, args);

    /// <summary>
    /// Invokes a JavaScript function and returns its result using the supplied cancellation token,
    /// adding its identifier to script failures while preserving the original exception as the inner exception.
    /// </summary>
    /// <remarks>Disconnect and cancellation exceptions propagate unchanged.</remarks>
    public static async ValueTask<T> InvokeWithContextAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        this IJSObjectReference module, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        try
        {
            return await module.InvokeAsync<T>(identifier, cancellationToken, args);
        }
        catch (JSException exception)
        {
            throw new JSException($"JavaScript operation '{identifier}' failed: {exception.Message}", exception);
        }
    }
}