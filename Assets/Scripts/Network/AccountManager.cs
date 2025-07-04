using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine.Events;

public class AccountManager : MonoBehaviour, IPlayerDependent
{
    private static PlayerController _controller;
    private static Player _player => _controller?.Player;
    public static AccountManager Instance { get; private set; }
    private static Dictionary<string, UserDataRecord> _userData;
    private static bool _isGettingData;
    private bool _isLoggedIn;
    public bool IsLoggedIn
    {
        get
        {
            if (!_isLoggedIn) OnNotLoggedInInteraction?.Invoke();
            
            return _isLoggedIn;
        }
    }
    public UnityEvent OnNotLoggedInInteraction;

    private void Awake()
    {
        Instance = this;
        _isLoggedIn = false;
    }

    public void CreateAccount(string username, string password, Action success = null, Action<string> fail = null)
    {
        PlayFabClientAPI.RegisterPlayFabUser(
            new RegisterPlayFabUserRequest()
            {
                Username = username,
                Password = password,

                RequireBothUsernameAndEmail = false,
            },
            response =>
            {
                Debug.Log($"Successfull Account Creation : {username}, {password}");

                SavePlayerData(
                    new Dictionary<string, string>()
                    {
                        { "Name" , username },
                        { "EXP" , _player.EXP.ToString() },
                    }
                );

                LogIntoAccount(username, password, success, fail);
            },
            error =>
            {
                Debug.Log($"Unsuccessfull Account Creation : {username}, {password}\n{error.ErrorMessage}");
                fail(error.ErrorMessage);
            }
        );
    }
    public void LogIntoAccount(string username, string password, Action success = null, Action<string> fail = null)
    {
        PlayFabClientAPI.LoginWithPlayFab(
            new LoginWithPlayFabRequest()
            {
                Username = username,
                Password = password,
            },
            response =>
            {
                Debug.Log($"Successful Account Login for {username}");
                _isLoggedIn = true;
                LoadPlayerData(success, fail);
            },
            error =>
            {
                Debug.Log($"Unsuccessful Account Login for {username}\n{error.ErrorMessage}");
                fail(error.ErrorMessage);
            }
        );
    }

    private void SaveData(Dictionary<string, string> data, Action<UpdateUserDataResult> onSuccess, Action<PlayFabError> onFail)
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = data,
        },
        result =>
        {
            if (_userData != null)
            {
                foreach (var key in data.Keys)
                {
                    UserDataRecord value = new UserDataRecord { Value = data[key] };

                    if (_userData.ContainsKey(key)) _userData[key] = value;
                    else _userData.Add(key, value);
                }
            }

            onSuccess(result);
        },
        onFail);
    }
    private void GetData(Action<GetUserDataResult> onSuccess, Action<PlayFabError> onFail)
    {
        while (_isGettingData) Task.Delay(100);

        if (_userData != null)
        {
            onSuccess(new GetUserDataResult() { Data = _userData });
            return;
        }

        _isGettingData = true;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result =>
        {
            _userData = result.Data;
            _isGettingData = false;
            onSuccess(result);
        },
        fail =>
        {
            _isGettingData = false;
            onFail(fail);
        });
    }

    public void SavePlayerData(Dictionary<string, string> saveValues)
    {
        SaveData(
            new Dictionary<string, string>(saveValues),
            onSuccess =>
            {
                Debug.Log("Updated Player Data");
                foreach (string key in saveValues.Keys)
                {
                    Debug.Log($"Updated {key}");
                }
            },
            onFail => Debug.Log("Error Updating Player Data")
        );
    }
    public void LoadPlayerData(Action success, Action<string> fail)
    {
        GetData(
            result =>
            {
                Debug.Log("Successfully Loaded Data");
                Dictionary<string, UserDataRecord> data = result.Data;

                Player player =
                _controller.Player.CreatePlayer(data["Name"].Value);

                _controller.SetUpPlayer(player);

                _controller.Player.SetEXP(
                    int.Parse(data["EXP"].Value)
                );

                if (data.ContainsKey("Creature"))
                {
                    _controller.Player.LoadCreature(
                        data["Creature"].Value,
                        data["Move1"].Value,
                        data["Move2"].Value,
                        data["Move3"].Value,
                        data["Move4"].Value
                    );
                }

                _controller.UpdatePlayer();
                success();
            },
            error =>
            {
                Debug.Log("Failed Loading Data");
                fail(error.ErrorMessage);
            }
        );
    }

    public void SetPlayer(PlayerController controller)
    {
        _controller = controller;
    }
}
