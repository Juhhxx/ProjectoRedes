using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public string Name => _name.ToString();
    public int EXP => _exp;
    public ulong ID => _id;
    public string Creature => _creature.ToString();
    public string Move1 => _move1.ToString();
    public string Move2 => _move2.ToString();
    public string Move3 => _move3.ToString();
    public string Move4 => _move4.ToString();
    private FixedString32Bytes _name;
    private int _exp;
    private ulong _id;
    private FixedString32Bytes _creature;
    private FixedString32Bytes _move1;
    private FixedString32Bytes _move2;
    private FixedString32Bytes _move3;
    private FixedString32Bytes _move4;

    public PlayerData(FixedString32Bytes name, int exp, ulong id, FixedString32Bytes creature, params FixedString32Bytes[] moves)
    {
        _name = name;
        _exp = exp;
        _id = id;
        _creature = creature;
        _move1 = moves[0];
        _move2 = moves[1];
        _move3 = moves[2];
        _move4 = moves[3];
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _name);
        serializer.SerializeValue(ref _exp);
        serializer.SerializeValue(ref _id);
        serializer.SerializeValue(ref _creature);
        serializer.SerializeValue(ref _move1);
        serializer.SerializeValue(ref _move2);
        serializer.SerializeValue(ref _move3);
        serializer.SerializeValue(ref _move4);
    }

    public bool Equals(PlayerData other)
    {
        return Name.Equals(other.Name)
            && EXP == other.EXP
            && ID == other.ID
            && Creature.Equals(other.Creature)
            && Move1.Equals(other.Move1)
            && Move2.Equals(other.Move2)
            && Move3.Equals(other.Move3)
            && Move4.Equals(other.Move4);
    }
}