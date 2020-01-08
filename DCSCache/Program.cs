using Akka.Actor;
using Akka.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace XtreamCache.Actors
{
    class Program
    {
        private static ActorSystem actorManager;
        static async Task Main(string[] args)
        {

            actorManager = ActorManager.Start(args);
            IActorRef cacheMangerRef = actorManager.ActorOf(CacheManagerActor.Props, nameof(CacheManagerActor));
            await LoadSampleMeetings(cacheMangerRef);
            Console.WriteLine("Done");
            Console.ReadLine();
        }
        static async Task LoadSampleMeetings(IActorRef cacheMangerRef)
        {
            int sampleRecords = 100000;
            Parallel.ForEach(Enumerable.Range(0, sampleRecords), meetingId =>
            {
                cacheMangerRef.Tell(new CacheAction<Meeting> { Action = Action.Insert, Record = new Meeting { ClientId = meetingId, MeetingId = meetingId, MeetingName = $"Meeting {meetingId}" } });
            });
            while (true)
            {
                if (Cache<Meeting>.CacheMap.Count == sampleRecords)
                {
                    var query = new CacheQuery<Meeting>(m => m.MeetingId >= 0);
                    var actor = actorManager.ActorOf(Props.Create(() => new CacheQueryActor<Meeting>(TimeSpan.FromSeconds(31), false)));
                    CacheQueryResponse<Meeting> tt = await actor.Ask<CacheQueryResponse<Meeting>>(query);
                    CacheQueryResponse<Meeting> tt1 = await actor.Ask<CacheQueryResponse<Meeting>>(query);
                    break;
                }
            }
        }
    }
}
