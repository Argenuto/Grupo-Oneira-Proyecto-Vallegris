using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class CustomHoldButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
{
    [InputControl(layout = "Button")]
    [SerializeField] private string m_ControlPath;
    
    private bool isHolding;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        SendValueToControl(0.0f); // Send the release signal
    }

    private void Update()
    {
        // Continuously fire the pressed value every single frame to prevent the joystick from overwriting it
        if (isHolding)
        {
            SendValueToControl(1.0f); 
        }
    }
}