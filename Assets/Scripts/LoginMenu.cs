using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginMenu : MonoBehaviour, IPlayerDependent
{
    [SerializeField] private GameObject _loginMenu;
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private Sprite _buttonhShow;
    [SerializeField] private Sprite _buttonhHide;
    [SerializeField] private Image _showHideButton;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TMP_InputField _inputFieldUsername;
    [SerializeField] private TMP_InputField _inputFieldPassword;
    [SerializeField] private TextMeshProUGUI _errorText;
    [SerializeField] private float _errorFadeTime;

    private YieldInstruction _wfs;
    private Coroutine _showError;
    private PlayerController _controller;
    private Player _player => _controller?.Player;

    private void Start()
    {
        _wfs = new WaitForSeconds(_errorFadeTime);
    }

    public void StartCreateAccount()
    {
        _mainMenu.SetActive(false);
        gameObject.SetActive(true);
        _titleText.text = "Create Account";
        _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "Create";
        _confirmButton.onClick.AddListener(() => SubmitInfo(true));
        EventSystemUtilities.JumpToButton(_inputFieldUsername.gameObject);
    }
    public void StartLogin()
    {
        _mainMenu.SetActive(false);
        gameObject.SetActive(true);
        _titleText.text = "Login";
        _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "Confirm";
        _confirmButton.onClick.AddListener(() => SubmitInfo(false));
        EventSystemUtilities.JumpToButton(_inputFieldUsername.gameObject);
    }

    public void ShowHidePassword()
    {
        if (_showHideButton.sprite == _buttonhShow)
        {
            _showHideButton.sprite = _buttonhHide;
            _inputFieldPassword.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            _showHideButton.sprite = _buttonhShow;
            _inputFieldPassword.contentType = TMP_InputField.ContentType.Password;
        }

        _inputFieldPassword.ForceLabelUpdate();
    }
    private void SubmitInfo(bool createAccount)
    {
        string errorMsg = "";

        string username = _inputFieldUsername.text;
        string password = _inputFieldPassword.text;

        if (_inputFieldUsername.text == "") errorMsg += "Username";
        if (_inputFieldPassword.text == "")
        {
            if (errorMsg != "") errorMsg += " and ";

            errorMsg += "Password";
        }

        if (errorMsg != "")
        {
            errorMsg += " missing!";

            ErrorMessage(errorMsg);

            return;
        }
        else if (password.Length < 6)
        {
            errorMsg = "Password too Short !\n(Has to be 6 or more character long)";

            ErrorMessage(errorMsg);

            return;
        }
        else if (username.Length < 3)
        {
            errorMsg = "Username too Short !\n(Has to be 3 or more character long)";

            ErrorMessage(errorMsg);

            return;
        }

        if (createAccount)
        {
            AccountManager.Instance.CreateAccount(
                username,
                password,
                () =>
                {
                    gameObject.SetActive(false);
                    _mainMenu.SetActive(true);
                    _loginMenu.SetActive(false);
                },
                (e) => ErrorMessage(errorMsg)
            );
        }
        else
        {
            AccountManager.Instance.LogIntoAccount(
                username,
                password,
                () =>
                {
                    gameObject.SetActive(false);
                    _mainMenu.SetActive(true);
                    _loginMenu.SetActive(false);
                },
                (e) => ErrorMessage(errorMsg)
            );
        }

        Debug.Log($"SUCCESSFUL SUBMITION");
    }

    private void ErrorMessage(string message)
    {
        if (_showError != null) StopCoroutine(_showError);
        _showError = StartCoroutine(ShowErrorMessage(message));
    }
    private IEnumerator ShowErrorMessage(string message)
    {
        _errorText.text = message;

        Color newColor = _errorText.color;
        newColor.a = 1.0f;

        while (newColor.a > 0f)
        {
            newColor.a -= 0.1f;

            _errorText.color = newColor;

            yield return _wfs;
        }
    }

    public void SetPlayer(PlayerController controller)
    {
        _controller = controller;
    }
}
