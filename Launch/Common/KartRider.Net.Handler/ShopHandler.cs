using KartRider.Game;
using KartRider.IO.Packet;
using KartRider.IO;

namespace KartRider.Net.Handler;

public static class ShopHandler
{
    [PacketHandler(RTTIValues.SpReqEnterShopPacket)]
    public static void HandleEnterShop(KartClient c, InPacket p)
    {
        if (c.Player == null)
            return;

        using OutPacket outPacket =
            new OutPacket(RTTIValues.SpRepEnterShopPacket);

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.SpRqRemainCashPacket)]
    public static void HandleSetRemainingCash(KartClient c, InPacket p)
    {
        if (c.Player == null)
            return;

        using OutPacket outPacket =
            new OutPacket(RTTIValues.SpRpRemainCashPacket);

        outPacket.WriteInt(c.Player.Cash);

        byte[] payload = outPacket.ToArray();

        Log.Inform(
            "[RemainCash] Cash={0}, Length={1}",
            c.Player.Cash,
            payload.Length);

        Log.Inform(
            "[RemainCash] HEX={0}",
            Convert.ToHexString(payload));

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.SpReqLeaveShopPacket)]
    public static void HandleLeaveShop(KartClient c, InPacket p)
    {
        if (c.Player == null)
            return;

        using OutPacket outPacket =
            new OutPacket(RTTIValues.SpRepLeaveShopPacket);

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.SpRqGetGiftListIncomingPacket)]
    public static void HandleGetGiftList(KartClient c, InPacket p)
    {
        if (c.Player == null)
            return;

        using OutPacket outPacket =
            new OutPacket(RTTIValues.SpRpGetGiftListIncomingPacket);

        outPacket.WriteInt(c.Player.Gifts.Count);

        foreach (Gift gift in c.Player.Gifts)
        {
            outPacket.WriteInt(gift.GameId);
            outPacket.WriteShort(0);
            outPacket.WriteShort(0);
            outPacket.WriteShort(0);
            outPacket.WriteShort(0);
            outPacket.WriteString(gift.Message);
            outPacket.WriteShort(0);
            outPacket.WriteString(gift.Sender);
            outPacket.WriteByte(0);
        }

        c.Send(outPacket);
    }

    [PacketHandler(RTTIValues.SpReqBuyItemPacket)]
    public static void HandleBuyItem(KartClient c, InPacket p)
    {
        if (c.Player == null)
            return;

        int gameId = p.ReadInt();

        Log.Inform(
            "Shop buy request: Player={0}, GameID={1}, Cash={2}, Lucci={3}",
            c.Player.Name,
            gameId,
            c.Player.Cash,
            c.Player.Lucci);

        if (gameId <= 0 || gameId > ushort.MaxValue)
        {
            Log.Error(
                "Invalid shop GameID={0}",
                gameId);

            return;
        }

        ushort itemGameId = (ushort)gameId;

        bool added = c.Player.Items.AddItem(
            itemGameId,
            ItemType.Kart,
            (short)1,
            -1L,
            false);

        if (!added)
        {
            Log.Error(
                "Shop buy failed. GameID={0}",
                itemGameId);

            return;
        }

        c.Player.Items.Save();

        Log.Inform(
            "Shop buy success. Player={0}, GameID={1}",
            c.Player.Name,
            itemGameId);
    }
}