using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using TUnit.Core;

namespace Soenneker.Blazor.Extensions.IJSObjectReferences.Tests;

public sealed class IJSObjectReferencesExtensionsTests
{
    [Test]
    public async Task DefaultOverloadsPreserveRuntimeTimeoutAndArguments()
    {
        var module = new RecordingJsObjectReference();
        object?[] args = [42, "value"];
        await module.InvokeVoidWithContextAsync("run", args);
        Check(module.UsedDefaultTimeout && module.Identifier == "run" && ReferenceEquals(args, module.Arguments));
        module = new RecordingJsObjectReference { Result = 42 };
        Check(await module.InvokeWithContextAsync<int>("read", args) == 42 && module.UsedDefaultTimeout);
        module = new RecordingJsObjectReference { Result = JsonSerializer.SerializeToElement("value", TestJsonContext.Default.String) };
        Check(await module.InvokeWithContextAsync("read", TestJsonContext.Default.String, args) == "value" && module.UsedDefaultTimeout);
    }

    [Test]
    public async Task CancellationOverloadsForwardToken()
    {
        using var cts = new CancellationTokenSource();
        var module = new RecordingJsObjectReference();
        await module.InvokeVoidWithContextAsync("run", cts.Token);
        Check(module.Token == cts.Token && !module.UsedDefaultTimeout);
        await module.InvokeWithContextAsync<int>("read", cts.Token);
        Check(module.Token == cts.Token && !module.UsedDefaultTimeout);
        module.Result = JsonSerializer.SerializeToElement("value", TestJsonContext.Default.String);
        Check(await module.InvokeWithContextAsync("read", TestJsonContext.Default.String, cts.Token) == "value");
        Check(module.Token == cts.Token && !module.UsedDefaultTimeout);
    }

    [Test]
    public async Task ScriptFailuresAreWrappedOnceAndLifecycleFailuresAreUnchanged()
    {
        foreach (Exception failure in new Exception[] { new JSException("broken"), new JSDisconnectedException("gone"), new OperationCanceledException(), new ObjectDisposedException("module") })
        {
            for (int kind = 0; kind < 3; kind++)
            {
                var module = new RecordingJsObjectReference { Failure = failure };
                Exception actual = await Capture(async () =>
                {
                    if (kind == 0) await module.InvokeVoidWithContextAsync("operation");
                    else if (kind == 1) await module.InvokeWithContextAsync<int>("operation");
                    else await module.InvokeWithContextAsync("operation", TestJsonContext.Default.String);
                });
                if (failure is JSException)
                    Check(actual is JSException && ReferenceEquals(actual.InnerException, failure) && actual.Message.Contains("'operation'"));
                else
                    Check(ReferenceEquals(actual, failure));
            }
        }
    }

    [Test]
    public async Task MetadataSupportsNullAndReportsInvalidJson()
    {
        var module = new RecordingJsObjectReference { Result = JsonSerializer.SerializeToElement<string?>(null, TestJsonContext.Default.String) };
        Check(await module.InvokeWithContextAsync("read", TestJsonContext.Default.String) is null);
        module.Result = JsonSerializer.SerializeToElement(42);
        Check(await Capture(async () => { await module.InvokeWithContextAsync("read", TestJsonContext.Default.String); }) is JsonException);
        Check(await Capture(async () => { await module.InvokeWithContextAsync("read", (System.Text.Json.Serialization.Metadata.JsonTypeInfo<string>)null!); }) is ArgumentNullException);
    }

    [Test]
    public async Task TimeoutOverloadsCancelPendingInvocations()
    {
        for (int kind = 0; kind < 3; kind++)
        {
            var module = new RecordingJsObjectReference { WaitForCancellation = true };
            Exception actual = await Capture(async () =>
            {
                if (kind == 0) await module.InvokeVoidWithContextAsync("wait", TimeSpan.Zero);
                else if (kind == 1) await module.InvokeWithContextAsync<int>("wait", TimeSpan.Zero);
                else await module.InvokeWithContextAsync("wait", TestJsonContext.Default.String, TimeSpan.Zero);
            });
            Check(actual is OperationCanceledException && module.Token.CanBeCanceled && !module.UsedDefaultTimeout);
        }
    }

    private static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Contract assertion failed.");
    }

    private static async Task<Exception> Capture(Func<Task> action)
    {
        try { await action(); }
        catch (Exception exception) { return exception; }
        throw new InvalidOperationException("Expected an exception.");
    }
}
