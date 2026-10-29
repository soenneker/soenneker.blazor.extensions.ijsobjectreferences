using System.Text.Json.Serialization;

namespace Soenneker.Blazor.Extensions.IJSObjectReferences.Tests;

[JsonSerializable(typeof(string))]
internal partial class TestJsonContext : JsonSerializerContext;
