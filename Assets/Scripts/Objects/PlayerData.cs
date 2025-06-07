using System;
using Unity.Netcode;

public struct PlayerData : INetworkSerializable
{
    public string Name => _name;
    public int EXP => _exp;
    public string Creature => _creature;
    public string Move1 => _move1;
    public string Move2 => _move2;
    public string Move3 => _move3;
    public string Move4 => _move4;
    public string _name;
    public int _exp;
    public string _creature;
    public string _move1;
    public string _move2;
    public string _move3;
    public string _move4;

    public PlayerData(string name, int exp, string creature, params string[] moves)
    {
        _name = name;
        _exp = exp;
        _creature = creature;
        _move1 = moves[0];
        _move2 = moves[1];
        _move3 = moves[2];
        _move4 = moves[3];
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        _name ??= "";
        _creature ??= "";
        _move1 ??= "";
        _move2 ??= "";
        _move3 ??= "";
        _move4 ??= "";
        
        serializer.SerializeValue(ref _name);
        serializer.SerializeValue(ref _exp);
        serializer.SerializeValue(ref _creature);
        serializer.SerializeValue(ref _move1);
        serializer.SerializeValue(ref _move2);
        serializer.SerializeValue(ref _move3);
        serializer.SerializeValue(ref _move4);
    }
}