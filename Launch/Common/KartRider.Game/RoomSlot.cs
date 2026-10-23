using KartRider.Game.Players;

namespace KartRider.Game;

public class RoomSlot
{
    public Player Player { get; set; }

    public GameRoomState State { get; set; }

    public bool IsPrepared { get; set; }

    /*
     * Actual finish status.
     *
     * true  = finished normally
     * false = not finished yet / eligible for retirement
     */
    public bool Finished { get; set; }

    /*
     * Position for a normal finish.
     *
     * 1 = first place
     * 2 = second place
     * ...
     * 0 = not finished yet / retired
     */
    public int Rank { get; set; }

    /*
     * Actual finish time.
     *
     * Normal finish:
     *   Actual race time
     *
     * Retirement:
     *   int.MaxValue
     *
     * In the results packet, int.MaxValue is converted to -1 before sending.
     */
    public int Time { get; set; }

    public RoomSlot()
    {
        Player = null;
        State = GameRoomState.None;
        IsPrepared = false;
        Finished = false;
        Rank = 0;
        Time = 0;
    }
}