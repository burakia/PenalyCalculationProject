using System;
using System.Net;
using System.Xml;

namespace PenalyCalculationProject.ExchangeRate
{
    class TRYExchRate
    {
        /// <summary>
        /// TCMB base api link
        /// </summary>
        private const string ApiBaseUrl = "http://www.tcmb.gov.tr/kurlar/{0}.xml";

        /// <summary>
        /// If there is no exchange rate on the date specified by the user, the number of days to check backwards
        /// </summary>
        private const int ExchRateAttempts = 5;

        /// <summary>
        /// Api link with date information used for fetching rates
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// CurrencyDate
        /// </summary>
        public DateTime CurrencyDate { get; set; }

        /// <summary>
        /// Actual date the exchange rate was received from the TCMB
        /// </summary>
        public DateTime ActualCurrencyDate { get; set; }

        /// <summary>
        /// The variable where the used exchange rate data is stored
        /// </summary>
        private XmlDocument XmlDoc { get; set; }


        /// <summary>
        /// </summary>
        /// <param name="currencyDate">Date of exchange rate</param>
        public TRYExchRate(DateTime currencyDate)
        {
            CurrencyDate = currencyDate;
        }

        /// <summary>
        /// Get currency Rate
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="exchRateType"></param>
        /// <returns></returns>
        public Decimal GetExchRate(string currency, ExchRateType exchRateType)
        {
            
            if (XmlDoc == null)
            {
                LoadExchRate();
            }

            // TCMB noktayı (.) ondalık ayracı olarak kullanıyor.
            // string'den decimal'e çevrim sırasında windows region ayarlarından etkilenmeden doğru çevrilmesi için en-us culture'ı kullanılır
            System.Globalization.CultureInfo culInfo = new System.Globalization.CultureInfo("en-US", true);

            // xml içinde okunacak node ayarlanır
            string nodeStr = String.Format("Tarih_Date/Currency[@CurrencyCode='{0}']/{1}", currency.ToUpper(), GetExchRateTypeNodeStr(exchRateType));

            // string olarak alınan kur decimal'e çevrilip dönülür
            return Decimal.Parse(XmlDoc.SelectSingleNode(nodeStr).InnerXml, culInfo);
        }


        /// <summary>
        /// api url information
        /// </summary>
        private void GenerateApiUrl()
        {
            ApiUrl = String.Format(TRYExchRate.ApiBaseUrl, this.ActualCurrencyDate.ToString("yyyyMM") + "/" + this.ActualCurrencyDate.ToString("ddMMyyyy"));
        }


        /// <summary>
        /// Belirtilen tarihteki TCMB'deki bütün kurları çeker
        /// </summary>
        public void LoadExchRate()
        {
            ActualCurrencyDate = CurrencyDate;

            // kullanıcının belirttiği tarihte kur var ise alınır
            // yok ise en yakın kur olan gün bulunur
            for (int attempts = 0; attempts <= TRYExchRate.ExchRateAttempts; attempts++)
            {
                try
                {
                    GenerateApiUrl();

                    XmlDoc = new XmlDocument();
                    XmlDoc.Load(ApiUrl);

                    break;
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        // 404 not found
                        HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                        if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            // bir gün öncesi kontrol edilir
                            ActualCurrencyDate = ActualCurrencyDate.AddDays(-1);
                        }
                        else
                        {
                            throw new Exception("Kur bilgisi bulunamadı.");
                        }
                    }
                    else
                    {
                        throw new Exception("Kur bilgisi bulunamadı.");
                    }
                }
            }

            if (XmlDoc == null)
            {
                throw new Exception("Kur bilgisi bulunamadı.");
            }
        }


        private string GetExchRateTypeNodeStr(ExchRateType exchRateType)
        {
            string ret = "";

            switch (exchRateType)
            {
                case ExchRateType.ForexBuying:
                    ret = "ForexBuying";
                    break;

                case ExchRateType.BanknoteSelling:
                    ret = "BanknoteSelling";
                    break;
            }

            return ret;
        }
    }


    /// <summary>
    ///Exchange rate
    /// </summary>
    public enum ExchRateType
    {
        /// <summary>
        /// Döviz Alış
        /// </summary>
        ForexBuying,


        /// <summary>
        /// Efektif Satış
        /// </summary>
        BanknoteSelling
    }
  
}