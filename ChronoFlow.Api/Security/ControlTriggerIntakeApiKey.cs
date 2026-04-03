using ChronoFlow.Api.Options;

namespace ChronoFlow.Api.Security;

/// <summary>Shared X-Api-Key check for control trigger intake and execution query endpoints.</summary>
public static class ControlTriggerIntakeApiKey
{
    public static bool TryValidate(
        HttpRequest httpRequest,
        ControlTriggersIntakeOptions options,
        out IResult failureResult)
    {
        failureResult = Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(options.ExpectedApiKey))
            return true;

        if (!httpRequest.Headers.TryGetValue("X-Api-Key", out var provided) || provided.Count == 0)
            return false;

        if (!string.Equals(provided.ToString(), options.ExpectedApiKey, StringComparison.Ordinal))
            return false;

        return true;
    }
}
