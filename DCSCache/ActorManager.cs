using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Cluster.Sharding;

namespace XtreamCache.Actors
{
    public static class ActorManager
    {
        public static ActorSystem actorSystem { get; private set; }

        public static Task WhenTerminated => actorSystem.WhenTerminated;
        public static ActorSystem DCSActorSystem { get; private set; }
        public static IActorRef ShardRegion { get; private set; }
        public static ActorSystem Start(string[] args)
        {
            var config = ConfigurationFactory.ParseString(File.ReadAllText("node.hocon"))
                .WithFallback(ClusterSharding.DefaultConfig())
                .BootstrapFromDocker();
            actorSystem = ActorSystem.Create("xTreamCacheSystem", config);
            return actorSystem;
        }
        public static Task Stop()
        {
            return CoordinatedShutdown
                .Get(actorSystem)
                .Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }
}
