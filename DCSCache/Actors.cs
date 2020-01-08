using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace XtreamCache.Actors
{
    public class CacheManagerActor : ReceiveActor
    {
        public static ConcurrentDictionary<string, IActorRef> CacheMap { get; private set; } = new ConcurrentDictionary<string, IActorRef>();
        public CacheManagerActor()
        {
            ReceiveAny((message) =>
            {
                Type messageType = message.GetType();
                Type cacheType = messageType.GetGenericArguments()[0];
                var cacheName = cacheType.Name;
                if (!CacheMap.ContainsKey(cacheName))
                    CacheMap.GetOrAdd(cacheName, Context.ActorOf(Props.Create(typeof(Cache<>).MakeGenericType(cacheType)), cacheName)).Tell(message);
                else
                    CacheMap.GetValueOrDefault(cacheName).Tell(message);
            });
        }
        public static Props Props => Props.Create<CacheManagerActor>();
    }
    public class Cache<T> : ReceiveActor where T : ICache
    {
        public static ConcurrentDictionary<string, IActorRef> CacheMap { get; private set; } = new ConcurrentDictionary<string, IActorRef>();
        public Cache()
        {
            Receive<ICacheAction<T>>(message =>
            {
                if (!CacheMap.ContainsKey(message.Record.getId()))
                    CacheMap.GetOrAdd(message.Record.getId(), Context.ActorOf(Props.Create(typeof(CacheRecord<T>)), message.Record.getId())).Tell(message);
                else
                    CacheMap.GetValueOrDefault(message.Record.getId()).Tell(message);
                if (CacheMap.Count() % 10000 == 0)
                    Debug.WriteLine(CacheMap.Count());
            });
        }

    }

    public class CacheRecord<T> : ReceiveActor where T : ICache
    {
        public T Record { get; private set; }
        public CacheRecord()
        {
            Receive<ICacheAction<T>>(message =>
            {
                Record = message.Record;
            });

            Receive<CacheQuery<T>>(query =>
            {
                if (query.Filter.Invoke(Record))
                    Sender.Tell(Record);
                else
                    Sender.Tell(new CacheQueryEmptyResponse { });
            });
        }
    }

    public enum Action
    {
        Insert,
        Update,
        Delete
    }

    public interface ICacheAction<T> where T : ICache
    {
        Action Action { get; set; }
        T Record { get; set; }
    }

    public sealed class CacheAction<T> : ICacheAction<T> where T : ICache
    {
        public Action Action { get; set; }
        public T Record { get; set; }
    }

    public interface ICache
    {
        string getId();
    }

    public class Client : ICache
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }

        public string getId()
        {
            return ClientId.ToString();
        }
    }
    public class Contact : ICache
    {
        public int ContactId { get; set; }

        public int ClientId { get; set; }

        public string ContactName { get; set; }

        public string getId()
        {
            return ContactId.ToString();
        }
    }
    public class Meeting : ICache
    {
        public int MeetingId { get; set; }
        public string MeetingName { get; set; }
        public int ClientId { get; set; }

        public string getId()
        {
            return MeetingId.ToString();
        }
    }
}
