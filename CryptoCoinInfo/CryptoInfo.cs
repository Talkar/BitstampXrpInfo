using CryptoCoinInfo.Objects;
using Microsoft.Win32;
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
        private const string AppName = "CryptoInfoXrp";
        public CryptoInfo()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "CryptoInfo";
            txtXrp.Text = GetValueFromFile();
            var coinTimer = new Timer();
            SetCoinInfo();
            SetChecked();
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
            lblXrpCoinValue.Text = Math.Round(xrpInfo.Bid, 6).ToString() + "€";
            lblXrpEurDiff.Text = Math.Round(xrpInfo.Difference, 6).ToString() + "%";
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
                    lblXrpAmountValue.Text = Math.Round(xrpAmount * xrpInfo.Bid, 2).ToString() + "€";
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

        private void SaveValueToFile()
        {
            if (string.IsNullOrEmpty(txtXrp.Text))
                return;

            if (!decimal.TryParse(txtXrp.Text, out var tempValue))
                return;
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo");
            try
            {
                using (var tw = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo/XrpAmount.txt", false))
            {
                tw.WriteLine(txtXrp.Text);
            }
            }
            catch (Exception)
            {
                return;
            }
        }

        private string GetValueFromFile()
        {
            try
            {

                var directoryInfo = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo");
                using (var tw = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo/XrpAmount.txt"))
                {
                    return tw.ReadLine();
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void RegisterInStartup()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (chkStartWithWindows.Checked)
            {
                registryKey.SetValue(AppName, Application.ExecutablePath);
            }
            else
            {
                registryKey.DeleteValue(AppName);
            }
        }

        private void SetChecked()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            chkStartWithWindows.Checked = registryKey.GetValue(AppName) != null;
        }

        private void chkStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            RegisterInStartup();
        }

		private void btnUpdate_Click(object sender, EventArgs e)
		{
			SetCoinInfo();
		}
	}
}
