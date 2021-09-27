using System;

namespace Detekonai.Core
{
    public interface IMessageBus
    {
        void Trigger<T>(T evt)
			where T : BaseMessage;
        void Unsubscribe<T>(Action<T> handler)
			where T : BaseMessage;
        void Unsubscribe(IHandlerToken token);
        void Subscribe<T>(Action<T> handler)
			where T : BaseMessage;
        IHandlerToken Subscribe(Type type, Action<BaseMessage> handler);
#if USE_REACTIVE
        IDisposable SubscribeRx<T>(Action<T> handler)
			where T : BaseMessage;
        public IObservable<T> Observe<T>()
		   where T : BaseMessage;
#endif
	}
}
