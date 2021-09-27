#if USE_REACTIVE

using System;
using UniRx;

namespace Detekonai.Core
{
	public partial class MessageBus
	{
		private IObservable<object>[] source;

		public IObservable<T> Observe<T>()
		   where T : BaseMessage
		{
			if(BaseMessage.Keys.TryGetValue(typeof(T), out int key))
			{
				if(source[key] == null)
				{
					source[key] = Observable.FromEvent<T>(x => Subscribe(x), y => Unsubscribe(y));
				}
				return (IObservable<T>)source[key];
			}
			else
			{
				throw new ArgumentException($"Unknown event type {typeof(T)}");
			}
		}

		public IDisposable SubscribeRx<T>(Action<T> handler)
			where T : BaseMessage
		{
			Subscribe(handler);
			return Disposable.Create(() => Unsubscribe(handler));
		}

	}
}

#endif
