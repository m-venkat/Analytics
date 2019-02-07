using System;

namespace FxAnalytics.Model
{
    public class InProgressTradeRecord
    {
        public BidOrAsk TradeDirection { get; set; }
        public DateTime OrderOpenTimeStamp { get; set; }
     
        public Double EntryPrice { get; set; }
        public Double TargetPrice { get; set; }
        public Double MaxDrawDownPips { get; set; } = 0;

        public override string ToString()
        {
            return $"TradeDirection : {TradeDirection.ToString()}, OrderTimeStamp:{OrderOpenTimeStamp}, EntryPrice: {EntryPrice}, TargetPrice: {TargetPrice}, MaximumDrawDown: {MaxDrawDownPips}";
        }
    }
}
