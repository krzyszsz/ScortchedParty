using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    private Rigidbody _rigidBody;
    private bool _jumpRequest;
    private float _horizontalInput;
    private float _verticalInput;
    private Camera _mainCamera;
    private float yRotation = 0f;

    [SerializeField] private Transform groundCheckTransform = null;
    [SerializeField] private Transform playerBody = null;
    [SerializeField] private Transform weapon = null;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float MouseSensitivity = 300.0f;

    // Start is called before the first frame update
    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _mainCamera = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Jump") > 0)
        {
            if (Physics.OverlapSphere(groundCheckTransform.position, 0.5f, playerMask).Length == 1)
            {
                //Debug.Log(Input.GetAxis("Jump"));
                _jumpRequest = true;
            }
        }

        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");
        MouseAim();
        if (Input.GetKeyDown(KeyCode.Escape)) // TODO: Menu and when exiting menu, re-lock cursor again
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void FixedUpdate()
    {
        if (_jumpRequest)
        {
            _rigidBody.AddForce(Vector3.up * 5, ForceMode.VelocityChange);
        }
        _rigidBody.velocity = transform.rotation * new Vector3(_verticalInput * 3, _rigidBody.velocity.y, _horizontalInput * 3);
        _jumpRequest = false;
    }

    private void MouseAim()
    {
        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * Time.deltaTime;

        yRotation += mouseY;
        yRotation = Mathf.Clamp(yRotation, -90f, +90f);

        weapon.localRotation = Quaternion.Euler(0f, 0f, yRotation);
        _mainCamera.transform.localRotation = Quaternion.Euler(-yRotation, 90f, 0f); // not sure why the axes are messed up (y,z,x?); maybe there is some other transformation applied

        // I can't see a way to rotate around end of the weapon, so after we rotate around the middle, we need to translate it:
        var lengthOfWeapon = 1f;
        var yRotationInRads = yRotation / 57.296f;
        weapon.localPosition = new Vector3(0.7f - 0.5f * lengthOfWeapon * (1 - Mathf.Cos(yRotationInRads)), 0.3f + Mathf.Sin(yRotationInRads) * 0.5f * lengthOfWeapon, 0f);

        playerBody.Rotate(Vector3.up * mouseX);
    }
}
