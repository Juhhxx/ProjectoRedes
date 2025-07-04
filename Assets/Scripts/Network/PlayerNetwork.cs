using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<PlayerData> _player = new NetworkVariable<PlayerData>(default,
                        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public PlayerData Player => _player.Value;
    private PlayerController _ctrl;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _ctrl = FindAnyObjectByType<PlayerController>();
        SetPlayer();
        Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
    }

    public void SetPlayer()
    {
        if (!IsOwner) return;

        Player p = _ctrl.Player;
        Debug.Log($"Player : {p.Name}");

        string[] moves = p.Creature.CurrentAttackSet.ConvertAll(
                                                    (m) => m.name
                                                    .Replace("(Clone)", ""))
                                                    .ToArray();

        FixedString32Bytes[] movesF = new FixedString32Bytes[4];

        for (int i = 0; i < moves.Length; i++) movesF[i] = moves[i];

        ulong id = NetworkManager.Singleton.LocalClientId;

        _player.Value = new PlayerData(p.Name, p.EXP, id, p.Creature.Name, movesF);

        Debug.Log(p.Name);
        Debug.Log(p.EXP);
        Debug.Log(moves[0]);
    }
}
