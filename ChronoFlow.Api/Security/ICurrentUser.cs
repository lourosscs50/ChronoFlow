namespace ChronoFlow.Api.Security;

public interface ICurrentUser
{
bool IsAuthenticated { get; }
string? UserId { get; }
string? Email { get; }
}
