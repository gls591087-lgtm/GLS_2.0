using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using KartRider.Collections;
using KartRider.Data;
using KartRider.Game.Players;
using KartRider.IO.Packet;
using KartRider.Net;

namespace KartRider.Game;

public sealed class Garage : NumericalKeyedCollection<Player>, ISerializable
{
	public int Id { get; set; }

	public int PlayerId { get; set; }

	public ushort GameId { get; set; }

	public string Password { get; set; }

	public string ItemPassword { get; set; }

	public ushort Kart1GameId { get; set; }

	public ushort Kart2GameId { get; set; }

	public bool IsProtected => !string.IsNullOrEmpty(Password);

	public bool IsItemProtected => !string.IsNullOrEmpty(ItemPassword);

	public Player Owner
	{
		get
		{
			lock (this)
			{
				return this.SingleOrDefault((Player p) => p.Id == PlayerId);
			}
		}
	}

	private bool Assigned { get; set; }

	public Garage(int playerId)
	{
		PlayerId = playerId;
	}

	public bool Load()
	{
		dynamic datum = new Datum("garages");
		try
		{
			datum.Populate("PlayerId = '{0}'", PlayerId);
			Assigned = true;
			Id = datum.Id;
			GameId = datum.GameId;
			Password = datum.Password;
			ItemPassword = datum.ItemPassword;
			Kart1GameId = datum.Kart1GameId;
			Kart2GameId = datum.Kart2GameId;
			return true;
		}
		catch (RowNotInTableException)
		{
			return false;
		}
	}

	public void Save()
	{
		dynamic datum = new Datum("garages");
		datum.PlayerId = PlayerId;
		datum.GameId = GameId;
		datum.Password = Password;
		datum.ItemPassword = ItemPassword;
		datum.Kart1GameId = Kart1GameId;
		datum.Kart2GameId = Kart2GameId;
		if (Assigned)
		{
			datum.Update("PlayerId = '{0}'", PlayerId);
			return;
		}
		datum.Insert();
		Id = Database.Fetch("garages", "Id", "PlayerId = '{0}'", PlayerId);
		Assigned = true;
	}

	public void Delete()
	{
		Database.Delete("garages", "Id = '{0}'", Id);
	}

	public void Serialize(OutPacket p)
	{
		p.WriteUShort(GameId);
		p.WriteBool(IsProtected);
		p.WriteBool(value: false);
		p.WriteBool(IsItemProtected);
		p.WriteString(Password);
		p.WriteString(string.Empty);
		p.WriteString(ItemPassword);
		p.WriteUShort(Kart1GameId);
		p.WriteUShort(Kart2GameId);
	}

	public void Deserialize(InPacket p)
	{
	}

	public int GetIndex(Player player)
	{
		int index = 1;
		using (IEnumerator<Player> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				Player loopPlayer = enumerator.Current;
				if (loopPlayer.Id != PlayerId)
				{
					if (loopPlayer == player)
					{
						return index;
					}
					index++;
				}
			}
		}
		return -1;
	}

	protected override int GetKeyForItem(Player item)
	{
		return item.Id;
	}

	protected override void InsertItem(int index, Player item)
	{
		if (Contains(item.Id))
		{
			throw new InvalidOperationException();
		}
		if (item.Id != PlayerId && base.Count >= 7)
		{
			throw new InvalidOperationException();
		}
		item.Garage = this;
		base.InsertItem(index, item);
	}

	protected override void RemoveItem(int index)
	{
		Player item = GetAtIndex(index);
		if (item == null)
		{
			throw new InvalidOperationException();
		}
		item.Garage = null;
		base.RemoveItem(index);
		UpdateSlotData();
	}

	public void UpdateSlotData()
	{
		int index = 0;
		using OutPacket outPacket = new OutPacket(RTTIValues.RmSlotDataPacket);
		Player owner = Owner;
		outPacket.WriteInt(owner?.AccountId ?? 0);
		outPacket.WriteEndpoint((owner != null) ? owner.Client.RemoteEndPoint : new IPEndPoint(0L, 0));
		outPacket.WriteEndpoint((owner != null) ? owner.Client.UDPEndPoint : new IPEndPoint(0L, 0));
		outPacket.WriteString((owner != null) ? owner.Name : string.Empty);
		outPacket.WriteString(string.Empty);
		outPacket.WriteSByte(owner?.License ?? 0);
		if (owner != null)
		{
            owner.Items.Serialize(outPacket);
        }
		else
		{
			for (int i = 0; i < 16; i++)
			{
				outPacket.WriteUShort(0);
			}
		}
		outPacket.WriteInt(owner?.RP ?? 0);
		outPacket.WriteSByte(0);
		outPacket.WriteInt();
		outPacket.WriteInt();
		outPacket.WriteInt();
		outPacket.WriteInt();
		outPacket.WriteInt();
		outPacket.WriteInt();
		index = 1;
		lock (this)
		{
			using IEnumerator<Player> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				Player player = enumerator.Current;
				if (player.Id != PlayerId)
				{
					outPacket.WriteInt(player.AccountId);
					outPacket.WriteEndpoint(player.Client.RemoteEndPoint);
					outPacket.WriteEndpoint(player.Client.UDPEndPoint);
					outPacket.WriteString(player.Name);
					outPacket.WriteString(string.Empty);
					outPacket.WriteSByte(player.License);
                    player.Items.Serialize(outPacket);
                    outPacket.WriteInt(player.RP);
					outPacket.WriteSByte(0);
					outPacket.WriteInt();
					outPacket.WriteInt();
					outPacket.WriteInt();
					outPacket.WriteInt();
					outPacket.WriteInt();
					outPacket.WriteInt();
					index++;
				}
			}
		}
		for (; index < 8; index++)
		{
			outPacket.WriteInt();
			outPacket.WriteInt();
			outPacket.WriteUShort(0);
			outPacket.WriteInt();
			outPacket.WriteUShort(0);
			outPacket.WriteString(string.Empty);
			outPacket.WriteString(string.Empty);
			outPacket.WriteSByte(0);
			for (int j = 0; j < 16; j++)
			{
				outPacket.WriteUShort(0);
			}
			outPacket.WriteUShort(0);
			outPacket.WriteInt();
			outPacket.WriteByte(0);
			outPacket.WriteInt();
			outPacket.WriteInt();
			outPacket.WriteInt();
			outPacket.WriteInt();
			outPacket.WriteInt();
			outPacket.WriteInt();
		}
		Broadcast(outPacket);
	}

	public void Broadcast(OutPacket outPacket)
	{
		lock (this)
		{
			using IEnumerator<Player> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				Player player = enumerator.Current;
				player.Client.Send(outPacket);
			}
		}
	}

	public void BroadcastUDP(
		UdpOutPacket outPacket,
		Player skipped = null)
	{
		lock (this)
		{
			using IEnumerator<Player> enumerator = GetEnumerator();

			while (enumerator.MoveNext())
			{
				Player player = enumerator.Current;

				if (player != skipped &&
					player.Client.UDPEndPoint != null)
				{
					Server.UdpListener.BeginSend(
						outPacket,
						player.Client.UDPEndPoint);
				}
			}
		}
	}
}