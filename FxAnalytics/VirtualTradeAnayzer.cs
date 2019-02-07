using FxAnalytics.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FxAnalytics
{
    public class VirtualTradeAnayzer
    {

        private string _inputTradeListFilePath = string.Empty;
        private InProgressTradeRecord currentBuy = null;
        private InProgressTradeRecord currentShort = null;

        public bool FirstTrade { get; private set; }
        public int ConsecutiveWinCounter { get; private set; }
        public int TradeGroupCounter { get; private set; } = 1;

        public List<TradeCompletedRecord> GroupCompleted { get; private set; }

        public BidOrAsk LastWonTrade  { get; private set; } =BidOrAsk.NotSet;
        


        public VirtualTradeAnayzer(string tickInfoFilePath)
        {
            _inputTradeListFilePath = tickInfoFilePath;
        }

        internal TickInfo ParseTextLineToTickInfo(string tickText)
        {
            TickInfo tickInfo = new TickInfo();
            var elements = tickText.Split(',');
            if (elements.Length != 5)
            {
                Console.WriteLine($"{DateTime.Now}\tExpected Number of columns not present\tRecord:{tickText}");
                throw new Exception($"{DateTime.Now}\tExpected Number of columns not present\tRecord:{tickText}");
            }
            try
            {
                tickInfo.PriceTimeStamp = DateTime.ParseExact(elements[0], "yyyyMMdd HH:mm:ss:fff", CultureInfo.InvariantCulture);
                tickInfo.Bid = Double.Parse(elements[1], CultureInfo.InvariantCulture);
                tickInfo.Ask = Double.Parse(elements[2], CultureInfo.InvariantCulture);
                tickInfo.BidVolume = Double.Parse(elements[3], CultureInfo.InvariantCulture);
                tickInfo.AskVolume = Double.Parse(elements[4], CultureInfo.InvariantCulture);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}\tData Conversion Issue:{tickText}");
                throw new Exception($"{DateTime.Now}\tData Conversion Issue:{tickText}");
              
            }
            return tickInfo;
        }

        public void AnalyzeTrades()
        {
            using (StreamReader sr = File.OpenText(_inputTradeListFilePath))
            {
                sr.ReadLine();//Ignore Header
                string s = String.Empty;
                TickInfo tickInfo = ParseTextLineToTickInfo(sr.ReadLine());
            }           
        }

        internal void OpenNewTrade(TickInfo record)
        {
            currentBuy = new InProgressTradeRecord();
            currentBuy.OrderOpenTimeStamp = record.PriceTimeStamp;
            currentBuy.EntryPrice = record.Ask;
            currentBuy.TargetPrice = record.Ask + 0.00030;

            currentShort = new InProgressTradeRecord();
            currentShort.OrderOpenTimeStamp = record.PriceTimeStamp;
            currentShort.EntryPrice = record.Bid;
            currentShort.TargetPrice = record.Bid - 0.00030;
        }

        internal void ProcessTicks(TickInfo tick)
        {
            if (FirstTrade)
            {
                //Make one Buy and Make one Sell
                OpenNewTrade(tick);
                FirstTrade = false;
            }
            else
            {
                //On Every Tick this block will be called
                if (tick.Bid >= currentBuy.TargetPrice || tick.Ask <= currentShort.TargetPrice)//TP Met for either
                {
                    TradeCompletedRecord tc = new TradeCompletedRecord();
                    tc.EntryPrice = tick.Bid >= currentBuy.TargetPrice ? currentBuy.EntryPrice : currentShort.EntryPrice;
                    tc.OrderOpenTimeStamp = tick.Bid >= currentBuy.TargetPrice ? currentBuy.OrderOpenTimeStamp : currentShort.OrderOpenTimeStamp;
                    tc.OrderCloseTimeStamp = tick.PriceTimeStamp;
                    tc.TradeGroupId = TradeGroupCounter;

                    tc.TradeDirection = tick.Bid >= currentBuy.TargetPrice ? BidOrAsk.Ask : BidOrAsk.Bid;
                    tc.ExitPrice = tick.Bid >= currentBuy.TargetPrice ? currentBuy.TargetPrice : currentShort.TargetPrice;

                    tc.MaximumDrawDownPips = tick.Bid >= currentBuy.TargetPrice ? currentBuy.MaxDrawDownPips : currentShort.MaxDrawDownPips;
                    if (LastWonTrade == BidOrAsk.NotSet)//Very first win (No previous trades exists
                    {
                        tc.ConsecutiveWinNumber = 1;
                        tc.WinAfterConsecutiveLoses = 0;
                        LastWonTrade = tick.Bid >= currentBuy.TargetPrice ? BidOrAsk.Ask : BidOrAsk.Bid;
                        GroupCompleted.Add(tc);
                        OpenNewTrade(tick);
                    }
                    else
                    {
                        if (tick.Bid >= currentBuy.TargetPrice && LastWonTrade == BidOrAsk.Ask)//Buy won now and last win is buy (consecutive Win)
                        {
                            ConsecutiveWinCounter++;
                            tc.ConsecutiveWinNumber = ConsecutiveWinCounter;
                            LastWonTrade = BidOrAsk.Ask;
                            GroupCompleted.Add(tc);
                            OpenNewTrade(tick);
                        }
                        else if (tick.Bid >= currentBuy.TargetPrice && LastWonTrade == BidOrAsk.Bid)//Buy won now and last win is sell (Direction Reversal)
                        {
                            tc.ConsecutiveWinNumber = 1;
                            tc.WinAfterConsecutiveLoses = ConsecutiveWinCounter;
                            tc.TradeGroupId = TradeGroupCounter;
                            //As Direction reversed, Reset All Counters
                            ConsecutiveWinCounter = 1;
                            TradeGroupCounter++;//Move the Group Counter +1;
                            LastWonTrade = BidOrAsk.Ask;
                            //Create New Buy and Sell Order at this point (Make a generic function)
                            GroupCompleted.Add(tc);
                            OpenNewTrade(tick);
                        }
                        else if (tick.Ask <= currentShort.TargetPrice && LastWonTrade == BidOrAsk.Bid)//Sell won now and last win is Sell (consecutive Win)
                        {
                            ConsecutiveWinCounter++;
                            tc.ConsecutiveWinNumber = ConsecutiveWinCounter;
                            tc.TradeGroupId = TradeGroupCounter;
                            LastWonTrade = BidOrAsk.Bid;
                            GroupCompleted.Add(tc);
                            OpenNewTrade(tick);
                        }
                        else if (tick.Ask <= currentShort.TargetPrice && LastWonTrade == BidOrAsk.Ask)//Sell won now and last win is Buy (Direction Reversal)
                        {
                            tc.ConsecutiveWinNumber = 1;
                            tc.WinAfterConsecutiveLoses = ConsecutiveWinCounter;

                            //As Direction reversed, Reset All Counters
                            ConsecutiveWinCounter = 1;
                            TradeGroupCounter++;//Move the Group Counter +1;
                            tc.TradeGroupId = TradeGroupCounter;
                            LastWonTrade = BidOrAsk.Bid;
                            //Create New Buy and Sell Order at this point (Make a generic function)
                            GroupCompleted.Add(tc);
                            OpenNewTrade(tick);
                        }
                    }

                }
                else
                {
                    //track the max drawdown
                    currentBuy.MaxDrawDownPips = (currentBuy.EntryPrice - tick.Bid) * 100000 > currentBuy.MaxDrawDownPips ? (currentBuy.EntryPrice - tick.Bid) * 100000 : currentBuy.MaxDrawDownPips;
                    currentShort.MaxDrawDownPips = (tick.Ask - currentShort.EntryPrice) * 100000 > currentShort.MaxDrawDownPips ? (tick.Ask - currentShort.EntryPrice) * 100000 : currentShort.MaxDrawDownPips;
                }
            }
        }
    }
}
