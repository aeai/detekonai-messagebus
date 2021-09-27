using System;

namespace Detekonai.Core
{
	public interface IHandlerToken
	{
		public Type MessageType { get;}
		public Delegate Handler { get; }
		public void Trigger(BaseMessage msg);
	}

	public sealed class HandlerToken<T> : IHandlerToken
		where T : BaseMessage
	{
		public Type MessageType { get; private set; }
		public Delegate Handler { get; private set; }
		private IMessageBus bus;

		public void Trigger(BaseMessage msg)
		{
			bus.Trigger(msg as T);
		}

		internal HandlerToken(IMessageBus bus, Type mType, Delegate handler)
		{
			this.bus = bus;
			MessageType = mType;
			Handler = handler;
		}
	}
}
