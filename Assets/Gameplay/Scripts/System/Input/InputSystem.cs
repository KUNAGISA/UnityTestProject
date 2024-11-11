using Framework;

namespace Gameplay
{
    public interface IInputSystem : ISystem
    {
        bool CameraEnabled { get; set; }
        bool PlayerEnabled { get; set; }
        bool UIEnabled { get; set; }

        IUnRegister Register(InputSystem_Actions.IPlayerActions actions);
        void UnRegister(InputSystem_Actions.IPlayerActions actions);

        IUnRegister Register(InputSystem_Actions.ICameraActions actions);
        void UnRegister(InputSystem_Actions.ICameraActions actions);
    }

    internal class InputSystem : AbstractSystem, IInputSystem, IUnRegisterable<InputSystem_Actions.IPlayerActions>, IUnRegisterable<InputSystem_Actions.ICameraActions>
    {
        private InputSystem_Actions m_inputActions = new InputSystem_Actions();

        public bool PlayerEnabled
        {
            get => m_inputActions.Player.enabled;
            set
            {
                if (value == m_inputActions.Player.enabled)
                {
                    return;
                }
                if (value)
                {
                    m_inputActions.Player.Enable();
                }
                else
                {
                    m_inputActions.Player.Disable();
                }
            }
        }

        public bool UIEnabled
        {
            get => m_inputActions.UI.enabled;
            set
            {
                if (value == m_inputActions.UI.enabled)
                {
                    return;
                }
                if (value)
                {
                    m_inputActions.UI.Enable();
                }
                else
                {
                    m_inputActions.UI.Disable();
                }
            }
        }

        public bool CameraEnabled
        {
            get => m_inputActions.Camera.enabled;
            set
            {
                if (value == m_inputActions.Camera.enabled)
                {
                    return;
                }
                if (value)
                {
                    m_inputActions.Camera.Enable();
                }
                else
                {
                    m_inputActions.Camera.Disable();
                }
            }
        }

        protected override void OnInit()
        {
            m_inputActions.Enable();

            CameraEnabled = false;
            PlayerEnabled = false;
            UIEnabled = true;
        }

        protected override void OnDestroy()
        {
            m_inputActions.Dispose();
            m_inputActions = null;
        }

        public IUnRegister Register(InputSystem_Actions.IPlayerActions actions)
        {
            m_inputActions.Player.AddCallbacks(actions);
            return new UnRegister<InputSystem_Actions.IPlayerActions>(this, actions);
        }

        public void UnRegister(InputSystem_Actions.IPlayerActions actions)
        {
            m_inputActions.Player.RemoveCallbacks(actions);
        }

        public IUnRegister Register(InputSystem_Actions.ICameraActions actions)
        {
            m_inputActions.Camera.AddCallbacks(actions);
            return new UnRegister<InputSystem_Actions.ICameraActions>(this, actions);
        }

        public void UnRegister(InputSystem_Actions.ICameraActions actions)
        {
            m_inputActions.Camera.RemoveCallbacks(actions);
        }
    }
}