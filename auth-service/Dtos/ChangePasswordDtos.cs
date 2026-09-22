namespace AuthService.Dtos;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordResponse
{
    public string Message { get; set; } = string.Empty;

    public int RevokedSessionsCount { get; set; }
}