using System.Collections.Generic;
using KartRider.Game.Data;
using KartRider.IO.Packet;

namespace KartRider.Game;

public sealed class Kart : Item, ISerializable
{
    public static List<Statistics> karts =
        new List<Statistics>();

    public Statistics Statitics { get; set; }

    public Kart(
        ushort gameId,
        long expiration,
        bool isEquipped = false)
        : base(
            gameId,
            ItemType.Kart,
          1,
            expiration,
            isEquipped)
    {
        if (!DataProvider.Karts.TryGetValue(
                gameId,
                out Statistics statistics))
        {
            throw new KeyNotFoundException(
                $"Kart data not found. GameID={gameId}");
        }

        Statitics = statistics;
    }
    public Kart(dynamic datum)
        : this(new Dictionary<string, object>
        {
            ["ID"] = (object)datum.ID,
            ["GameID"] = (object)datum.GameID,
            ["Type"] = (object)datum.Type,
            ["Quantity"] = (object)datum.Quantity,
            ["Expiration"] = (object)datum.Expiration,
            ["IsEquipped"] = (object)datum.IsEquipped
        })
    {
    }

    private static Dictionary<string, object> ConvertToDictionary(dynamic datum)
    {
        return new Dictionary<string, object>
        {
            ["ID"] = datum.ID,
            ["GameID"] = datum.GameID,
            ["Type"] = datum.Type,
            ["Quantity"] = datum.Quantity,
            ["Expiration"] = datum.Expiration,
            ["IsEquipped"] = datum.IsEquipped
        };
    }


    public Kart(Dictionary<string, object> datum)
    : base(datum)
{
    if (!DataProvider.Karts.TryGetValue(
            GameId,
            out Statistics statistics))
    {
        throw new KeyNotFoundException(
            $"Kart data not found. GameID={GameId}");
    }

    Statitics = statistics;
}

public void CalculateAdditions()
    {
        if (Player == null)
            return;

        Statitics.Mass += Player.Statistics.Mass;
        Statitics.AirFriction += Player.Statistics.AirFriction;
        Statitics.DragFactor += Player.Statistics.DragFactor;
        Statitics.ForwardAccelForce +=
            Player.Statistics.ForwardAccelForce;
        Statitics.BackwardAccelForce +=
            Player.Statistics.BackwardAccelForce;
        Statitics.GripBrakeForce +=
            Player.Statistics.GripBrakeForce;
        Statitics.SlipBrakeForce +=
            Player.Statistics.SlipBrakeForce;
        Statitics.MaxSteerAngle +=
            Player.Statistics.MaxSteerAngle;
        Statitics.SteerConstraint +=
            Player.Statistics.SteerConstraint;
        Statitics.FrontGripFactor +=
            Player.Statistics.FrontGripFactor;
        Statitics.RearGripFactor +=
            Player.Statistics.RearGripFactor;
        Statitics.DriftTriggerFactor +=
            Player.Statistics.DriftTriggerFactor;
        Statitics.DriftTriggerTime +=
            Player.Statistics.DriftTriggerTime;
        Statitics.DriftSlipFactor +=
            Player.Statistics.DriftSlipFactor;
        Statitics.DriftEscapeForce +=
            Player.Statistics.DriftEscapeForce;
        Statitics.CornerDrawFactor +=
            Player.Statistics.CornerDrawFactor;
        Statitics.DriftLeanFactor +=
            Player.Statistics.DriftLeanFactor;
        Statitics.SteerLeanFactor +=
            Player.Statistics.SteerLeanFactor;
        Statitics.DriftMaxGauge +=
            Player.Statistics.DriftMaxGauge;
        Statitics.NormalBoosterTime +=
            Player.Statistics.NormalBoosterTime;
        Statitics.TeamBoosterTime +=
            Player.Statistics.TeamBoosterTime;
    }

    public void Serialize(OutPacket p)
    {
        p.WriteEncryptedInt(
            Statitics.SpeedSlotCapacity);

        p.WriteEncryptedByte(0);
        p.WriteEncryptedByte(0);

        p.WriteEncryptedBool(
            Statitics.motorcycleType);

        p.WriteEncryptedFloat(Statitics.Mass);
        p.WriteEncryptedFloat(Statitics.AirFriction);
        p.WriteEncryptedFloat(Statitics.DragFactor);
        p.WriteEncryptedFloat(Statitics.ForwardAccelForce);
        p.WriteEncryptedFloat(Statitics.BackwardAccelForce);
        p.WriteEncryptedFloat(Statitics.GripBrakeForce);
        p.WriteEncryptedFloat(Statitics.SlipBrakeForce);
        p.WriteEncryptedFloat(Statitics.MaxSteerAngle);
        p.WriteEncryptedFloat(Statitics.SteerConstraint);
        p.WriteEncryptedFloat(Statitics.FrontGripFactor);
        p.WriteEncryptedFloat(Statitics.RearGripFactor);
        p.WriteEncryptedFloat(Statitics.DriftTriggerFactor);
        p.WriteEncryptedFloat(Statitics.DriftTriggerTime);
        p.WriteEncryptedFloat(Statitics.DriftSlipFactor);
        p.WriteEncryptedFloat(Statitics.DriftEscapeForce);
        p.WriteEncryptedFloat(Statitics.CornerDrawFactor);
        p.WriteEncryptedFloat(Statitics.DriftLeanFactor);
        p.WriteEncryptedFloat(Statitics.SteerLeanFactor);
        p.WriteEncryptedFloat(Statitics.DriftMaxGauge);
        p.WriteEncryptedFloat(Statitics.NormalBoosterTime);
        p.WriteEncryptedFloat(Statitics.TeamBoosterTime);
        p.WriteEncryptedFloat(Statitics.BoostAccelFactor);
    }

    public void Deserialize(InPacket p)
    {
    }
}