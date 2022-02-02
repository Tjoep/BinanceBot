# BinanceBot

A bot to automatically buy and sell cryptocurrency on Binance. 

# Strategy

Currently this project consists of two different bots, named **EthTrader** and **BtcTrader** which each apply a different strategy. 

## EthTrader

The **EthTrader** applies a very simple strategy. It will check the current price of ETH, and create **BUY** orders for a customizable percentage difference below this current price. When this **BUY** order is filled, the bot will create a **SELL** order for a better price based on the same percentage difference.

Example: Say the current price of ETH is $1000 USD, and the customizable percentage difference is set at 10%.
1. The bot creates a BUY order for the price: $1000 * 0.9 = $900.
2. When the price of ETH drops below $900, the order will be filled.
3. The bot creates a SELL order for the price: $900 * 1.1 = $990.
4. When the price of ETH rises back above $990, the sell order will be filled. Back to step 1.  

## BtcTrader

The strategy of the **BtcTrader** is a bit more advanced than the **EthTrader**. The strategy is based on the reddit post: "https://www.reddit.com/r/CryptoCurrency/comments/s7ykio/when_to_buy_and_when_to_sell_bitcoin_an/". 

What the **BtcTrader** does is the following:
- Fetch the **BTC** price data for each day for the past 2 years. This data is used to calculated the 2 year moving average. The source of the data is: https://data.binance.vision/, the bot stores the price table in Azure Table Storage so it only has to fetch the historical price data once.
- Import the **BTC** revenue from a text file. This data is used to calculate the Puell Year Multiple. I couldn't find any free API sources to fetch the historical BTC revenue so I created an account on https://ycharts.com and saved their revenue data in a text file (~/Data/dayrevenue.txt), from https://ycharts.com/indicators/bitcoin_miners_revenue_per_day.
- Fetch the Fear & Greed index from https://alternative.me/crypto/fear-and-greed-index/.

The **BtcTrader** will **buy** BTC when **all** of the following criteria are met:
- Fear & Greed index < 20
- The Puell Year Multiple < 0.5
- The current price of BTC < the 2 year moving average


The **BtcTrader** will **sell** BTC when **all** of the following criteria are met:
- Fear & Greed index > 75
- The Puell Year Multiple > 4
- The current price of BTC > the 2 year moving average * 5.
### Staking
The **BtcTrader** will automatically put your available **BUSD** or **BTC** in a flexible savings account on Binance.
- If the **BUY** criteria are met: Redeems all **BUSD** from the flexible savings account and uses it to buy **BTC**, the **BTC** will be then but in a savings account until the **SELL** criteria are met.
- If the **SELL** criteria are met: Redeems all **BTC** and sells it for **BUSD**, the **BUSD** will be put in a flexible savings account until the **BUY** criteria are met. 

# Requirements
In order to install this bot you will need the following:
- [Microsoft Azure Storage Account](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-overview). 
- [Binance Account](https://accounts.binance.com/en/register?ref=103505228) with API credentials which Reading & Spot and Margin trading rights. 
- To properly calculate the Puell Multiple, the BTC revenue data from the last year is needed. The revenue data from Dec 28, 2020 to Feb 01, 2022 is provided in the text file (~/Data/dayrevenue.txt). You can either add the revenue data for dates after Feb 01, 2022 yourself or pay for API access to a revenue data provider, for example: https://cryptoquant.com/docs#operation/getPuellIndex


