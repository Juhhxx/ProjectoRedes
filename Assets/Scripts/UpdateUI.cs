using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Reflection;
using System;

public class UpdateUI : MonoBehaviour
{
    [Header("Debug Values")]
    [Space(5f)]
    [OnValueChanged("SetUpBattleUI")][SerializeField] private Player _player;
    [OnValueChanged("SetUpBattleUI")][SerializeField] private Player _enemy;

    [Space(10f)]
    [Header("UI References")]
    [Space(5f)]
    [SerializeField] private EventSystem _eventSystem;

    [Space(10f)]
    [Header("Player UI References")]
    [Space(5f)]
    [SerializeField] private SpriteRenderer _playerSprite;
    [SerializeField] private Image _playerTypeSprite;
    [SerializeField] private Image _playerHPBar;
    [SerializeField] private TextMeshProUGUI _playerName;
    [SerializeField] private TextMeshProUGUI _playerHP;
    [SerializeField] private TextMeshProUGUI _playerOwner;
    [SerializeField] private GameObject _attacks;
    [SerializeField] private TextMeshProUGUI _attackInfo;
    [SerializeField] private Image _attackType;
    [SerializeField] private GameObject _stats;
    [SerializeField] private TextMeshProUGUI _playerStats;

    [Space(10f)]
    [Header("Enemy UI References")]
    [Space(5f)]
    [SerializeField] private SpriteRenderer _enemySprite;
    [SerializeField] private Image _enemyTypeSprite;
    [SerializeField] private Image _enemyHPBar;
    [SerializeField] private TextMeshProUGUI _enemyName;
    [SerializeField] private TextMeshProUGUI _enemyHP;
    [SerializeField] private TextMeshProUGUI _enemyOwner;

    private GameObject _lastSelected;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        SetUpBattleUI();
    }
    private void Update()
    {
        CheckButtonSelected();
        CheckCloseMenu();
    }

    public void SetUpBattleUI()
    {
        // Setup Player UI
        _playerSprite.sprite = _player?.Creature?.BackSprite;
        _playerTypeSprite.sprite = _player?.Creature?.Type?.Sprite;
        _playerName.text = _player?.Creature?.Name;
        _playerHP.text = $"{_player?.Creature?.HP} / {_player?.Creature?.HP}";
        _playerOwner.text = $"{_player?.Name} Lv. {_player?.Level}";
        _playerStats.text = $"ATK : {_player?.Creature?.Attack} DEF : {_player?.Creature?.Defense} SPD : {_player?.Creature?.Speed}";

        SetUpAttacks();

        // Setup Enemy UI
        _enemySprite.sprite = _enemy?.Creature?.FrontSprite;
        _enemyTypeSprite.sprite = _enemy?.Creature?.Type?.Sprite;
        _enemyName.text = _enemy?.Creature?.Name;
        _enemyHP.text = $"{_enemy?.Creature?.HP} / {_enemy?.Creature?.HP}";
        _enemyOwner.text = $"{_enemy?.Name} Lv. {_enemy?.Level}";
    }

    private void SetUpAttacks()
    {
        for (int i = 0; i < 4; i++)
        {
            Button atkButton = _attacks.transform.GetChild(0).GetChild(0).GetChild(i).GetComponent<Button>();

            // atkButton.onClick.AddListener(() => ShowAttackInfo(_player?.Creature?.Attacks[i]));

            atkButton.GetComponentInChildren<TextMeshProUGUI>().text = $"> {_player?.Creature?.Attacks[i]?.name}";
        }
    }
    private void ShowAttackInfo(Attack attack)
    {
        _attackInfo.text = $"{attack.CurrenPP}/{attack.PP}\n{attack.Accuracy}";
        _attackType.sprite = attack.Type.Sprite;
    }
    private void CheckButtonSelected()
    {
        if (_attacks.activeInHierarchy)
        {
            int idx = _eventSystem.currentSelectedGameObject.transform.GetSiblingIndex();
            ShowAttackInfo(_player.Creature.Attacks[idx]);
        }
    }
    private void CheckCloseMenu()
    {
        if (_attacks.activeInHierarchy || _stats.activeInHierarchy)
        {
            if (Input.GetButtonDown("Quit"))
            {
                _attacks.SetActive(false);
                _stats.SetActive(false);
                _eventSystem.SetSelectedGameObject(_lastSelected);
            }
        }
        else
        {
            _lastSelected = _eventSystem.currentSelectedGameObject;
        }
    }

    public void UpdateHPBars()
    {
        _playerHPBar.fillAmount = _player.Creature.CurrentHP / _player.Creature.HP;
        _enemyHPBar.fillAmount = _enemy.Creature.CurrentHP / _enemy.Creature.HP;
    }
    public void JumpToButton(Selectable button)
    {
        _eventSystem.SetSelectedGameObject(button.gameObject);
    }
}
