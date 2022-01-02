using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GamePadController : MonoBehaviour {
    [SerializeField] private GameObject buttonOverlay;
    [SerializeField] private RectTransform buttonOverlayTransform;
    [SerializeField] private GameObject drawButton;
    [SerializeField] private Image maskImage;
    private GameObject lastSelected = null;
    private RectTransform lastSelectedTransform = null;
    private bool isEnabled = true;
    private bool active = false;

    void Update() {
        if (!isEnabled) return;

        active = Input.GetJoystickNames().Length > 0;

        if (!active) return;

        if (EventSystem.current.currentSelectedGameObject == null) {
            EventSystem.current.SetSelectedGameObject(drawButton);
        }

        if (lastSelected != EventSystem.current.currentSelectedGameObject) {
            lastSelected = EventSystem.current.currentSelectedGameObject;
            MoveOverlay();
        }

        ShowButtonOverlay(active);
    }

    public void ToggleView() {
        isEnabled = !isEnabled;
        ShowButtonOverlay(isEnabled);
    }

    void ShowButtonOverlay(bool show) {
        if (buttonOverlay && buttonOverlay.activeSelf != show) {
            buttonOverlay.SetActive(show);
        }
    }

    void MoveOverlay() {
        lastSelectedTransform = lastSelected.GetComponent<RectTransform>();
        SetAndStretchToParentSize(buttonOverlayTransform, lastSelectedTransform);
    }

    void SetAndStretchToParentSize(RectTransform _mRect, RectTransform _parent) {
        _mRect.transform.SetParent(_parent);
        _mRect.anchoredPosition = _parent.position;
        _mRect.anchorMin = new Vector2(0, 0);
        _mRect.anchorMax = new Vector2(1, 1);
        _mRect.offsetMin = new Vector2(-3, -3);
        _mRect.offsetMax = new Vector2(3, 3);
    }
}
