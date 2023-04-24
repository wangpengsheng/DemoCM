using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApp1
{
    public abstract class Subscriber
    {
        public int Addresses { get; set; }

        public abstract void OnDataChanged(int value);
    }

    internal class Program
    {
        private static readonly ConcurrentDictionary<int, Subscriber> Subscribers =
            new ConcurrentDictionary<int, Subscriber>();

        private readonly object _lock = new object();

        private static void Main(string[] args)
        {

            // 模拟数据变化
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (true)
                {
                    var value = new Random().Next(100); // 假设变化后的数据值为一个随机数

                    Thread.Sleep(1);
                    var address = new Random().Next(10); // 假设数据变化的地址为0到99之间的随机数
                    UpdateCache(address, value); // 更新缓存中的数据
                    NotifySubscribers(address); // 通知订阅者
                }
            });
            // 订阅数据变化
            Subscribe(new List<Subscriber>
            {
                new V1(new AddressSubscriber(3)),
                new V1(new AddressSubscriber(4)),
                new V1(new AddressSubscriber(5)),
                new V1(new AddressSubscriber(6)),
                new V1(new AddressSubscriber(7)),
                new V1(new AddressSubscriber(8)),
                new V1(new AddressSubscriber(9)),
            });
            Console.ReadLine();
        }

        private static void Subscribe(IEnumerable<Subscriber> subscribers)
        {
            foreach (var subscriber in subscribers)
            {
                if (!Program.Subscribers.ContainsKey(subscriber.Addresses))
                {
                    Program.Subscribers.TryAdd(subscriber.Addresses, subscriber);
                }
            }
        }

        private static void Unsubscribe(IEnumerable<Subscriber> subscribers)
        {
            foreach (var subscriber in subscribers)
            {
                if (Program.Subscribers.ContainsKey(subscriber.Addresses))
                {
                    Program.Subscribers.TryRemove(subscriber.Addresses, out _);
                }
            }
        }

        private static void NotifySubscribers(int address)
        {
            if (Program.Subscribers.ContainsKey(address))
            {
                var subscriber = Program.Subscribers[address];
                var cacheV = GetCacheValue(address);
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    lock (subscriber)
                    {
                        var st = new Stopwatch();
                        st.Start();
                        subscriber.OnDataChanged(cacheV);
                        st.Stop();
                        Console.WriteLine($"{st.ElapsedMilliseconds}");
                    }
                });
            }
            else
            {
                Console.WriteLine($"没有包含当前地址:{address}");
            }
        }

        private static readonly ConcurrentDictionary<int, int> Cache = new ConcurrentDictionary<int, int>();

        private static void UpdateCache(int address, int value)
        {
            Cache[address] = value;
        }

        private static int GetCacheValue(int address)
        {
            return Cache.TryGetValue(address, out var value) ? value : 0;
        }
    }

    public class AddressSubscriber : Subscriber
    {
        public AddressSubscriber(int address)
        {
            Addresses = address;
        }

        public override void OnDataChanged(int value)
        {
            Console.WriteLine($"Data at address {Addresses} has changed to {value}");
        }
    }

    public abstract class ValueDecorator : Subscriber
    {
        protected Subscriber sub;

        protected ValueDecorator(Subscriber subscriber)
        {
            Addresses = subscriber.Addresses;
            sub = subscriber;
        }

        // 抽象方法，用于定义装饰器的操作
        public abstract bool Decorate();

        public abstract void After();
    }

    public class V1 : ValueDecorator
    {
        private int CurrentValue { get; set; }
        private int OldValue { get; set; }

        public V1(Subscriber subscriber) : base(subscriber)
        {
        }

        public override void OnDataChanged(int value)
        {
            CurrentValue = value;
            if (Decorate())
            {
                Console.WriteLine($"{CurrentValue}:{OldValue}");
                sub.OnDataChanged(CurrentValue);
                OldValue = CurrentValue;
            }
            else
            {
                Console.WriteLine($"值相同...");
            }

            After();
        }

        public override bool Decorate()
        {
            return CurrentValue != OldValue;
        }

        public override void After()
        {
            Thread.Sleep(1000);
        }
    }
}