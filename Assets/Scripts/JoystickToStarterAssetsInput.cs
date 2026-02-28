using UnityEngine;
using StarterAssets;

public class JoystickToStarterAssetsInput : MonoBehaviour
{
    public VariableJoystick joystick;
    private StarterAssetsInputs input;

    private void Awake()
    {
        input = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        Vector2 joystickInput = new Vector2(
            joystick.Horizontal,
            joystick.Vertical
        );

        // 只有搖桿真的有推動時才覆蓋
        if (joystickInput.magnitude > 0.1f)
        {
            input.move = joystickInput;
        }
    }

    // 可選：跳躍按鈕
    public void JumpPressed()
    {
        input.jump = true;
    }

    public void JumpReleased()
    {
        input.jump = false;
    }
}
