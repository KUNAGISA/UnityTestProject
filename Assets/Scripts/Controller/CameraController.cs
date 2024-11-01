using Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour, IGameController, InputSystem_Actions.ICameraActions
{
    [SerializeField]
    private float m_moveSpeed = 5f;

    [SerializeField]
    private float m_rotationSpeed = 30f;

    private bool m_enableRotations = false;
    private Vector3 m_currentEulers = Vector3.zero;

    private int m_moveVerical = 0;
    private Vector2 m_inputDirection = Vector2.zero;
    private Vector2 m_currentDelta = Vector2.zero;

    private void Awake()
    {
        m_currentEulers = transform.localEulerAngles;
        this.GetSystem<IInputSystem>().Register(this)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnEnable()
    {
        this.GetSystem<IInputSystem>().CameraEnabled = true;
    }

    private void OnDisable()
    {
        this.GetSystem<IInputSystem>().CameraEnabled = false;
    }

    private void LateUpdate()
    {
        if (m_enableRotations)
        {
            m_currentEulers += Time.deltaTime * m_rotationSpeed * new Vector3(-m_currentDelta.y, m_currentDelta.x, 0f);
            transform.localRotation = Quaternion.Euler(m_currentEulers);
        }

        var moveDirection = (transform.forward * m_inputDirection.y + transform.right * m_inputDirection.x + Vector3.up * m_moveVerical).normalized;
        transform.position += m_moveSpeed * Time.deltaTime * moveDirection;
    }

    void InputSystem_Actions.ICameraActions.OnMove(InputAction.CallbackContext context)
    {
        m_inputDirection = context.ReadValue<Vector2>();
        if (m_inputDirection.sqrMagnitude <= (float.Epsilon * float.Epsilon))
        {
            m_inputDirection = Vector2.zero;
        }
    }

    void InputSystem_Actions.ICameraActions.OnLook(InputAction.CallbackContext context)
    {
        m_currentDelta = context.ReadValue<Vector2>();
    }

    void InputSystem_Actions.ICameraActions.OnUp(InputAction.CallbackContext context)
    {
        m_moveVerical = context.ReadValueAsButton() ? 1 : (m_moveVerical > 0 ? 0 : m_moveVerical);
    }

    void InputSystem_Actions.ICameraActions.OnDown(InputAction.CallbackContext context)
    {
        m_moveVerical = context.ReadValueAsButton() ? -1 : (m_moveVerical < 0 ? 0 : m_moveVerical);
    }

    void InputSystem_Actions.ICameraActions.OnRotation(InputAction.CallbackContext context)
    {
        m_enableRotations = context.ReadValueAsButton();
    }
}
