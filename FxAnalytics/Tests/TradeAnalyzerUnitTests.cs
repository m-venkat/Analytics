using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using FxAnalytics.Model;
using Xunit;

namespace FxAnalytics.Tests
{
    public class TradeAnalyzerUnitTests
    {
        string validationLog = $@"E:\TickData\Experiments\ValidationErrors.txt";
        string input = $@"E:\TickData\Experiments\sampleFile.txt";


        InProgressTradeRecord currentBuy = null;
        InProgressTradeRecord currentShort = null;

        [Fact(DisplayName = "Check if parsing tick text into TickInfo Without losing format")]
        [Trait("FileProcessing", "File Processing Tests")]
        public void TestParsingTextToTickInfo()
        {


        }
         

        public void ParseWithoutLosingFormat(string inputpath, string validationLog)
        {
            StreamWriter sw = new StreamWriter(validationLog);
            using (StreamReader sr = File.OpenText(inputpath))
            {
                sr.ReadLine();//Ignore Header
                string s = String.Empty;
                while ((s = sr.ReadLine()) != null)
                {
                    TickInfo record = new TickInfo();
                   var elements = s.Split(',');
                    if(elements.Length != 5)
                    {
                        sw.WriteLine($"{DateTime.Now}\tExpected Number of columns not present\tRecord:{s}");
                        sw.Flush();
                    }
                    try
                    {
                        record.PriceTimeStamp = DateTime.ParseExact(elements[0], "yyyyMMdd HH:mm:ss:fff", CultureInfo.InvariantCulture);
                        record.Bid = Double.Parse(elements[1], CultureInfo.InvariantCulture);
                        record.Ask = Double.Parse(elements[2], CultureInfo.InvariantCulture);
                        record.BidVolume = Double.Parse(elements[3], CultureInfo.InvariantCulture);
                        record.AskVolume = Double.Parse(elements[4], CultureInfo.InvariantCulture);
                        var formedString = $"{record.PriceTimeStamp.ToString("yyyyMMdd HH:mm:ss:fff")},{record.Bid},{record.Ask},{record.BidVolume},{record.AskVolume}";
                        if (s != formedString)
                        {
                            sw.WriteLine($"Error: Not matching\n{s}\n{formedString}");
                            Console.WriteLine($"Error: Not matching\n{s}\n{formedString}");
                        }
                        Console.Write("|");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now}\tData Conversion Issue:{s}");
                        sw.WriteLine($"{DateTime.Now}\tData Conversion Issue:{s}");
                        sw.Flush();
                    }
                }
            }
        
            sw.Flush();
            sw.Dispose();
            sw = null;

        }

        public void OneBuyAndOneSell(string inputpath)
        {
            bool firstTrade = true;
      
            using (StreamReader sr = File.OpenText(inputpath))
            {
                //Buy at Ask Price
                //Sell at Bid Price

                sr.ReadLine();//Ignore Header
                string s = String.Empty;
                int consecutiveWinCounter = 1;
               
                int tradeGroupCounter = 1;

                List<TradeCompletedRecord> groupCompleted = new List<TradeCompletedRecord>();
             
                BidOrAsk lastWonTrade = BidOrAsk.NotSet;

              
             

                while ((s = sr.ReadLine()) != null)
                {
                    TickInfo tickPrice = new TickInfo();
                    var elements = s.Split(',');
                   
                    try
                    {
                        tickPrice.PriceTimeStamp = DateTime.ParseExact(elements[0], "yyyyMMdd HH:mm:ss:fff", CultureInfo.InvariantCulture);
                        tickPrice.Bid = Double.Parse(elements[1], CultureInfo.InvariantCulture);
                        tickPrice.Ask = Double.Parse(elements[2], CultureInfo.InvariantCulture);
                        tickPrice.BidVolume = Double.Parse(elements[3], CultureInfo.InvariantCulture);
                        tickPrice.AskVolume = Double.Parse(elements[4], CultureInfo.InvariantCulture);
                        if(firstTrade)
                        {
                            //Make one Buy and Make one Sell
                            OpenNewTrade(tickPrice);
                            firstTrade = false;
                        }
                        else
                        {
                            //On Every Tick this block will be called
                            if(tickPrice.Bid >= currentBuy.TargetPrice || tickPrice.Ask <= currentShort.TargetPrice)//TP Met for either
                            {
                                TradeCompletedRecord tc = new TradeCompletedRecord();
                                tc.EntryPrice = tickPrice.Bid >= currentBuy.TargetPrice ? currentBuy.EntryPrice : currentShort.EntryPrice;
                                tc.OrderOpenTimeStamp = tickPrice.Bid >= currentBuy.TargetPrice ? currentBuy.OrderOpenTimeStamp : currentShort.OrderOpenTimeStamp;
                                tc.OrderCloseTimeStamp = tickPrice.PriceTimeStamp;
                                tc.TradeGroupId = tradeGroupCounter;

                                tc.TradeDirection = tickPrice.Bid >= currentBuy.TargetPrice ? BidOrAsk.Ask :BidOrAsk.Bid;
                                tc.ExitPrice = tickPrice.Bid >= currentBuy.TargetPrice ? currentBuy.TargetPrice : currentShort.TargetPrice;
                                
                                tc.MaximumDrawDownPips = tickPrice.Bid >= currentBuy.TargetPrice ? currentBuy.MaxDrawDownPips : currentShort.MaxDrawDownPips;
                                if (lastWonTrade == BidOrAsk.NotSet)//Very first win (No previous trades exists
                                {
                                    tc.ConsecutiveWinNumber = 1;
                                    tc.WinAfterConsecutiveLoses = 0;
                                    lastWonTrade = tickPrice.Bid >= currentBuy.TargetPrice ? BidOrAsk.Ask : BidOrAsk.Bid;
                                    groupCompleted.Add(tc);
                                    OpenNewTrade(tickPrice);
                                }
                                else
                                {
                                    if(tickPrice.Bid >= currentBuy.TargetPrice &&  lastWonTrade == BidOrAsk.Ask)//Buy won now and last win is buy (consecutive Win)
                                    {
                                        consecutiveWinCounter++;
                                        tc.ConsecutiveWinNumber = consecutiveWinCounter;
                                        lastWonTrade = BidOrAsk.Ask;
                                        groupCompleted.Add(tc);
                                        OpenNewTrade(tickPrice);
                                    }
                                    else if (tickPrice.Bid >= currentBuy.TargetPrice && lastWonTrade == BidOrAsk.Bid)//Buy won now and last win is sell (Direction Reversal)
                                    {
                                        tc.ConsecutiveWinNumber =1;
                                        tc.WinAfterConsecutiveLoses = consecutiveWinCounter;
                                        tc.TradeGroupId = tradeGroupCounter;
                                        //As Direction reversed, Reset All Counters
                                        consecutiveWinCounter = 1;
                                         tradeGroupCounter++;//Move the Group Counter +1;
                                         lastWonTrade = BidOrAsk.Ask;
                                        //Create New Buy and Sell Order at this point (Make a generic function)
                                        groupCompleted.Add(tc);
                                        OpenNewTrade(tickPrice);
                                      

                                    }
                                    else if (tickPrice.Ask <= currentShort.TargetPrice && lastWonTrade == BidOrAsk.Bid)//Sell won now and last win is Sell (consecutive Win)
                                    {
                                        consecutiveWinCounter++;
                                        tc.ConsecutiveWinNumber = consecutiveWinCounter;
                                        tc.TradeGroupId = tradeGroupCounter;
                                        lastWonTrade = BidOrAsk.Bid;
                                        groupCompleted.Add(tc);
                                        OpenNewTrade(tickPrice);
                                    }
                                    else if (tickPrice.Ask <= currentShort.TargetPrice && lastWonTrade == BidOrAsk.Ask)//Sell won now and last win is Buy (Direction Reversal)
                                    {
                                        tc.ConsecutiveWinNumber = 1;
                                        tc.WinAfterConsecutiveLoses = consecutiveWinCounter;

                                        //As Direction reversed, Reset All Counters
                                        consecutiveWinCounter = 1;
                                        tradeGroupCounter++;//Move the Group Counter +1;
                                        tc.TradeGroupId = tradeGroupCounter;
                                        lastWonTrade = BidOrAsk.Bid;
                                        //Create New Buy and Sell Order at this point (Make a generic function)
                                        groupCompleted.Add(tc);
                                        OpenNewTrade(tickPrice);
                                    }
                                }
                                
                            }
                            else
                            {
                                //track the max drawdown
                                currentBuy.MaxDrawDownPips = (currentBuy.EntryPrice - tickPrice.Bid )* 100000 > currentBuy.MaxDrawDownPips ? (currentBuy.EntryPrice - tickPrice.Bid) *100000 : currentBuy.MaxDrawDownPips;
                                currentShort.MaxDrawDownPips = (tickPrice.Ask - currentShort.EntryPrice  ) * 100000 > currentShort.MaxDrawDownPips ? (tickPrice.Ask - currentShort.EntryPrice) * 100000 : currentShort.MaxDrawDownPips;
                            }
                        }
                       
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now}\tData Conversion Issue:{s}");                      
                    }
                }//End of While Loop Read
            }//End of Stream Reader
           
        }//End of function

        private void OpenNewTrade(TickInfo record)
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
        
    }
}
