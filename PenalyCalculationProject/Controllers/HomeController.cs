using PenalyCalculationProject.ExchangeRate;
using PenalyCalculationProject.Models;
using PenalyCalculationProject.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;

namespace PenalyCalculationProject.Controllers
{
    public class HomeController : Controller
    {
        PenaltyCalculationEntities db = new PenaltyCalculationEntities();
        public ActionResult Index()
        {
            //populate dropdownlist
            var items = db.Country.ToList();
            if (items != null && items.Count > 0)
            {
                ViewBag.CountryId = new SelectList(items, "countryId", "countryName");
            }
            return View();
        }

        //click calcute button
        [HttpPost]
        public ActionResult Index(BookViewModel model)
        {
            ViewBag.CountryId = new SelectList(db.Country, "countryId", "countryName", model.countryId);
            const int dayRestriction = 10;
            
            if (!Validate(model))
            {
                return Redirect(Request.UrlReferrer.ToString());
            }

            string currencyDesc = GetCountryCurrencyDesc(model.countryId);                      
            double diffDateCount =TwoDateDifference(model.checkedOutDate,model.returnedDate);

            if (diffDateCount > dayRestriction)
            {
                double amountPerDay = PenaltyAmount(model.countryId);
                double totalPenaltyAmount = Math.Round(amountPerDay, 2) * (diffDateCount- dayRestriction);
                
                TempData["error"] = string.Format("You have a penalty of {0} {1} for {2} day delay", totalPenaltyAmount, currencyDesc, (diffDateCount - dayRestriction));
                //example  You have a penalty of  50 usd for a  12 day delay.
            }
            else
            {
                TempData["error"] = "it has not penalty";
            }
            return View();
        }
        /// <summary>
        /// this method validate weekends and national holidays/religious holidays
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Validate(BookViewModel model)
        {
            string countryCode = GetCountryCode(model.countryId);
            CultureInfo newCulture;
            newCulture = new CultureInfo(countryCode);
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                TempData["error"] = "Country can not find in database";
                return false;
            }
            else if (model.returnedDate < model.checkedOutDate)
            {
                TempData["error"] = "ReturnedDate must be greater than checked Out Date";
                return false;
            }

            else if(IsItWeekend(model.returnedDate, newCulture))
            {
                TempData["error"] = "Returned date can not be in weekends";
                return false;
            }
            else if(Holiday(model))
            {
                TempData["error"] = "Returned date have chosen holiday.Please choose next day";
                return false;
            }

            return true;
        }
        /// <summary>
        /// get Country Code (ex: TR,US,DE)
        /// </summary>
        /// <param name="countryId"></param>
        /// <returns></returns>
        public string GetCountryCode(int countryId)
        {
            string countryCode = db.Country.Where(x => x.countryId == countryId).FirstOrDefault().countryCode;

            return countryCode;
        }
        /// <summary>
        /// get Country Currency  (ex: TRY,USD,EUR)
        /// </summary>
        /// <param name="countryId"></param>
        /// <returns></returns>
        public string GetCountryCurrency(int countryId)
        {
            string currency = db.Country.Where(x => x.countryId == countryId).FirstOrDefault().currency;

            return currency;
        }
        /// <summary>
        /// get Country Currency  (ex: turk liras,dolar,euro)
        /// </summary>
        /// <param name="countryId"></param>
        /// <returns></returns>
        public string GetCountryCurrencyDesc(int countryId)
        {
            string currencyDesc = db.Country.Where(x => x.countryId == countryId).FirstOrDefault().currencyDesc;

            return currencyDesc;
        }
        /// <summary>
        /// weekend check
        /// </summary>
        /// <param name="countryId"></param>
        /// <returns></returns>
        public bool IsItWeekend(DateTime currentDay, CultureInfo cultureInfo)
        {
            bool isItWeekend = false;

            DayOfWeek firstDay = cultureInfo.DateTimeFormat.FirstDayOfWeek;

            DayOfWeek currentDayInProvidedDatetime = currentDay.DayOfWeek;

            DayOfWeek lastDayOfWeek = firstDay + 4;

            DayOfWeek firstHolidayDay;

            DayOfWeek secondHolidayDay;

            if (lastDayOfWeek.GetHashCode() == 5)
            {
                firstHolidayDay = lastDayOfWeek + 1;
                secondHolidayDay = lastDayOfWeek - 5;
            }
            else if (lastDayOfWeek.GetHashCode() == 6)
            {
                firstHolidayDay = lastDayOfWeek - 6;
                secondHolidayDay = lastDayOfWeek - 5;
            }
            else
            {
                firstHolidayDay = lastDayOfWeek + 1;
                secondHolidayDay = lastDayOfWeek + 2;
            }

            if (currentDayInProvidedDatetime == firstHolidayDay || currentDayInProvidedDatetime == secondHolidayDay)
                isItWeekend = true;

            return isItWeekend;
        }
        /// <summary>
        /// national holidays/religious holidays  for a specific country (using holidays.abstractapi.com )
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Holiday(BookViewModel model)
        {
            const string apiKey = "ed1c8b9cb2d14c608577413110921cb7";
            using (var client = new HttpClient())
            {
                string countryCode = GetCountryCode(model.countryId);
                string url = String.Format("https://holidays.abstractapi.com/v1/?api_key={0}&country={1}&year={2}&month={3}&day={4}", apiKey, countryCode, model.returnedDate.Year, model.returnedDate.Month, model.returnedDate.Day);
                client.DefaultRequestHeaders.Accept.Clear();
                client.BaseAddress = new Uri("https://holidays.abstractapi.com/v1");

                var responseTask = client.GetAsync(url);

                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsAsync<IList<HolidayModel>>();

                    readTask.Wait();
                    if (readTask.Result.Count > 0)
                    {
                        //it is holiday
                        return true;
                    }
                    else
                    {
                        //it is not holiday
                        return false;
                    }
                }
                else //web api sent error response 
                {
                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }
            return false;
        }

        /// <summary>
        /// Calculate Penalty Amount. TCMB Service used.Values in all currencies are references 5 dollars.Parity calculated
        /// </summary>
        /// <param name="countryId"></param>
        /// <returns></returns>
        public double PenaltyAmount(int countryId)
        {
            TRYExchRate helper = new TRYExchRate(DateTime.Now.Date);
            helper.LoadExchRate();
            string currency = GetCountryCurrency(countryId);          
            string othercost = string.Empty;//first value assign
            string turkLira = string.Empty;
            const double usdCost = 5; //5$

            if (currency == "USD")
            {
                return usdCost;
            }
            else if (currency == "TRY")
            {
                turkLira = helper.GetExchRate("USD", ExchRateType.BanknoteSelling).ToString(); // 1 $ = 7.76
                return usdCost * Convert.ToDouble(turkLira);
            }
            //The parity is calculated in dollars for the relevant currency
            else
            {
                turkLira = helper.GetExchRate("USD", ExchRateType.BanknoteSelling).ToString(); // 1 $ = 7.76
                othercost = helper.GetExchRate(currency, ExchRateType.BanknoteSelling).ToString(); // example 1 € 9.09

                double parity = Convert.ToDouble(turkLira) / Convert.ToDouble(othercost);

                return 5 * parity;
            }
        }

        /// <summary>
        /// checkedOutDate and returnedDate Difference (count formula : n-r+1) 
        /// </summary>
        /// <param name="checkedOutDate"></param>
        /// <param name="returnedDate"></param>
        /// <returns></returns>
        public double TwoDateDifference(DateTime checkedOutDate, DateTime returnedDate)
        {
            //count formula : n-r+1 
            double diff = (returnedDate - checkedOutDate).TotalDays+1;
            return diff;
        }
    }
}