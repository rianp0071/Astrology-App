public class TokenStorageService
{
    private string? _token;
    private string? _userEmail;

    public void SetToken(string token, String userEmail)
    {
        _token = token;
        _userEmail = userEmail;
    }

    public string? GetToken()
    {
        return _token;
    }

    public string? GetUserEmail()
    {
        return _userEmail;
    }

    public void ClearToken()
    {
        _token = null;
    }
}
