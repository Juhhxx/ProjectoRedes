using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EventSystemUtilities : MonoBehaviour
{
    private static EventSystem _eventSystem;

    private void Start()
    {
        _eventSystem = GetComponent<EventSystem>();
    }

    public static GameObject GetCurrentSelection()
    => _eventSystem.currentSelectedGameObject;

    public static void JumpToButton(GameObject button)
    => _eventSystem.SetSelectedGameObject(button);

    public static void ToggleNavigation(bool onoff)
    => _eventSystem.sendNavigationEvents = onoff;

    public static void QuitApplication()
    => Application.Quit();
}
;