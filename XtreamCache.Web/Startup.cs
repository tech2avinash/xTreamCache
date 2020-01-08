using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Dapper;
using Dapper.FluentMap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XtreamCache.Actors;

namespace XtreamCache.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ActorSystem actorManager = ActorManager.Start(new string[] { });
            IActorRef cacheMangerRef = actorManager.ActorOf(CacheManagerActor.Props, nameof(CacheManagerActor));
            FluentMapper.Initialize(config =>
            {
                config.AddMap(new IpInformationMap());
            });
            services.AddTransient<IDbConnection>(_ => new SqlConnection(Configuration.GetConnectionString("IpInfoDB")));
            services.AddSingleton(actorManager);
            services.AddControllers();
            var provider = services.BuildServiceProvider();
            Task.Run(async () =>
            {
                try
                {
                    Func<IDbConnection> conn = () => provider.GetService<IDbConnection>();
                    var totalRows = await conn().ExecuteScalarAsync<long>("select count(*) as count from IPData");
                    var pageSize = 100000;
                    var totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
                    for (var pageNumber = 0; pageNumber < totalPages; pageNumber++)
                    {
                        var query = $@"select * from IPData where CountryCode in ('IN') ORDER BY [From] OFFSET {pageSize * pageNumber} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                        var data = (await conn().QueryAsync<IPInformation>(query, null, commandTimeout: 180)).ToList();
                        foreach (var row in data) cacheMangerRef.Tell(new CacheAction<IPInformation> { Action = Actors.Action.Insert, Record = row }); ;
                        data.Clear();
                    };
                }
                catch (Exception ex)
                {

                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
