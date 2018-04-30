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
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoCoinInfo
{
    public partial class CryptoInfo : Form
    {
        private readonly string baseUrl = "https://www.bitstamp.net/api/v2/ticker/";
        private const string AppName = "CryptoInfoXrp";
        private readonly string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo/Settings.ini";
        private const string xrpAmountKey = "XrpAmount";
        private const string apiKeyKey = "ApiKey";
        private const string exchangeCoinKey = "ExchangeCoin";
        private const string alwaysOnTopKey = "AlwaysOnTop";
        private IniFileHandler iniFileHandler;
        private MemoryCache cache;
        public CryptoInfo()
        {
            InitializeComponent();
            iniFileHandler = new IniFileHandler(IniPath: settingsPath);
            cache = new MemoryCache(name: "CryptoInfoXrpCache");
            lblApiKeyError.Visible = false;
            CheckIfOldFileExist_MoveToNewSettingsFile();
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "CryptoInfo";
            txtXrp.Text = iniFileHandler.Read(Key: xrpAmountKey);
            var coinTimer = new Timer();
            LoadAlwaysOnTop();
            SetChecked();
            LoadApiKey();
            LoadCurrencyCoin();
            SetCoinInfo();
            coinTimer.Tick += CoinTimer_Tick;
            coinTimer.Interval = 10000;
            coinTimer.Start();
        }

        private void LoadAlwaysOnTop()
        {
            var topmostValue = iniFileHandler.Read(Key: alwaysOnTopKey);
            if (string.IsNullOrWhiteSpace(topmostValue))
            {
                topmostValue = "False";
                iniFileHandler.Write(Key: alwaysOnTopKey, Value: topmostValue);
            }
            chkAlwaysOnTop.Checked = TopMost = bool.Parse(topmostValue);
        }

        private void LoadCurrencyCoin()
        {
            var currencyCoin = iniFileHandler.Read(Key: exchangeCoinKey);
            if (!string.IsNullOrWhiteSpace(currencyCoin))
                txtCurrency.Text = currencyCoin;
        }

        private void LoadApiKey()
        {
            var apiKey = iniFileHandler.Read(Key: apiKeyKey);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                txtApiKey.Text = apiKey;
            }
        }

        private decimal ConvertFromEurToCurrency(string valueInEur, string currencyToConvertTo)
        {
            var exhangeRate = cache[$"EUR{currencyToConvertTo.ToUpper().Trim()}"] as string;
            if (string.IsNullOrWhiteSpace(exhangeRate))
            {
                exhangeRate = SetExchangeRate(currencyToConvertTo);
            }

            return decimal.Parse(valueInEur.Replace('.', ',')) * decimal.Parse(exhangeRate.Replace('.',','));
        }

        private string SetExchangeRate(string currencyToConvertTo)
        {
            if (string.IsNullOrWhiteSpace(currencyToConvertTo))
                return "1";

            var apiKey = iniFileHandler.Read(Key: apiKeyKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var savedApiKey = SaveApiKey();
                if (string.IsNullOrWhiteSpace(savedApiKey))
                {
                    lblApiKeyError.Visible = true;
                    return "1";
                }
                else
                    apiKey = SaveApiKey();
            }

            var client = new RestClient("http://data.fixer.io/api/");
            var request = new RestRequest(resource: "latest", method: Method.GET);
            request.AddQueryParameter(name: "access_key", value: apiKey);
            request.AddQueryParameter(name: "base", value: "EUR");
            request.AddQueryParameter(name: "symbols", value: currencyToConvertTo);

            var response = client.Get<FixerIoResponseObject>(request);
            var exchangeRate = response.Data.Rates.Single(r => r.Key == currencyToConvertTo).Value;
            cache.Set(key: $"EUR{currencyToConvertTo.ToUpper().Trim()}", value: exchangeRate, policy: new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddDays(1) });
            return exchangeRate;
        }

        private void CheckIfOldFileExist_MoveToNewSettingsFile()
        {
            var xrpAmount = GetValueFromOldFile();
            if (string.IsNullOrWhiteSpace(xrpAmount))
                return;

            iniFileHandler.Write(Key: xrpAmountKey, Value: xrpAmount);

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
            lblXrpEur.Text = $"{xrpInfo.LastInEuro} €";
            lblCoinDiff.Text = "XRP / " + iniFileHandler.Read(Key: exchangeCoinKey) + ": ";
            lblDiffPercentage.Text = "XRP open/last in percent: ";
            lblXrpCoinValue.Text = Math.Round(xrpInfo.Last, 3).ToString() + " " + iniFileHandler.Read(Key: exchangeCoinKey);
            lblXrpEurDiff.Text = Math.Round(xrpInfo.Difference, 3).ToString() + "%";
            SaveCoinInfo();
            var xrpAmountText = iniFileHandler.Read(Key: xrpAmountKey);
            if (string.IsNullOrEmpty(xrpAmountText))
            {
                lblXrpAmountValue.Text = "";
            }
            else
            {
                var couldParseXrpAmount = decimal.TryParse(xrpAmountText, out var xrpAmount);
                if (couldParseXrpAmount)
                {
                    lblXrpAmountValue.Text = Math.Round(xrpAmount * xrpInfo.Last, 2).ToString() + " " + iniFileHandler.Read(Key: exchangeCoinKey);
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

        private void SaveCoinInfo()
        {
            if (string.IsNullOrEmpty(txtXrp.Text))
                return;

            if (!decimal.TryParse(txtXrp.Text, out var tempValue))
                return;

            iniFileHandler.Write(Key: xrpAmountKey, Value: txtXrp.Text.Trim());
        }

        private CoinInfo GetCryptoInfo(string coinPram)
        {
            var client = new RestClient(baseUrl);

            var request = new RestRequest(coinPram, Method.GET);

            var coin = client.Execute<CoinInfo>(request).Data;
            if (coin == null)
                return null;
            coin.LastInEuro = coin.Last;
            var convertedLast = ConvertFromEurToCurrency(coin.Last.ToString(), txtCurrency.Text.ToUpper().Trim());
            coin.Last = convertedLast;

            var convertedOpen = ConvertFromEurToCurrency(coin.Open.ToString(), txtCurrency.Text.ToUpper().Trim());
            coin.Open = convertedOpen;

            coin.Difference = CalculatePriceDifferenceInPercentage(currentPrice: coin.Last, openPrice: coin.Open);

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
            if (!String.IsNullOrWhiteSpace(txtCurrency.Text.Trim()))
                iniFileHandler.Write(Key: exchangeCoinKey, Value: txtCurrency.Text.Trim().ToUpper());
            SetCoinInfo();
        }

        private string GetValueFromOldFile()
        {
            try
            {

                var directoryInfo = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo");
                using (var tw = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo/XrpAmount.txt"))
                {
                    var result = tw.ReadLine();
                    if (!string.IsNullOrWhiteSpace(result))
                        File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CryptoInfo/XrpAmount.txt");
                    return result;
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

        private void btnSaveApiKey_Click(object sender, EventArgs e)
        {
            SaveApiKey();
        }

        private string SaveApiKey()
        {
            iniFileHandler.Write(Key: apiKeyKey, Value: txtApiKey.Text.Trim());
            return txtApiKey.Text.Trim();
        }

        private void chkAlwaysOnTop_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAlwaysOnTop.Checked)
            {
                iniFileHandler.Write(Key: alwaysOnTopKey, Value: "True");
                TopMost = true;
            }
            else
            {
                iniFileHandler.Write(Key: alwaysOnTopKey, Value: "False");
                TopMost = false;
            }
        }
    }
}
