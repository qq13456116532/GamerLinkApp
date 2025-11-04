namespace GamerLinkApp.Services;

public sealed class RagResponse
{
    public RagResponse(bool isSuccess, string message, string? errorDetail = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorDetail = errorDetail;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorDetail { get; }
}
