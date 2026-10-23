namespace KartRider;

public static class ItemConstants
{
    public static ItemType GetItemType(int i)
    {
        return i switch
        {
            1 => ItemType.Character,
            2 => ItemType.Color,
            3 => ItemType.Kart,
            4 => ItemType.Plate,
            6 => ItemType.TempLicense,
            7 => ItemType.SlotChanger,
            8 => ItemType.Goggle,
            9 => ItemType.Balloon,
            10 => ItemType.SuperBoss,
            11 => ItemType.HeadBand,
            12 => ItemType.HeadPhone,
            13 => ItemType.Ticket,
            14 => ItemType.UpgradeKit,
            16 => ItemType.HandGearL,
            17 => ItemType.HandGearR,
            18 => ItemType.Uniform,
            20 => ItemType.Decal,
            21 => ItemType.Pet,
            22 => ItemType.InitialCard,
            23 => ItemType.Card,
            24 => ItemType.Lottery,
            25 => ItemType.QuestKey,
            26 => ItemType.Aura,
            27 => ItemType.SkidMark,
            28 => ItemType.RoomCard,
            29 => ItemType.LucciItem,
            30 => ItemType.SpecialKit,
            31 => ItemType.RidColor,
            32 => ItemType.RpLucciBonus,
            33 => ItemType.Jewel,
            34 => ItemType.BonusCard,
            _ => ItemType.None
        };
    }
}
