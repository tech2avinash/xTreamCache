using Dapper.FluentMap.Mapping;
using System;
using XtreamCache.Actors;

namespace XtreamCache.Web
{
    public class IPInformation: ICache
    {
        public long From { get; set; }
        public long To { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Lat { get; set; }
        public string Long { get; set; }
        public string PinCode { get; set; }
        public string TimeZone { get; set; }

        public string getId()
        {
            return $"{From}-{To}";
        }
    }


    internal class IpInformationMap : EntityMap<IPInformation>
    {
        internal IpInformationMap()
        {
            Map(i => i.From).ToColumn("From");
            Map(i => i.To).ToColumn("To");
            Map(i => i.CountryCode).ToColumn("CountryCode");
            Map(i => i.CountryName).ToColumn("CountryName");
            Map(i => i.State).ToColumn("State");
            Map(i => i.City).ToColumn("City");
            Map(i => i.Lat).ToColumn("Lat");
            Map(i => i.Long).ToColumn("Long");
            Map(i => i.PinCode).ToColumn("Pin");
            Map(i => i.TimeZone).ToColumn("TimeZone");
        }
    }
}
