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
        // §â·n±ì­ÈÁýµ¹ Starter Assets
        input.move = new Vector2(
            joystick.Horizontal,
            joystick.Vertical
        );
    }

    // ¥i¿ï¡G¸õÅD«ö¶s
    public void JumpPressed()
    {
        input.jump = true;
    }

    public void JumpReleased()
    {
        input.jump = false;
    }
}
