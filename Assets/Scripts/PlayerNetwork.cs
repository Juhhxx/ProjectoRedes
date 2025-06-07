using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField][ReadOnly] private string _playerName;
    private NetworkVariable<PlayerData> _player = new NetworkVariable<PlayerData>(default,
                        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public PlayerData Player => _player.Value;
    private PlayerController _ctrl;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _ctrl = FindAnyObjectByType<PlayerController>();
        Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        SetPlayer();
    }

    private void SetPlayer()
    {
        if (!IsOwner) return;

        Player p = _ctrl.Player;
        Debug.Log($"Player : {p.Name}");

        string[] moves = p.Creature.CurrentAttackSet.ConvertAll(
                                                    (m) => m.name
                                                    .Replace("(Clone)", ""))
                                                    .ToArray();

        _player.Value = new PlayerData(p.Name, p.EXP, p.Creature.Name, moves);

        Debug.Log(p.Name);
        Debug.Log(p.EXP);
        Debug.Log(moves[0]);

        _playerName = p.Name;
    }
}
