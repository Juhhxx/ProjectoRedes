using UnityEngine;

public class LoadingScreenActivator : MonoBehaviour
{
    [SerializeField] private GameObject _loadingScreen;

    public static LoadingScreenActivator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void ToogleScreen(bool onoff)
    {
        _loadingScreen.SetActive(onoff);
        EventSystemUtilities.ToggleNavigation(!onoff);
    }
}
