namespace secret67;

public static class LocalUrl
{
    // Та же логика, что в IUrlHelper.IsLocalUrl из ASP.NET Core
    public static bool IsLocal(string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/')
            return false;
        if (url.Length == 1)
            return true;
        if (url[1] == '/' || url[1] == '\\')
            return false;
        // Браузеры вырезают tab/перевод строки, и "/\t/evil.com" превращается в "//evil.com"
        return !url.AsSpan(1).ContainsAnyInRange('\0', '\x1F') && !url.Contains('\x7F');
    }

    public static string OrRoot(string? url) => IsLocal(url) ? url! : "/";
}
