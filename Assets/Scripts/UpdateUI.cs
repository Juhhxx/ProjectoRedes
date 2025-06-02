using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Reflection;
using System;

public class UpdateUI : MonoBehaviour
{
    private Player _player, _enemy;

    [Space(10f)]
    [Header("UI References")]
    [Space(5f)]
    [SerializeField] private EventSystem _eventSystem;
    [SerializeField] private DialogueManager _dialogueManager;

    [Space(10f)]
    [Header("Player UI References")]
    [Space(5f)]
    [SerializeField] private SpriteRenderer _playerSprite;
    [SerializeField] private SpriteMask _playerSpriteMask;
    [SerializeField] private Image _playerTypeSprite;
    [SerializeField] private Image _playerHPBar;
    [SerializeField] private TextMeshProUGUI _playerName;
    [SerializeField] private TextMeshProUGUI _playerHP;
    [SerializeField] private TextMeshProUGUI _playerOwner;
    [SerializeField] private RectTransform _battleText;
    [SerializeField] private GameObject _actions;
    [SerializeField] private GameObject _attacks;
    [SerializeField] private TextMeshProUGUI _attackInfo;
    [SerializeField] private Image _attackType;
    [SerializeField] private GameObject _stats;
    [SerializeField] private TextMeshProUGUI _playerStats;
    [SerializeField] private Animator _playerAnim;

    [Space(10f)]
    [Header("Enemy UI References")]
    [Space(5f)]
    [SerializeField] private SpriteRenderer _enemySprite;
    [SerializeField] private SpriteMask _enemySpriteMask;
    [SerializeField] private Image _enemyTypeSprite;
    [SerializeField] private Image _enemyHPBar;
    [SerializeField] private TextMeshProUGUI _enemyName;
    [SerializeField] private TextMeshProUGUI _enemyHP;
    [SerializeField] private TextMeshProUGUI _enemyOwner;
    [SerializeField] private Animator _enemyAnim;

    [Space(10f)]
    [Header("HP Bars References")]
    [Space(5f)]
    [SerializeField] private Color _fullHPColor;
    [SerializeField] private Color _halfHPColor;
    [SerializeField] private Color _lowHPColor;

    // [Space(10f)]
    // [Header("Animation References")]
    // [Space(5f)]
    // [SerializeField] private Animator _battleMenuAnimator;

    private GameObject _lastSelected;
    private BattleManager _battleManager;

    private void Update()
    {
        CheckButtonSelected();
        CheckCloseMenu();
    }

    public void SetUpUI(BattleManager manager)
    {
        Debug.Log("UI CONFIGURED");
        Cursor.lockState = CursorLockMode.Locked;

        _battleManager = manager;

        _player = _battleManager.P1;
        _enemy = _battleManager.P2;

        Debug.Log($"PLAYER : {_player.Creature.Name}");
        Debug.Log($"ENEMY : {_enemy.Creature.Name}");

        SetUpBattleUI();
    }
    private void SetUpBattleUI()
    {
        // Setup Player UI
        _playerSprite.sprite = _player?.Creature?.BackSprite;
        _playerSpriteMask.sprite = _player?.Creature?.BackSprite;
        _playerTypeSprite.sprite = _player?.Creature?.Type?.Sprite;
        _playerName.text = _player?.Creature?.Name;
        _playerHPBar.color = _fullHPColor;
        _playerHP.text = $"{_player?.Creature?.HP} / {_player?.Creature?.HP}";
        _playerOwner.text = $"{_player?.Name} Lv. {_player?.Level}";
        _playerStats.text = $"ATK : {_player?.Creature?.Attack} DEF : {_player?.Creature?.Defense} SPD : {_player?.Creature?.Speed}";
        _player.Creature.OnDamageTaken += () => UpdateHPBars();
        _player.Creature.SetAnimator(_playerAnim);
        _player.Creature.SetDialogueManager(_dialogueManager);

        SetUpAttacks();

        // Setup Enemy UI
        _enemySprite.sprite = _enemy?.Creature?.FrontSprite;
        _enemySpriteMask.sprite = _enemy?.Creature?.FrontSprite;
        _enemyTypeSprite.sprite = _enemy?.Creature?.Type?.Sprite;
        _enemyName.text = _enemy?.Creature?.Name;
        _enemyHPBar.color = _fullHPColor;
        _enemyHP.text = $"{_enemy?.Creature?.HP} / {_enemy?.Creature?.HP}";
        _enemyOwner.text = $"{_enemy?.Name} Lv. {_enemy?.Level}";
        _enemy.Creature.OnDamageTaken += () => UpdateHPBars();
        _enemy.Creature.SetAnimator(_enemyAnim);
        _enemy.Creature.SetDialogueManager(_dialogueManager);

        // Set up text

        _lastSelected = _actions.transform.GetChild(1).GetChild(0).gameObject;
        if (Application.isPlaying) SetUpActionScene();
    }
    private void UpdateBattleUI()
    {
        _playerStats.text = $"ATK : {_player?.Creature?.Attack} DEF : {_player?.Creature?.Defense} SPD : {_player?.Creature?.Speed}";
    }
    private void SetUpAttacks()
    {
        for (int i = 0; i < 4; i++)
        {
            Button atkButton = _attacks.transform.GetChild(0)
                                                 .GetChild(0)
                                                 .GetChild(i)
                                                 .GetComponent<Button>();

            Attack attack = _player.Creature.CurrentAttackSet[i];

            Debug.LogWarning($"{attack.Name} was Regsitred for {attack.Attacker.Name}");

            atkButton.onClick.AddListener(() => SetUpBattleScene());
            atkButton.onClick.AddListener(() => _battleManager?.RegisterAction(attack));

            atkButton.GetComponentInChildren<TextMeshProUGUI>().text = $"> {attack.Name}";
        }
    }
    private void ShowAttackInfo(Attack attack)
    {
        _attackInfo.text = $"{attack.Power}\n{attack.Accuracy}";
        _attackType.sprite = attack.Type.Sprite;
    }
    private void CheckButtonSelected()
    {
        if (_attacks.activeInHierarchy)
        {
            int idx = _eventSystem.currentSelectedGameObject.transform.GetSiblingIndex();
            ShowAttackInfo(_player.Creature.CurrentAttackSet[idx]);
        }
        else if (_actions.activeInHierarchy)
        {
            _lastSelected = _eventSystem.currentSelectedGameObject;
        }
    }
    private void CheckCloseMenu()
    {
        if (_attacks.activeInHierarchy || _stats.activeInHierarchy)
        {
            if (Input.GetButtonDown("Quit"))
            {
                _eventSystem.sendNavigationEvents = true;

                if (_attacks.activeInHierarchy) _attacks.SetActive(false);
                else _stats.SetActive(false);

                _eventSystem.SetSelectedGameObject(_lastSelected);
            }
        }
    }

    public void SetUpBattleScene()
    {
        _actions.SetActive(false);
        _attacks.SetActive(false);
        _battleText.sizeDelta = new Vector2(500f, _battleText.rect.height);
    }
    public void SetUpActionScene()
    {
        Debug.Log(_lastSelected.name);
        _actions.SetActive(true);
        UpdateBattleUI();
        _eventSystem.SetSelectedGameObject(_lastSelected);
        _battleText.sizeDelta = new Vector2(360f, _battleText.rect.height);
        _dialogueManager.StartDialogues($"What will {_player?.Creature?.Name} do?");
    }
    public void UpdateHPBars()
    {
        _playerHPBar.fillAmount = _player.Creature.CurrentHP / _player.Creature.HP;
        _playerHP.text = $"{_player.Creature.CurrentHP:f0} / {_player.Creature.HP:f0}";

        if (_playerHPBar.fillAmount <= 0.25f) _playerHPBar.color = _lowHPColor;
        else if (_playerHPBar.fillAmount <= 0.5f) _playerHPBar.color = _halfHPColor;

        _enemyHPBar.fillAmount = _enemy.Creature.CurrentHP / _enemy.Creature.HP;
        _enemyHP.text = $"{_enemy.Creature.CurrentHP:f0} / {_enemy.Creature.HP:f0}";

        if (_enemyHPBar.fillAmount <= 0.25f) _enemyHPBar.color = _lowHPColor;
        else if (_enemyHPBar.fillAmount <= 0.5f) _enemyHPBar.color = _halfHPColor;
    }
    public void JumpToButton(Selectable button)
    {
        _eventSystem.SetSelectedGameObject(button.gameObject);
    }
}
