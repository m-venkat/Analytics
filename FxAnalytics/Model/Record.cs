using System;

namespace FxAnalytics.Model
{
    public enum BidOrAsk { NotSet = 0,Bid = 1, Ask = 2};

    public class TickInfo
    {
        public DateTime PriceTimeStamp { get; set;}
        public double Ask { get; set; }
        public double Bid { get; set; }
        public double AskVolume { get; set; }
        public double BidVolume { get; set; }
    }
}
