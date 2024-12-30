public class IPBanManager
{
    private HashSet<string> bannedIPs = new HashSet<string>();

    // IP yasakla
    public void BanIP(string ipAddress)
    {
        bannedIPs.Add(ipAddress);
    }

    // IP'nin yasaklı olup olmadığını kontrol et
    public bool IsBanned(string ipAddress)
    {
        return bannedIPs.Contains(ipAddress);
    }
}
