using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcDialog : MonoBehaviour
{
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject ToActivate;

    [SerializeField] private Transform standingPoint;

    private Transform avatar;

    private async void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            avatar = other.transform;
            // disable main cam, enable dialog cam
            mainCamera.SetActive(false);
            ToActivate.SetActive(true);

            // disable player input
            avatar.GetComponent<PlayerInput>().enabled = false;

            await Task.Delay(50);

            // teleport the avatar to standing point

            avatar.position = standingPoint.position;
            avatar.rotation = standingPoint.rotation;



            // display cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // recover
    public void Recover()
    {
        // disable player input
        avatar.GetComponent<PlayerInput>().enabled = true;


        // disable main cam, enable dialog cam
        mainCamera.SetActive(true);
        ToActivate.SetActive(false);


        // display cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
