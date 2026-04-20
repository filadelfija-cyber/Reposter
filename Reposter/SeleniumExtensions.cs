using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace odnoklassniki_selenium;

internal static class SeleniumExtensions
{
	public static IWebElement Find(this IWebDriver driver, By by, int attempts = 2, bool logError = true)
	{
		IWebElement result = null;
		int attemptCount = 0;
		while (attemptCount != attempts)
		{
			attemptCount++;
			try
			{
				return driver.FindElement(by);
			}
			catch (NoSuchElementException ex)
			{
				if (logError)
				{
					Program.logger.Error((object)(by.Criteria + "\n" + ex.Message + "\n" + Program.HandleStackTraceString(ex.StackTrace)));
				}
			}
		}
		return result;
	}

	public static IWebElement ClickElement(this IWebDriver driver, By by, int waitSeconds = 10, int attempts = 6)
	{
		IWebElement webElement = null;
		int attemptsCounter = 0;
		while (true)
		{
			try
			{
				if (Program.status.IsStopping)
				{
					Program.logger.Info((object)"Остановка рассылки...");
					return null;
				}
				attemptsCounter++;
				WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitSeconds));
				webElement = wait.Until(ExpectedConditions.ElementToBeClickable(by));
				webElement.Click();
			}
			catch (Exception ex)
			{
				Program.logger.Error((object)(by.Criteria + "\n" + ex.Message + "\n" + Program.HandleStackTraceString(ex.StackTrace)));
				if (ex.InnerException != null)
				{
					Program.logger.Error((object)("\n" + ex.InnerException.Message + "\n" + Program.HandleStackTraceString(ex.InnerException.StackTrace)));
				}
				if (attemptsCounter < attempts)
				{
					continue;
				}
			}
			break;
		}
		return webElement;
	}

	public static async Task GoToUrl(this IWebDriver driver, string url)
	{
		while (true)
		{
			try
			{
				driver.Navigate().GoToUrl(url);
				break;
			}
			catch (UnknownErrorException ex) when (ex.Message.Contains("net::"))
			{
				Program.logger.Error((object)("Ошибка подключения к интернету. Попытка перейти на страницу " + url + "..."), (Exception)ex);
				await Task.Delay(30000);
			}
		}
	}

	public static void TakeScreenshot(this IWebDriver driver)
	{
		Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
		ss.SaveAsFile(Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", "screenshot_" + DateTime.Now.ToString("yyyy_MM_dd__mm_ss.ff") + ".png"));
	}

	public static void RemoveItemByValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue valueToRemove)
	{
		dictionary = dictionary.Where((KeyValuePair<TKey, TValue> kvp) => !EqualityComparer<TValue>.Default.Equals(kvp.Value, valueToRemove)).ToDictionary((KeyValuePair<TKey, TValue> kvp) => kvp.Key, (KeyValuePair<TKey, TValue> kvp) => kvp.Value);
	}

	public static Dictionary<TKey, TValue> RemoveItemsByValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, List<TValue> valuesToRemove)
	{
		return dictionary.Where((KeyValuePair<TKey, TValue> kvp) => !valuesToRemove.Contains(kvp.Value)).ToDictionary((KeyValuePair<TKey, TValue> kvp) => kvp.Key, (KeyValuePair<TKey, TValue> kvp) => kvp.Value);
	}
}
