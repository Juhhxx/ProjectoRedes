using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "Type", menuName = "Scriptable Objects/Type")]
public class Type : ScriptableObject
{
    [SerializeField][ShowAssetPreview] private Sprite _sprite;
    public Sprite Sprite => _sprite;
    [field: SerializeField] public string Name;
    [field: SerializeField] public Type[] StrongAgainst;
    [field: SerializeField] public Type[] WeakAgainst;
}
