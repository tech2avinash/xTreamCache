using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace XtreamCache.Actors
{
    public class CacheQueryActor<T> : ReceiveActor where T : ICache
    {
        private IActorRef originalSender;
        private ISet<IActorRef> refs;
        private bool firstItem;
        private Stopwatch stopwatch =null;
        private long count = 0;
        private IActorRef router;
        public CacheQueryActor(TimeSpan timeOut = default(TimeSpan), bool firstItemOnly = false)
        {
            var cacheActorPaths = Cache<T>.CacheMap.Values.Select(c => c.Path.ToString());
            count = cacheActorPaths.LongCount();
            this.router = Context.System.ActorOf(Props.Empty
                        .WithRouter(new BroadcastGroup(cacheActorPaths)));
            //this.refs = Cache<T>.CacheMap.Values.ToHashSet();
            TimeSpan timeSpan = timeOut != default(TimeSpan) ? timeOut : TimeSpan.FromSeconds(5);
            firstItem = firstItemOnly;
            Context.SetReceiveTimeout(timeSpan);
            Receive<CacheQuery<T>>(x =>
            {
                originalSender = Sender;
                //foreach (var aref in this.refs) aref.Tell(x);
                this.router.Tell(x);
                stopwatch = Stopwatch.StartNew();
                Become(Query);
            });
        }
        private void Query()
        {
            var replies = new List<T>();
            // when timeout occurred, we reply with what we've got so far
            Receive<ReceiveTimeout>(_ => ReplyAndStop(replies));
            Receive<T>(x =>
            {
                //if (refs.Remove(Sender)) replies.Add(x);
                //if (refs.Count == 0 || firstItem) ReplyAndStop(replies);
                replies.Add(x); count--;
                if (count == 0 || firstItem) ReplyAndStop(replies);
            });
            Receive<CacheQueryEmptyResponse>(_ =>
            {
                //refs.Remove(Sender);
                //if (refs.Count == 0) ReplyAndStop(replies);
                count--;
                if (count == 0) ReplyAndStop(replies);
            });
        }

        private void ReplyAndStop(List<T> replies)
        {
            this.originalSender.Tell(new CacheQueryResponse<T>(replies));
            Debug.WriteLine($"Took {stopwatch.Elapsed.TotalMilliseconds} ms");
            Context.Stop(Self);
        }
    }
    public sealed class CacheQueryResponse<T>
    {
        public CacheQueryResponse(List<T> result)
        {
            Response = result;
        }

        public List<T> Response { get; private set; }
    }
    public sealed class CacheQueryEmptyResponse
    {
        public CacheQueryEmptyResponse() { }
    }
    public sealed class CacheQuery<T>
    {
        public Func<T, bool> Filter { get; private set; }
        public CacheQuery(Func<T, bool> predicate)
        {
            Filter = predicate;
        }
    }
}

