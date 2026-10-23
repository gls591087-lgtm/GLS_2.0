using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace KartRider.Net;

public sealed class KartClients : List<KartClient>
{
    // =========================================================
    // PLAYER
    // =========================================================

    public KartClient GetByPlayerId(
        int playerId)
    {
        return this.SingleOrDefault(
            c =>
                c != null &&
                c.Player != null &&
                c.Player.Id == playerId);
    }

    // =========================================================
    // ACCOUNT
    // =========================================================

    public KartClient GetByAccountId(
        int accountId)
    {
        foreach (KartClient client in this)
        {
            if (client == null)
                continue;

            if (client.Account == null)
                continue;

            if (client.Account.Id == accountId)
                return client;
        }

        return null;
    }

    // =========================================================
    // UDP SOURCE
    // =========================================================

    public KartClient GetByUdpSource(
        IPEndPoint endpoint)
    {
        if (endpoint == null)
            return null;

        KartClient found = null;

        foreach (KartClient client in this)
        {
            if (client == null)
                continue;

            IPEndPoint tcpEndpoint =
                client.RemoteEndPoint;

            if (tcpEndpoint == null)
                continue;

            if (!tcpEndpoint.Address.Equals(
                    endpoint.Address))
            {
                continue;
            }

            if (found != null)
            {
                // If multiple TCP sessions share the same IP,
                // do not identify them by IP alone.
                return null;
            }

            found = client;
        }

        return found;
    }

    // =========================================================
    // UDP ENDPOINT
    // =========================================================

    public KartClient GetByUdpEndpoint(
        IPEndPoint endpoint)
    {
        if (endpoint == null)
            return null;

        foreach (KartClient client in this)
        {
            if (client == null)
                continue;

            if (client.MatchesUdpEndpoint(
                    endpoint))
            {
                return client;
            }
        }

        return null;
    }

    // =========================================================
    // FIND UDP CLIENT
    // =========================================================

    public KartClient FindUdpClient(
        uint accountId,
        IPEndPoint udpEndpoint)
    {
        if (udpEndpoint == null)
            return null;

        int id;

        try
        {
            id =
                checked((int)accountId);
        }
        catch
        {
            return null;
        }

        // =====================================================
        // 1. AccountId
        // =====================================================

        KartClient accountClient =
            GetByAccountId(id);

        if (accountClient != null)
        {
            return accountClient;
        }

        // =====================================================
        // 2. UDP endpoint
        // =====================================================

        KartClient udpClient =
            GetByUdpEndpoint(
                udpEndpoint);

        if (udpClient != null)
        {
            return udpClient;
        }

        // =====================================================
        // 3. TCP IP
        // =====================================================

        return GetByUdpSource(
            udpEndpoint);
    }
}