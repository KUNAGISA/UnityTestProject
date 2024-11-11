using Framework;

namespace Gameplay
{
    internal interface IGameController : ICanGetModel, ICanGetSystem, ICanGetUtility, ICanRegisterEvent, ICanSendCommand, ICanSendQuery, ICanSendEvent
    {
        IArchitecture IBelongArchitecture.GetArchitecture() => GameArchitecture.Instance;
    }

    public class GameArchitecture : Architecture<GameArchitecture>
    {
        protected override void OnInit()
        {
            Register<IInputSystem>(new InputSystem());
        }

        protected override void OnDestroy()
        {

        }
    }
}