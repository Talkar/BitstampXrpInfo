using CryptoCoinInfo.Objects;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoCoinInfo
{
    public partial class CryptoInfo : Form
    {
        private readonly string baseUrl = "https://www.bitstamp.net/api/v2/ticker/";
        public CryptoInfo()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "CryptoInfo";
            txtXrp.Text = GetValueFromFile();
            var coinTimer = new Timer();
            SetCoinInfo();
            coinTimer.Tick += CoinTimer_Tick;
            coinTimer.Interval = 10000;
            coinTimer.Start();
        }

        private void CoinTimer_Tick(object sender, EventArgs e)
        {
            SetCoinInfo();
        }

        private void SetCoinInfo()
        {

            var xrpInfo = GetCryptoInfo(coinPram: "xrpeur");
            if (xrpInfo == null)
                return;
            lblXrpCoinValue.Text = Math.Round(xrpInfo.Bid, 3).ToString() + "€";
            lblXrpEurDiff.Text = Math.Round(xrpInfo.Difference, 3).ToString() + "%";
            SaveValueToFile();
            var xrpAmountText = GetValueFromFile();
            if (string.IsNullOrEmpty(xrpAmountText))
            {
                lblXrpAmountValue.Text = "";
            }
            else
            {
                var couldParseXrpAmount = decimal.TryParse(xrpAmountText, out var xrpAmount);
                if (couldParseXrpAmount)
                {
                    lblXrpAmountValue.Text = Math.Round(xrpAmount * xrpInfo.Bid,2).ToString() + "€";
                }
                else
                {
                    lblXrpAmountValue.Text = "";
                }
            }


            if (xrpInfo.Difference > 0)
            {
                lblXrpEurDiff.ForeColor = Color.Green;
                lblXrpCoinValue.ForeColor = Color.Green;
                lblXrpAmountValue.ForeColor = Color.Green;
            }
            else
            {
                lblXrpEurDiff.ForeColor = Color.Red;
                lblXrpCoinValue.ForeColor = Color.Red;
                lblXrpAmountValue.ForeColor = Color.Red;

            }
        }

        private CoinInfo GetCryptoInfo(string coinPram)
        {
            var client = new RestClient(baseUrl);

            var request = new RestRequest(coinPram, Method.GET);

            var coin = client.Execute<CoinInfo>(request).Data;
            if (coin == null)
                return null;
            coin.Difference = CalculatePriceDifferenceInPercentage(currentPrice: coin.Bid, openPrice: coin.Open);

            return coin;
        }

        private decimal CalculatePriceDifferenceInPercentage(decimal currentPrice, decimal openPrice)
        {
            var difference = currentPrice - openPrice;
            var differenceInPercentage = (difference / currentPrice) * 100;

            return differenceInPercentage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SetCoinInfo();
        }

        private void SaveValueToFile()
        {
            if (string.IsNullOrEmpty(txtXrp.Text))
                return;

            if (!decimal.TryParse(txtXrp.Text, out var tempValue))
                return;

            using (var tw = new StreamWriter("XrpAmount.txt", false))
            {

                tw.WriteLine(txtXrp.Text);
            }

        }

        private string GetValueFromFile()
        {
            try
            {

                using (var tw = new StreamReader("XrpAmount.txt"))
                {
                    return tw.ReadLine();
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
