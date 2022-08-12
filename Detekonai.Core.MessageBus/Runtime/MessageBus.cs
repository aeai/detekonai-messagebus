using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Detekonai.Core
{
    public partial class MessageBus : IMessageBus
    {

        private readonly Delegate[] delegates;
        private readonly Dictionary<int, object> messagesUnderWait = new Dictionary<int, object>();

        public MessageBus()
        {
            delegates = new Delegate[BaseMessage.Keys.Count];
#if USE_REACTIVE
            source = new IObservable<object>[BaseMessage.Keys.Count];
#endif
        }

        public async Task<T> GetMessageAsync<T>(CancellationToken token) where T : BaseMessage
        {
            if (BaseMessage.Keys.TryGetValue(typeof(T), out int key))
            {
                TaskCompletionSource<T> tcs = null;
                if(messagesUnderWait.TryGetValue(key, out object ob))
                {
                    tcs = (TaskCompletionSource<T>)ob;
                }
                else
                {
                    tcs = new TaskCompletionSource<T>();
                    messagesUnderWait[key] = tcs;
                }

                if(token != CancellationToken.None)
                {
                    token.Register(() => 
                        {
                            tcs.TrySetCanceled();
                            messagesUnderWait.Remove(key);
                        }
                    , true);
                }
                return await tcs.Task.ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException($"Unknown event type {typeof(T)}");
            }
        }

        public async Task<T> GetMessageAsync<T>() where T : BaseMessage
        {
            return await GetMessageAsync<T>(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary> if you not unsubscribe you will leak memory!!!</summary>
        public void Subscribe<T>(Action<T> handler)
			where T : BaseMessage
        {
            if (BaseMessage.Keys.TryGetValue(typeof(T), out int key))
            {
                Delegate originalHandler = delegates[key];
                if (originalHandler != null)
                {
                    var old = (Action<T>)originalHandler;
                    old += handler;
                    delegates[key] = old;
                }
                else
                {
                    delegates[key] = handler;
                }
            }
            else
            {
                throw new ArgumentException($"Unknown event type {typeof(T)}", nameof(handler));
            }
        }

        public IHandlerToken Subscribe(Type type, Action<BaseMessage> handler)
		{
			if(BaseMessage.Keys.TryGetValue(type, out int key))
			{
				Delegate originalHandler = delegates[key];
				var actionType = typeof(Action<>).MakeGenericType(type);
				var nh = Delegate.CreateDelegate(actionType, handler.Target, handler.Method);
				var handlerType = typeof(HandlerToken<>).MakeGenericType(type);

				if(originalHandler != null)
				{
					delegates[key] = Delegate.Combine(originalHandler, nh);	
				}
				else
				{
					delegates[key] = nh;
				}
				return (IHandlerToken)Activator.CreateInstance(handlerType,
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
					null,
					new object[] {this, type, nh },
					null);
			}
			else
			{
				throw new ArgumentException($"Unknown event type {type}", nameof(handler));
			}
		}

        public void Trigger<T>(T evt)
			where T : BaseMessage
        {
            var originalHandler = delegates[evt.Id] as Action<T>;
            originalHandler?.Invoke(evt);
            if (messagesUnderWait.TryGetValue(evt.Id, out object val))
            {
                ((TaskCompletionSource<T>)val).TrySetResult(evt);
                messagesUnderWait.Remove(evt.Id);
            }
        }

        public void Unsubscribe<T>(Action<T> handler)
			where T : BaseMessage
        {
            if (BaseMessage.Keys.TryGetValue(typeof(T), out int key))
            {
                Delegate originalHandler = delegates[key];

                if (originalHandler != null)
                {
                    var old = (Action<T>)originalHandler;
                    if (old == handler)
                    {
                        delegates[key] = null;
                    }
                    else
                    {
                        old -= handler;
                        delegates[key] = old;
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Unknown event type {typeof(T)}", nameof(handler));
            }
        }

        public void Unsubscribe(IHandlerToken token)
		{
			if(BaseMessage.Keys.TryGetValue(token.MessageType, out int key))
			{
				Delegate originalHandler = delegates[key];

				if(originalHandler != null)
				{
					if(originalHandler == token.Handler)
					{
						delegates[key] = null;
					}
					else
					{
						delegates[key] = Delegate.Remove(originalHandler, token.Handler);
					}
				}
			}
			else
			{
				throw new ArgumentException($"Unknown event type {token.MessageType}", nameof(token));
			}
		}
	}
}
