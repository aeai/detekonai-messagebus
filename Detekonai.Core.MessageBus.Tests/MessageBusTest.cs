using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
#if USE_REACTIVE
using UniRx;
#endif

namespace Detekonai.Core.Tests
{
	public class MessageBusTest
	{
		public class TestEvent : BaseMessage
		{
			public int Alma { get; set; } = 1;
		}
		public class TestEvent2 : BaseMessage
		{
		}
		public class TestEvent3 : BaseMessage
		{
		}

		private event Action<TestEvent> PerfEvent;

		[Test]
		public void CallingSingleSubsriptionWorks()
		{
			var bus = new MessageBus();

			int call = 0;
			bus.Subscribe<TestEvent>(e => call++);
			bus.Trigger(new TestEvent());
			Assert.That(call, Is.EqualTo(1));
		}

		[Test]
		public void CallingSingleSubsriptionInNonGenericFormWorks()
		{
			var bus = new MessageBus();

			int call = 0;
			bus.Subscribe(typeof(TestEvent),e => call++);
			bus.Trigger(new TestEvent());
			Assert.That(call, Is.EqualTo(1));
		}

		[Test]
		public void CallingSingleSubsriptionInNonGenericFormWithHandlerTrigger()
		{
			var bus = new MessageBus();

			int call = 0;
			IHandlerToken token = bus.Subscribe(typeof(TestEvent), e => call++);
			token.Trigger(new TestEvent());
			Assert.That(call, Is.EqualTo(1));
		}

		[Test]
		public void CallingSubscriptionWithBothGenericAndNonGenericFormWorksIfWeStartWithNonGeneric()
		{
			var bus = new MessageBus();

			int call = 0;
			bus.Subscribe(typeof(TestEvent), e => call++);
			bus.Subscribe<TestEvent>(e => call++);
			bus.Subscribe(typeof(TestEvent), e => call++);
			bus.Subscribe<TestEvent>(e => call++);
			bus.Trigger(new TestEvent());

			Assert.That(call, Is.EqualTo(4));
		}

		[Test]
		public void CallingSubscriptionWithBothGenericAndNonGenericFormWorksIfWeStartWithGeneric()
		{
			var bus = new MessageBus();

			int call = 0;
			bus.Subscribe<TestEvent>(e => call++);
			bus.Subscribe(typeof(TestEvent), e => call++);
			bus.Subscribe<TestEvent>(e => call++);
			bus.Subscribe(typeof(TestEvent), e => call++);
			bus.Trigger(new TestEvent());

			Assert.That(call, Is.EqualTo(4));
		}

		[Test]
		public void CallingSubscriptionWithBothGenericAndNonGenericFormParamResolutionWorks()
		{
			var bus = new MessageBus();

			int call = 0;
			void Handler1(TestEvent evt)
			{
				call += evt.Alma;
			}

			void Handler2(BaseMessage msg)
			{
				call += (msg as TestEvent).Alma;
			}

			bus.Subscribe<TestEvent>(Handler1);
			bus.Subscribe(typeof(TestEvent), Handler2);
			bus.Subscribe<TestEvent>(Handler1);
			bus.Subscribe(typeof(TestEvent), Handler2);
			bus.Trigger(new TestEvent());

			Assert.That(call, Is.EqualTo(4));
		}

		[Test]
		public void WeDontLetTheSubsribersCallReleaseOnTheEvent()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			var evt = new TestEvent();
			bus.Subscribe<TestEvent>(e => callCounter++);
			bus.Trigger(evt);
			Assert.That(callCounter, Is.EqualTo(1));
		}

		[Test]
		public void CallingSingleSubscriptionMultipleTimesWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			bus.Subscribe<TestEvent>(e => callCounter++);
			bus.Trigger(new TestEvent());
			bus.Trigger(new TestEvent());
			bus.Trigger(new TestEvent());
			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(4));
		}

		[Test]
		public void SubscribingMultipleTimesForSameEventWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			int callCounter12 = 0;
			int callCounter13 = 0;
			bus.Subscribe<TestEvent>(e => callCounter++);
			bus.Subscribe<TestEvent>(e => callCounter12++);
			bus.Subscribe<TestEvent>(e => callCounter13++);
			bus.Trigger(new TestEvent());

			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter12, Is.EqualTo(1));
			Assert.That(callCounter13, Is.EqualTo(1));
		}
		[Test]
		public void SubscribingForDifferentEventWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			int callCounter2 = 0;
			int callCounter3 = 0;
			bus.Subscribe<TestEvent>(e => callCounter++);
			bus.Subscribe<TestEvent2>(e => callCounter2++);
			bus.Subscribe<TestEvent3>(e => callCounter3++);
			bus.Trigger(new TestEvent());
			bus.Trigger(new TestEvent2());
			bus.Trigger(new TestEvent3());

			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter2, Is.EqualTo(1));
			Assert.That(callCounter3, Is.EqualTo(1));
		}

		[Test]
		public void UnSubscribingTheLastListenerWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			void Handler(TestEvent e) => callCounter++;
			bus.Subscribe((Action<TestEvent>)Handler);

			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));

			bus.Unsubscribe((Action<TestEvent>)Handler);

			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
		}

		[Test]
		public void UnSubscribingAListenerWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			int callCounter12 = 0;
			void Handler(TestEvent e) => callCounter++;
			void Handler2(TestEvent e) => callCounter12++;
			bus.Subscribe((Action<TestEvent>)Handler);
			bus.Subscribe((Action<TestEvent>)Handler2);
			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter12, Is.EqualTo(1));
			bus.Unsubscribe((Action<TestEvent>)Handler);

			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter12, Is.EqualTo(2));
		}

		[Test]
		public void UnSubscribingANonGenericListenerWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			int callCounter12 = 0;
			void Handler(BaseMessage e) => callCounter++;
			void Handler2(BaseMessage e) => callCounter12++;
			IHandlerToken token1 = bus.Subscribe(typeof(TestEvent), Handler);
			IHandlerToken token2 = bus.Subscribe(typeof(TestEvent), Handler2);
			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter12, Is.EqualTo(1));
			bus.Unsubscribe(token1);

			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter12, Is.EqualTo(2));
		}

		[Test]
		public void UnSubscribingANonGenericListenerWorksEvenWhenWehaveGenericListenersToo()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			int callCounter12 = 0;
			void Handler(BaseMessage e) => callCounter++;
			void Handler2(TestEvent e) => callCounter12++;
			bus.Subscribe<TestEvent>(Handler2);
			IHandlerToken token1 = bus.Subscribe(typeof(TestEvent), Handler);
			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter12, Is.EqualTo(1));
			bus.Unsubscribe(token1);

			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callCounter12, Is.EqualTo(2));
		}

		[Test]
		public void UnSubscribingAListenerWhichNotSubscribedWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			int callCounter2 = 0;
			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(0));
			Assert.That(callCounter2, Is.EqualTo(0));

			Assert.That(() => bus.Unsubscribe<TestEvent>(e => callCounter++), Throws.Nothing);
		}

		[Test]
		public void SubscribingInAHandlerWorks()
		{
			var bus = new MessageBus();
			int counter = 0;
			int counter2 = 0;
			void InnerHandler(TestEvent x) => counter2++;
			void Handler(TestEvent x)
			{
				if(counter == 0)
				{
					bus.Subscribe<TestEvent>(InnerHandler);
				}
				counter++;
			}
			bus.Subscribe<TestEvent>(Handler);
			Assert.That(() => bus.Trigger(new TestEvent()), Throws.Nothing);
			Assert.That(counter, Is.EqualTo(1));
			Assert.That(counter2, Is.EqualTo(0));
			Assert.That(() => bus.Trigger(new TestEvent()), Throws.Nothing);
			Assert.That(counter, Is.EqualTo(2));
			Assert.That(counter2, Is.EqualTo(1));
		}

		[Test]
		public void TriggeringANotListenedEventDoNothing()
		{
			var bus = new MessageBus();

			Assert.That(() => bus.Trigger(new TestEvent()), Throws.Nothing);
		}

		[Test]
		public async Task CanGetMessages()
		{
			var bus = new MessageBus();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
				{
					await Task.Delay(1000);
					bus.Trigger(new TestEvent(){ Alma = 12});
				}
			);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			TestEvent ret = await bus.GetMessageAsync<TestEvent>();

            Assert.That(ret, Is.Not.Null);
			Assert.That(ret.Alma, Is.EqualTo(12));
		}

		[Test]
		public async Task CanGetMultipleMessages()
		{
			var bus = new MessageBus();
			TestEvent anotherEvent = null;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Task.Run(async () =>
			{
				await Task.Delay(500);
				anotherEvent = await bus.GetMessageAsync<TestEvent>();
			}
);
			Task.Run(async () =>
			{
				await Task.Delay(1000);
				bus.Trigger(new TestEvent() { Alma = 12 });
			}
			);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			TestEvent ret = await bus.GetMessageAsync<TestEvent>();
			Assert.That(ret, Is.Not.Null);
			Assert.That(ret.Alma, Is.EqualTo(12));
			Assert.That(anotherEvent, Is.Not.Null);
			Assert.That(anotherEvent.Alma, Is.EqualTo(12));
		}

		[Test]
		public async Task GetMessageCanTimeout()
		{
			var bus = new MessageBus();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
			{
				await Task.Delay(1000);
				bus.Trigger(new TestEvent() { Alma = 12 });
			}
			);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


            var source = new CancellationTokenSource();
			source.CancelAfter(50);
		

			Assert.ThrowsAsync<TaskCanceledException>( async () => { await bus.GetMessageAsync<TestEvent>(source.Token); });
		}

		[Test]
		public async Task FinishedGetMessageDontTimeout()
		{
			var bus = new MessageBus();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Task.Run(async () =>
			{
				await Task.Delay(1000);
				bus.Trigger(new TestEvent() { Alma = 12 });
			}
			);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			var source = new CancellationTokenSource();
			source.CancelAfter(2000);
			TestEvent ret = await bus.GetMessageAsync<TestEvent>(source.Token);
			await Task.Delay(3000);
			Assert.That(ret, Is.Not.Null);
			Assert.That(ret.Alma, Is.EqualTo(12));
		}


		[Test]
		//[Ignore("Performance Test, not used during regular CI")]
		public void CheckBusPerformanceVsDirectCall()
		{
			Console.WriteLine($"----------------------------------------");
			var bus = new MessageBus();
			IMessageBus ibus = bus;
			int counter = 0;
			void Handler(TestEvent x) => counter++;
			void Handler3(BaseMessage m) => counter++;
			bus.Subscribe<TestEvent>(Handler);
			IHandlerToken token = bus.Subscribe(typeof(TestEvent3), Handler3);
			var timer = new System.Diagnostics.Stopwatch();
			var evt = new TestEvent();
			var evt3 = new TestEvent3();
			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				bus.Trigger(evt);
			}
			timer.Stop();
			Console.WriteLine($"Bus: {timer.ElapsedMilliseconds} ms");

			timer.Reset();

			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				token.Trigger(evt3);
			}
			timer.Stop();
			Console.WriteLine($"Token: {timer.ElapsedMilliseconds} ms");

			timer.Reset();

			//Using DynamicInvoke is ~50x slower than using IhandlerToken
			//timer.Start();
			//for(int i = 0; i < 1000000; i++)
			//{
			//	bus.Trigger(typeof(TestEvent3), evt3);
			//}
			//timer.Stop();
			//Debug.Log($"DynamicTrigger: {timer.ElapsedMilliseconds} ms");

			timer.Reset();

			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				ibus.Trigger(evt);
			}
			timer.Stop();
			Console.WriteLine($"Bus Interface: {timer.ElapsedMilliseconds} ms");

			timer.Reset();

			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				bus.Trigger(new TestEvent());
			}
			timer.Stop();
			Console.WriteLine($"Bus new: {timer.ElapsedMilliseconds} ms");

			timer.Reset();
			PerfEvent += Handler;
			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				PerfEvent.Invoke(evt);
			}
			timer.Stop();
			Console.WriteLine($"Event: {timer.ElapsedMilliseconds} ms");

			timer.Reset();
			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				Handler(evt);
			}
			timer.Stop();
			Console.WriteLine($"Direct: {timer.ElapsedMilliseconds} ms");

			Action<TestEvent> handlerDelegate;
			handlerDelegate = Handler;
			timer.Reset();
			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				handlerDelegate(evt);
			}
			timer.Stop();
			Console.WriteLine($"Delegate: {timer.ElapsedMilliseconds} ms");
#if USE_REACTIVE
			TestEvent2 evt2 = new TestEvent2();
			void Handler2(TestEvent2 x) => counter++;
			bus.Observe<TestEvent2>().Subscribe(Handler2);
			timer.Reset();
			timer.Start();
			for(int i = 0; i < 1000000; i++)
			{
				bus.Trigger(evt2);
			}
			timer.Stop();
			Console.WriteLine($"Rx: {timer.ElapsedMilliseconds} ms");
#endif
			Console.WriteLine($"----------------------------------------");
		}

#if USE_REACTIVE

		[Test]
		public void UsingReactiveDisposableSubsribeUnsubscribeOnDispose()
		{
			var bus = new MessageBus();

			int callCounter = 0;
			using(bus.SubscribeRx<TestEvent3>(e => callCounter++))
			{
				bus.Trigger(new TestEvent3());
			}
			bus.Trigger(new TestEvent3());
			Assert.That(callCounter, Is.EqualTo(1));
		}

		[Test]
		public void UsingReactiveStandaloneObservableWorks()
		{
			var bus = new MessageBus();
			int callCounter = 0;
			bus.Observe<TestEvent>().Subscribe(x => callCounter++);
			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
		}

		[Test]
		public void UsingReactiveMixedWithRawEventHandlingWorksIfEventSubsribeFirst()
		{
			var bus = new MessageBus();
			int callRxCounter = 0;
			int callCounter = 0;
			bus.Subscribe<TestEvent>(x => callCounter++);
			bus.Observe<TestEvent>().Subscribe(x => callRxCounter++);
			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callRxCounter, Is.EqualTo(1));
		}

		[Test]
		public void UsingReactiveMixedWithRawEventHandlingWorksIfRxFirst()
		{
			var bus = new MessageBus();
			int callRxCounter = 0;
			int callCounter = 0;
			bus.Observe<TestEvent>().Subscribe(x => callRxCounter++);
			bus.Subscribe<TestEvent>(x => callCounter++);

			bus.Trigger(new TestEvent());
			Assert.That(callCounter, Is.EqualTo(1));
			Assert.That(callRxCounter, Is.EqualTo(1));
		}

		[Test]
		public void UsingReactiveUnsubsribeWorks()
		{
			var bus = new MessageBus();
			int callRxCounter = 0;
			using(bus.Observe<TestEvent>().Subscribe(x => callRxCounter++))
			{
				bus.Trigger(new TestEvent());

			}
			bus.Trigger(new TestEvent());
			Assert.That(callRxCounter, Is.EqualTo(1));
		}
#endif

	}

}
