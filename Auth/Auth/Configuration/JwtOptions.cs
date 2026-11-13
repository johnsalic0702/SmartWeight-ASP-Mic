namespace Auth.Configuration;
public sealed class JwtOptions
{
    public string Secret { get; set; } = "";
    public long Expiration { get; set; } = 43_200_000;
}
