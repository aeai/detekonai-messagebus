using System;
using System.Collections.Generic;

namespace Detekonai.Core
{
	public interface IHandlerToken
	{
		public Delegate Handler { get; }
		public void Trigger(BaseMessage msg);
	}

    public sealed class CompositeHandlerToken : IHandlerToken
	{
        public Delegate Handler { get; private set; }

		public Dictionary<int, IHandlerToken> Tokens { get; } = new Dictionary<int, IHandlerToken>();

		public void AddToken(int key, IHandlerToken token)
        {
			Tokens.Add(key, token);
        }

        public void Trigger(BaseMessage msg)
        {
			Tokens[msg.Id].Trigger(msg);
        }
    }

    public sealed class SingularHandlerToken : IHandlerToken
    {
        public Delegate Handler => token.Handler;

		private readonly IHandlerToken token;
		public int Key { get; }
		public void Trigger(BaseMessage msg)
        {
			token.Trigger(msg);
        }

		internal SingularHandlerToken(IMessageBus bus, int key, Type type, Delegate handler)
		{
			Key = key;
			var handlerType = typeof(HandlerToken<>).MakeGenericType(type);
			token =  (IHandlerToken)Activator.CreateInstance(handlerType,
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
					null,
					new object[] { bus, handler },
					null);
		}
	}

    public sealed class HandlerToken<T> : IHandlerToken
		where T : BaseMessage
	{
		public Delegate Handler { get; private set; }
		private readonly IMessageBus bus;
        public void Trigger(BaseMessage msg)
		{
			bus.Trigger(msg as T);
		}

		internal HandlerToken(IMessageBus bus, Delegate handler)
		{
			this.bus = bus;
            Handler = handler;
		}
	}
}
