using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Detekonai.Core
{
    public partial class MessageBus : IMessageBus
    {

        private readonly Delegate[] delegates;
        private readonly Dictionary<int, object> messagesUnderWait = new Dictionary<int, object>();
        private bool hasWaitingMessages = false;
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
                    hasWaitingMessages = true;
                }

                if(token != CancellationToken.None)
                {
                    token.Register(() => 
                        {
                            //TODO this can be trouble if we cancel in the wrong thread
                            tcs.TrySetCanceled();
                            messagesUnderWait.Remove(key);
                            hasWaitingMessages = messagesUnderWait.Count > 0;
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

        private IHandlerToken InternalSubscribe<T>(Type type, Action<T> handler) where T:BaseMessage
        {
            if (BaseMessage.Keys.TryGetValue(type, out int key))
            {
                Delegate originalHandler = delegates[key];
                var actionType = typeof(Action<>).MakeGenericType(type);
                var nh = Delegate.CreateDelegate(actionType, handler.Target, handler.Method);

                if (originalHandler != null)
                {
                    delegates[key] = Delegate.Combine(originalHandler, nh);
                }
                else
                {
                    delegates[key] = nh;
                }
                return new SingularHandlerToken(this, key, type, nh);
            }
            else
            {
                throw new ArgumentException($"Unknown message type {type}", nameof(handler));
            }
        }

        public IHandlerToken SubscribeChildren<T>(Action<T> handler) where T : BaseMessage
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().Where(x => !IsOmittable(x)).SelectMany(s => s.GetTypes()).Where(p => p.IsSubclassOf(typeof(T)));

            CompositeHandlerToken token = new CompositeHandlerToken();
            foreach (Type t in types)
            {
                if (BaseMessage.Keys.TryGetValue(t, out int key))
                {
                    token.AddToken(key, InternalSubscribe(t, handler));
                }
                else
                {
                    throw new ArgumentException($"Unknown message type {t}", nameof(handler));
                }
            }
            return token;
        }

        public IHandlerToken Subscribe(Type type, Action<BaseMessage> handler)
        {
            return InternalSubscribe(type, handler);
        }


        private static bool IsOmittable(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;
            return StartsWith("System") ||
                StartsWith("Microsoft") ||
                StartsWith("Windows") ||
                StartsWith("Unity") ||
                StartsWith("netstandard");

            bool StartsWith(string value) => assemblyName.StartsWith(value, ignoreCase: false, culture: CultureInfo.CurrentCulture);
        }


        public void Trigger<T>(T evt)
			where T : BaseMessage
        {
            var originalHandler = delegates[evt.Id] as Action<T>;
            originalHandler?.Invoke(evt);
            if (hasWaitingMessages && messagesUnderWait.TryGetValue(evt.Id, out object val))
            {
                ((TaskCompletionSource<T>)val).TrySetResult(evt);
                messagesUnderWait.Remove(evt.Id);
                hasWaitingMessages = messagesUnderWait.Count > 0;
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
            if(token is CompositeHandlerToken compositeTkn)
            {
                foreach(var t in compositeTkn.Tokens)
                {
                    Unsubscribe(t.Value);
                }
            }
            else if(token is SingularHandlerToken singleTkn)
            {
                Unsubscribe(singleTkn.Key, singleTkn.Handler);
            }
            else
            {
                throw new ArgumentException($"Unknown token type {token.GetType()}", nameof(token));
            }
		}
        private void Unsubscribe(int key, Delegate handler)
        {
            if(key < 0 || key > delegates.Length-1)
            {
                throw new ArgumentException($"Tyr to unsubscribe for unknown message: {key}", nameof(key));
            }
            Delegate originalHandler = delegates[key];

            if (originalHandler != null)
            {
                if (originalHandler == handler)
                {
                    delegates[key] = null;
                }
                else
                {
                    delegates[key] = Delegate.Remove(originalHandler, handler);
                }
            }
        }
    }
}
