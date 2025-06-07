using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NaughtyAttributes;
using TMPro;
using System.Collections.Generic;


public class CreatureEditor : MonoBehaviour, IPlayerDependent
{
    private PlayerController _controller;
    private Player _player => _controller?.Player;
    [SerializeField] private GameObject _selectionMenu;

    [Space(10f)]
    [Header("Showcase")]
    [Space(5f)]
    [SerializeField] private Image _creatureSprite;
    [SerializeField] private TextMeshProUGUI _creatureName;
    
    [Space(10f)]
    [Header("Creature Selection")]
    [Space(5f)]
    [SerializeField] private GameObject _creatureSelectionMenu;
    [SerializeField] private List<Creature> _creatures;
    [SerializeField] private TextMeshProUGUI _infoText1;
    [SerializeField] private TextMeshProUGUI _infoText2;
    [SerializeField] private Image _typeSprite;
    [SerializeField] private GameObject _creatureButtons;
    [SerializeField] private Button _nextButton;

    private Creature _selectedCreature;

    [Space(10f)]
    [Header("Creature Selection")]
    [Space(5f)]
    [SerializeField] private GameObject _moveSelectionMenu;
    [SerializeField] private TextMeshProUGUI _tittle;
    [SerializeField] private GameObject _moveButtonPrefab;
    [SerializeField] private ScrollRect _moveScrollRect;
    [SerializeField] private RectTransform _movesContentRect;
    [SerializeField] private TextMeshProUGUI _moveInfo;
    [SerializeField] private TextMeshProUGUI _moveDescription;
    [SerializeField] private Image _typeInfo;
    [SerializeField] private Sprite _unselectedMoveSprite;
    [SerializeField] private Sprite _selectedMoveSprite;
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _doneButton;

    private List<Button> _moveButonList;
    private List<Attack> _moveList;
    private List<Attack> _selectedMoves;

    private void Start()
    {
        UpdateShowcase();

        _moveButonList = new List<Button>();
        _moveList = new List<Attack>();
        _selectedMoves = new List<Attack>();

        _nextButton.onClick.AddListener(() => SetUpMovesMenu());
        _backButton.onClick.AddListener(() => SetUpCreaturesMenu());
        _backButton.onClick.AddListener(() => DeleteButtons());
        _doneButton.onClick.AddListener(() => Finish());
        _doneButton.onClick.AddListener(() => DeleteButtons());
    }
    private void Update()
    {
        CheckButtonSelected();
    }

    public void UpdateShowcase()
    {
        if (_player?.Creature == null)
        {
            _creatureName.transform.parent.gameObject.SetActive(false);
            return;
        }

        _creatureName.transform.parent.gameObject.SetActive(true);
        _creatureSprite.sprite = _player.Creature.FrontSprite;
        _creatureName.text = _player.Creature.Name;
    }

    private void SetUpCreaturesMenu()
    {
        if (!AccountManager.Instance.IsLoggedIn) return;

        _selectionMenu.SetActive(true);

        EventSystemUtilities.JumpToButton(_creatureButtons.transform
                                    .GetChild(GetFirstSelection()).gameObject);

        SetUpCreatureButtons();

        _creatureSelectionMenu.SetActive(true);
        _moveSelectionMenu.SetActive(false);
    }
    private int GetFirstSelection()
    {
        if (_player.Creature != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_creatures[i].Name == _player.Creature.Name)
                {
                    _creatures[i] = _player.Creature;
                    return i;
                }
            }
        }

        return 0;
    }
    private void SetUpCreatureButtons()
    {
        for (int i = 0; i < 3; i++)
        {
            _creatures[i] = _creatures[i].CreateCreature(_player);

            Transform btt = _creatureButtons.transform.GetChild(i);

            btt.GetComponentInChildren<TextMeshProUGUI>().text = _creatures[i].Name;
            btt.GetChild(1).GetComponent<Image>().sprite = _creatures[i].FrontSprite;
        }
    }
    private void ShowCreatureInfo(Creature creature)
    {
        _selectedCreature = creature;
        _infoText1.text = $"HP : {creature.HP}\nATK : {creature.Attack}";
        _infoText2.text = $"DEF : {creature.Defense}\nSPD : {creature.Speed}";
        _typeSprite.sprite = creature.Type.Sprite;
    }

    private void SetUpMovesMenu()
    {
        _tittle.text = $"Choose a MOVE SET for {_selectedCreature.Name}";

        SetUpMoveButtons(_selectedCreature);
        EventSystemUtilities.JumpToButton(_moveButonList[0].gameObject);

        _creatureSelectionMenu.SetActive(false);
        _moveSelectionMenu.SetActive(true);
    }
    private void SetUpMoveButtons(Creature creature)
    {
        foreach (Attack a in creature.Attacks)
        {
            Button newBtt = Instantiate(_moveButtonPrefab, _movesContentRect.transform)
                                .GetComponent<Button>();

            newBtt.GetComponentInChildren<TextMeshProUGUI>().text = a.Name;
            newBtt.gameObject.name = a.Name;
            newBtt.onClick.AddListener(() => AddMove(a));

            _moveButonList.Add(newBtt);
            _moveList.Add(a);
        }
        for (int i = 3; i >= 0; i--)
        {
            AddMove(_selectedCreature.Attacks[i]);
        }
    }
    private void ShowMoveInfo(Attack attack)
    {
        _moveInfo.text = $"PWR : {attack.Power}\nACC : {attack.Accuracy}\nPP : {attack.PP}";
        _moveDescription.text = attack.Description;
        _typeInfo.sprite = attack.Type.Sprite;
    }
    private void AddMove(Attack attack)
    {
        int idx = _moveList.IndexOf(attack);

        if (_selectedMoves.Contains(attack))
        {
            _moveButonList[idx].GetComponent<Image>().sprite = _selectedMoveSprite;
            _moveButonList[idx].transform.SetSiblingIndex(0);
            MoveAttackToStart(idx, 0);
            DefineNavigation();

            _selectedMoves.Remove(attack);
            _selectedMoves.Insert(0, attack);
            return;
        }

        if (_selectedMoves.Count == 4)
        {
            RemoveMove(_moveList[3]);
        }
        
        Debug.Log(idx);
        Debug.Log(_moveList.Count);

        _selectedMoves.Add(attack);

        _moveButonList[idx].GetComponent<Image>().sprite = _selectedMoveSprite;
        _moveButonList[idx].transform.SetSiblingIndex(0);
        MoveAttackToStart(idx, 0);
        DefineNavigation();
        
        foreach (Attack a in _selectedCreature.CurrentAttackSet)
        {
            Debug.Log($"Creature has {a.Name} move");
        }
    }
    private void RemoveMove(Attack attack)
    {
        int idx = _moveList.IndexOf(attack);

        _selectedMoves.Remove(attack);

        _moveButonList[idx].GetComponent<Image>().sprite = _unselectedMoveSprite;
        MoveAttackToStart(idx, 3);
        DefineNavigation();
    }
    private void MoveAttackToStart(int oldIdx, int newIdx)
    {
        Attack tmp = _moveList[oldIdx];
        Button tmpB = _moveButonList[oldIdx];

        _moveList.RemoveAt(oldIdx);
        _moveList.Insert(newIdx, tmp);

        _moveButonList.RemoveAt(oldIdx);
        _moveButonList.Insert(newIdx, tmpB);
    }

    private void Finish()
    {
        _selectedMoves.Reverse();

        foreach (Attack a in _selectedMoves)
        {
            _selectedCreature.AddAttack(a);
        }

        _player.SetCreature(_selectedCreature);

        UpdateShowcase();

        AccountManager.Instance.SavePlayerData(
                new Dictionary<string, string>()
                {
                    { "Creature" , _player.Creature?.Name },
                    { "Move1" , _player.Creature?.CurrentAttackSet[0].name.Replace("(Clone)", "") },
                    { "Move2" , _player.Creature?.CurrentAttackSet[1].name.Replace("(Clone)", "") },
                    { "Move3" , _player.Creature?.CurrentAttackSet[2].name.Replace("(Clone)", "") },
                    { "Move4" , _player.Creature?.CurrentAttackSet[3].name.Replace("(Clone)", "") }
                }
            );

        _moveSelectionMenu.SetActive(false);
        _selectionMenu.SetActive(false);
    }

    private void DefineNavigation()
    {
        Navigation customNav;

        for (int i = 0; i < _moveButonList.Count; i++)
        {
            customNav = new Navigation();
            customNav.mode = Navigation.Mode.Explicit;

            string m = "Navigation : ";

            if (i > 0)
            {
                customNav.selectOnUp = _moveButonList[i - 1];
                m += $"{_moveButonList[i - 1].name} => ";
            }

            m += $"{_moveButonList[i].name}";

            if (i < _moveButonList.Count - 1)
            {
                customNav.selectOnDown = _moveButonList[i + 1];
                m += $" => {_moveButonList[i + 1].name}";
            }

            customNav.selectOnRight = _backButton;

            _moveButonList[i].navigation = customNav;
            Debug.Log(m);
        }

        customNav = new Navigation();
        customNav.mode = Navigation.Mode.Explicit;

        customNav.selectOnUp = _moveButonList[0];
        customNav.selectOnRight = _doneButton;
        customNav.selectOnLeft = _doneButton;

        _backButton.navigation = customNav;

        customNav.selectOnRight = _backButton;
        customNav.selectOnLeft = _backButton;

        _doneButton.navigation = customNav;
    }
    private void CheckButtonSelected()
    {
        Debug.Log(EventSystemUtilities.GetCurrentSelection()?.name);

        if (_creatureSelectionMenu.activeInHierarchy && EventSystemUtilities.GetCurrentSelection()?.name != "Next")
        {
            int idx = EventSystemUtilities.GetCurrentSelection().transform.GetSiblingIndex();
            ShowCreatureInfo(_creatures[idx]);
        }
        else if (_moveSelectionMenu.activeInHierarchy && EventSystemUtilities.GetCurrentSelection()?.name != "Done")
        {
            int idx = EventSystemUtilities.GetCurrentSelection().transform.GetSiblingIndex();
            ShowMoveInfo(_moveList[idx]);
        }
    }
    private void DeleteButtons()
    {
        foreach (Button b in _moveButonList)
        {
            Destroy(b.gameObject);
        }

        _moveButonList.Clear();
        _moveList.Clear();
        _selectedMoves.Clear();
    }

    public void SetPlayer(PlayerController controller)
    {
        _controller = controller;
    }
}
