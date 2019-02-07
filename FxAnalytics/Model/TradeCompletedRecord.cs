using System;

namespace FxAnalytics.Model
{
    public class TradeCompletedRecord
    {
        public int TradeGroupId { get; set; }
        public BidOrAsk TradeDirection { get; set; }
        public DateTime OrderOpenTimeStamp { get; set; }
        public DateTime OrderCloseTimeStamp { get; set; }
        public Double EntryPrice { get; set; }
        public Double ExitPrice { get; set; }
        public int ConsecutiveWinNumber { get; set; }
        public int WinAfterConsecutiveLoses { get; set; }
        public double MaximumDrawDownPips { get; set; } = 0;

        public override string ToString()
        {
            return $"TradeGroupId : {TradeGroupId.ToString()},ConsecutiveWinNumber: {ConsecutiveWinNumber}, WinAfterConsecutiveLoses:{WinAfterConsecutiveLoses},  TradeDirection : {TradeDirection.ToString()}, OrderTimeStamp:{OrderOpenTimeStamp}, EntryPrice: {EntryPrice}, ExitPrice: {ExitPrice}, MaximumDrawDown: {MaximumDrawDownPips}";
        }
    }
}
