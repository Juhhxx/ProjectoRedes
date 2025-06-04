using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// using PlayFab;
// using PlayFab.ClientModels;

public class LoginMenu : MonoBehaviour, IPlayerDependent
{
    [SerializeField] private Sprite _buttonhShow;
    [SerializeField] private Sprite _buttonhHide;
    [SerializeField] private Image _showHideButton;
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
    public void Sumbit(bool createAccount)
    {
        string errorMsg = "";

        if (_inputFieldUsername.text == "") errorMsg += "Username";
        if (_inputFieldPassword.text == "")
        {
            if (errorMsg != "") errorMsg += " and ";

            errorMsg += "Password";
        }

        if (errorMsg != "")
        {
            errorMsg += " missing!";

            if (_showError != null) StopCoroutine(_showError);
            _showError = StartCoroutine(ShowErrorMessage(errorMsg));

            return;
        }

        Debug.Log($"SUCCESSFUL SUBMITION");
    }

    public void CreateAccount(string username, string password)
    {
        // PlayFab.PlayFabClientAPI.RegisterPlayFabUser(
        //     new RegisterPlayfabUserRequest()
        //     {
        //         Username = username,
        //         Password = password,
        //     },
        //     response =>
        //     {
        //         Debug.Log($"Successfull Account Creation : {username}, {password}");
        //     },
        //     error =>
        //     {
        //         Debug.Log($"Unsuccessfull Account Creation : {username}, {password}\n{error.ErrorMessage}");
        //     }
        // );
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
