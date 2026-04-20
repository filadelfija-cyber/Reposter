using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using MoreLinq;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace odnoklassniki_selenium;

internal class Program
{
	private static HashSet<string> processedGroups = new HashSet<string>();

	public static ILog logger;

	private static readonly Settings settings = ReadSettings();

	private static Random rnd = new Random();

	private static List<string> groupsToRemoveFromSharing = new List<string>();

	private static List<string> groupNames = new List<string>();

	private static Dictionary<string, string> groupDictionary = new Dictionary<string, string>();

	private static Dictionary<string, string> friendsDictionary = new Dictionary<string, string>();

	private static HashSet<string> groupNamesHash = new HashSet<string>();

	private static string shareBtnSelector = "div.feed-w:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > ul:nth-child(2) >li:nth-child(2)>div>div>button";

	public static string profileID;

	private static Statistics stat = new Statistics();

	private static Statistics loadedStatistics;

	internal static BotStatus status = new BotStatus();

	internal static ChromeDriver driver;

	private static async Task Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		logger = ConfigureLogging();
		if (!settings.UseBot)
		{
			await StartPromoting();
		}
	}

	public static async Task StartPromoting()
	{
		Stopwatch sw = Stopwatch.StartNew();
		logger.Info((object)$"Версия программы: {Assembly.GetExecutingAssembly().GetName().Version}");
		logger.Info((object)$"Программа начала свою работу. {DateTime.Now}");
		if (status.IsStarted && status.ShowStatistics)
		{
		}
		stat.PromotionStartedDateTime = DateTime.Now;
		stat.SermonSource = settings.PageForSharing;
		stat.ProcessedAccounts = new List<ProcessedAccount>();
		if (File.Exists(Path.Combine(AssemblyDirectory(), "processedGroups.json")))
		{
			string jsonText = File.ReadAllText(Path.Combine(AssemblyDirectory(), "processedGroups.json"));
			processedGroups = JsonConvert.DeserializeObject<HashSet<string>>(jsonText);
		}
		if (!Directory.Exists(Path.Combine(AssemblyDirectory(), "statistics")))
		{
			Directory.CreateDirectory(Path.Combine(AssemblyDirectory(), "statistics"));
		}
		loadedStatistics = LoadIntermediateResultFromDisk();
		groupsToRemoveFromSharing.Add("Одноклассники API");
		groupsToRemoveFromSharing.Add("Монархия РПЦ Царской Империи!");
		groupsToRemoveFromSharing.Add("Русский патриот. ☦РПЦ Царской Империи!");
		groupsToRemoveFromSharing.Add("Заблокированная группа");
		ChromeOptions options = new ChromeOptions();
		options.AddUserProfilePreference("profile.content_settings.exceptions.clipboard", new Dictionary<string, object> { ["profile.content_settings.exceptions.clipboard"] = new Dictionary<string, object> { 
		{
			"*",
			new Dictionary<string, object> { { "setting", 1 } }
		} } }["profile.content_settings.exceptions.clipboard"]);
		options.PageLoadTimeout = TimeSpan.FromSeconds(60.0);
		if (settings.PageLoadStrategyEager)
		{
			options.PageLoadStrategy = PageLoadStrategy.Eager;
		}
		if (settings.Headless)
		{
			options.AddArgument("--headless");
		}
		else
		{
			options.AddArgument("--start-maximized");
		}
		if (settings.KioskMode)
		{
			options.AddArgument("--kiosk");
		}
		if (settings.DisableWebDriverLogging)
		{
			options.AddArgument("--disable-logging");
			options.AddArgument("--log-level=3");
		}
		options.AddArgument("--user-data-dir=C:\\Users\\user\\AppData\\Local\\Google\\Chrome\\User Data\\Profile 1");
		options.AddArgument("--profile-directory=Profile 1");
		if (driver == null)
		{
			driver = new ChromeDriver(options);
		}
		logger.Info((object)$"Удаление данных куки...Сейчас имеется {driver.Manage().Cookies.AllCookies.Count}");
		driver.Manage().Cookies.DeleteAllCookies();
		Dictionary<string, object> param = new Dictionary<string, object>();
		driver.ExecuteCdpCommand("Storage.clearCookies", param);
		logger.Info((object)$"Куки удалены...Сейчас имеется {driver.Manage().Cookies.AllCookies.Count}");
		while (true)
		{
			logger.Info((object)"Попытка перейти на основную страницу...");
			try
			{
				await driver.GoToUrl("https://www.ok.ru");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				logger.Error((object)ex2);
				continue;
			}
			break;
		}
		try
		{
			foreach (Account account in settings.Accounts)
			{
				if (status.IsStopping)
				{
					break;
				}
				new Dictionary<string, object>();
				ProcessedAccount currentlyProcessedAccount = new ProcessedAccount
				{
					Name = account.Login
				};
				currentlyProcessedAccount.Groups = new List<ProcessedGroup>();
				currentlyProcessedAccount.FaultyGroups = new List<ProcessedGroup>();
				while (await CheckLoading(driver))
				{
					logger.Info((object)"Наблюдаются проблемы с интернетом. Проверьте подключение...");
				}
				stat.CurrentlyProcessedAccount = account.Login;
				logger.Info((object)$"Работа с аккаунтом '{account.Login}' {stat.ProcessedAccountCount}/{settings.Accounts.Count}");
				if (status.IsStarted && status.ShowStatistics)
				{
				}
				IWebElement loginTextBox = driver.ClickElement(By.CssSelector("#field_email"), 20);
				loginTextBox.Clear();
				loginTextBox.SendKeys(account.Login);
				IWebElement passwordTextBox = driver.ClickElement(By.CssSelector("#field_password"), 20);
				passwordTextBox.Clear();
				passwordTextBox.SendKeys(account.Password);
				driver.ClickElement(By.CssSelector(":is(form > div.login-form-actions > input,button[label = 'Войти'])"), 20);
				await Task.Delay(5000);
				string isPageExist = driver.Find(By.CssSelector("isbody > div > h1"))?.Text;
				if (isPageExist == "Этой страницы нет в OK" || driver.Url.TrimEnd('/') != "https://ok.ru")
				{
					logger.Info((object)driver.Url);
					if (status.IsStarted && status.ShowStatistics)
					{
					}
					await driver.GoToUrl("https://ok.ru");
					stat.ProcessedAccountCount++;
					stat.ProcessedAccounts.Add(currentlyProcessedAccount);
					continue;
				}
				string errText = driver.Find(By.CssSelector("form>div:nth-child(2)>span:nth-child(2)"))?.Text;
				if (errText != null)
				{
					logger.Error((object)errText);
					if (status.IsStarted && status.ShowStatistics)
					{
					}
					await driver.GoToUrl("https://ok.ru");
					stat.ProcessedAccountCount++;
					stat.ProcessedAccounts.Add(currentlyProcessedAccount);
					continue;
				}
				try
				{
					IWebElement isAccountLocked = driver.Find(By.CssSelector(".stub-empty_t"));
					if (isAccountLocked != null && isAccountLocked.Text == "Ваш профиль заблокирован за нарушение правил пользования сайтом")
					{
						logger.Info((object)("Аккаунт " + account.Login + " заблокирован!"));
						if (status.IsStarted && status.ShowStatistics)
						{
						}
						await driver.GoToUrl("https://www.ok.ru");
						stat.ProcessedAccountCount++;
						stat.ProcessedAccounts.Add(currentlyProcessedAccount);
						continue;
					}
				}
				catch (Exception ex3)
				{
					logger.Error((object)ex3);
					await driver.GoToUrl("https://www.ok.ru");
					stat.ProcessedAccountCount++;
					stat.ProcessedAccounts.Add(currentlyProcessedAccount);
					continue;
				}
				try
				{
					driver.Find(By.CssSelector("div > button[aria-label='Закрыть'][class*='modal_close']"))?.Click();
				}
				catch (Exception ex4)
				{
					Exception ex5 = ex4;
					logger.Debug((object)ex5);
				}
				if (settings.ShareInGroups)
				{
					try
					{
						await ShareNewsInGroups(driver, settings.PageForSharing, currentlyProcessedAccount);
					}
					catch (Exception ex4)
					{
						Exception ex6 = ex4;
						logger.Error((object)ex6);
					}
				}
				if (settings.ShareInFriends)
				{
					await ShareNewsForFriends(driver, settings.PageForSharing, currentlyProcessedAccount);
				}
				if (settings.ShareInGroupsDirectly)
				{
					await ShareInGroupsDirectly(driver, settings.PageForSharing, currentlyProcessedAccount);
				}
				try
				{
					driver.Find(By.CssSelector(".modal-new_close_ico"))?.Click();
				}
				catch (Exception ex7)
				{
					logger.Debug((object)ex7);
				}
				await Task.Delay(rnd.Next(500, 2000));
				try
				{
					logger.Info((object)$"Завершена работа с аккаунтом <{account.Login}>... Обработано {stat.ProcessedAccountCount} из {settings.Accounts.Count}");
					if (status.IsStarted && status.ShowStatistics)
					{
					}
					logger.Info((object)$"Удаление данных куки...Сейчас имеется {driver.Manage().Cookies.AllCookies.Count}");
					driver.Manage().Cookies.DeleteAllCookies();
					Dictionary<string, object> parameters = new Dictionary<string, object>();
					driver.ExecuteCdpCommand("Storage.clearCookies", parameters);
					logger.Info((object)$"Куки удалены...Сейчас имеется {driver.Manage().Cookies.AllCookies.Count}");
					await driver.GoToUrl("https://ok.ru");
					driver.Navigate().Refresh();
				}
				catch (Exception ex8)
				{
					logger.Error((object)"Неудачная попытка перейти на другой аккаунт.", ex8);
				}
				stat.ProcessedAccountCount++;
				stat.ProcessedAccounts.Add(currentlyProcessedAccount);
			}
		}
		catch (Exception ex4)
		{
			Exception ex9 = ex4;
			logger.Error((object)ex9);
		}
		finally
		{
			File.WriteAllText(contents: JsonConvert.SerializeObject((object)stat, (Formatting)1), path: Path.Combine(AssemblyDirectory(), "statistics", $"statistics{DateTime.Now:_yyyy_MM_dd__HH-mm_}.json"));
			status.IsStopping = false;
			status.IsStarted = false;
			stat.PromotionEndedDateTime = DateTime.Now;
			SaveIntermediateResultOnDisk();
		}
		logger.Info((object)$"Программа отработала.  {DateTime.Now}");
		logger.Info((object)$"Время работы программы: {sw.Elapsed}");
		if (status.IsStarted && status.ShowStatistics)
		{
		}
		ShowStatistics(stat);
		stat.ProcessedAccountCount = 0;
		_ = settings.RetryOnFaultyRun;
		if (settings.ShutdownAfterFinish)
		{
			ShutDownPC();
		}
		else if (settings.ExitAfterFinish)
		{
			driver.Quit();
		}
	}

	private static void ShowStatistics(Statistics stat)
	{
	}

	private static async Task<bool> CheckLoading(IWebDriver driver)
	{
		string loadingModalSelector = "#hook_Block_LayerLoader > layer-loader > div > div > div[aria-label=\"Загрузка...\"]";
		IWebElement loadModal = driver.Find(By.CssSelector(loadingModalSelector), 2, logError: false);
		if (loadModal != null)
		{
			await Task.Delay(10000);
			loadModal = driver.Find(By.CssSelector(loadingModalSelector));
			if (loadModal != null)
			{
				logger.Error((object)"Модальное окно загрузки не NULL. Проблемы с загрузкой контента! Проверьте интернет-соединение.");
				await driver.GoToUrl(settings.PageForSharing);
				return true;
			}
		}
		return false;
	}

	private static async Task ShareInGroupsDirectly(IWebDriver driver, string newsFromGroup, ProcessedAccount currentlyProcessedAccount)
	{
		if (status.IsStopping)
		{
			return;
		}
		if (status.ShowStatistics)
		{
		}
		IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
		IWebElement element = driver.FindElement(By.CssSelector("body"));
		string attribPID = element.GetAttribute("data-l");
		string pid = attribPID.Split(',').ToList().Last();
		logger.Info((object)("Profile ID:" + pid));
		string groupsPageUrl = "https://ok.ru/profile/" + pid + "/groups/mine";
		await driver.GoToUrl(groupsPageUrl);
		js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_PopularGroupsListBlock\");if (elementToRemove){elementToRemove.remove();}");
		WebDriverWait wait33 = new WebDriverWait(driver, TimeSpan.FromSeconds(10.0));
		IWebElement groupCountAsText = wait33.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("span.filter_count")));
		int groupCount = int.Parse(groupCountAsText.Text);
		while (groupNamesHash.Count != groupCount)
		{
			bool groupIncreased = false;
			js.ExecuteScript("window.scrollBy(0,400)");
			ReadOnlyCollection<IWebElement> groupsList = driver.FindElements(By.CssSelector("#hook_Loader_UserGroupsSectionBlockLoader > div > div > div> div:nth-child(2) > a"));
			if (!groupsList.Any())
			{
				groupsList = driver.FindElements(By.CssSelector("div > div > div > div > div > div > div.caption > a"));
			}
			foreach (IWebElement item in groupsList)
			{
				string groupID = item.GetDomAttribute("hrefattrs").Split('&').First((string e) => e.Contains("groupId"))
					.Split('=')[1];
				if (groupNamesHash.Add(groupID))
				{
					groupIncreased = true;
					groupDictionary.Add(groupID, item.Text);
					logger.Info((object)item.Text);
				}
				while (true)
				{
					IWebElement showMoreBtn = driver.Find(By.CssSelector("#hook_Loader_UserGroupsSectionBlockLoader > div.loader-controls.loader-controls-bottom > a"));
					try
					{
						if (showMoreBtn?.Displayed ?? false)
						{
							showMoreBtn.Click();
						}
					}
					catch (Exception ex)
					{
						Exception ex2 = ex;
						logger.Error((object)ex2);
						continue;
					}
					break;
				}
			}
			if (!groupIncreased)
			{
				break;
			}
		}
		groupNames = groupDictionary.Values.ToList();
		foreach (string item2 in groupsToRemoveFromSharing)
		{
			groupNames.Remove(item2);
		}
		foreach (string item3 in processedGroups)
		{
			groupNames.Remove(item3);
		}
		await driver.GoToUrl(newsFromGroup);
		string text = driver.Find(By.CssSelector("#hook_Block_UserProfileInfo > div > span > h1"))?.Text;
		if (text == "Заблокированный пользователь")
		{
			logger.Info((object)("Заблокировали страницу '" + newsFromGroup + "'. Нужно изменить в настройках на действительную."));
			status.IsStopping = true;
			return;
		}
		js.ExecuteScript("window.scrollBy(0,100)");
		js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_TipBlock\");if (elementToRemove){elementToRemove.remove();}");
		groupDictionary = groupDictionary.RemoveItemsByValue(groupsToRemoveFromSharing);
		HandleSermonTitleAndId(driver);
		currentlyProcessedAccount.TotalGroupCount = groupDictionary.Count;
		js.ExecuteScript("window.scrollBy(0,400)");
		try
		{
			bool linkCopied = false;
			int attempts = 0;
			while (!linkCopied && !status.IsStopping && attempts != 10)
			{
				string imagePreviewSelector = "div > div.feed-list > div:nth-child(1) > div > div.feed_cnt > div.feed_b > div > div > div.media-video";
				driver.ClickElement(By.CssSelector(imagePreviewSelector));
				driver.ClickElement(By.CssSelector(imagePreviewSelector));
				IWebElement videoLink = driver.Find(By.CssSelector("button.html5-vpl_silent.__visible"));
				if (videoLink == null)
				{
					videoLink = driver.Find(By.CssSelector("button:nth-child(2)>div.html5-vpl_ac_i_t"));
				}
				videoLink?.Click();
				string videoLinkTabSelector = "button.html5-vpl_tab:nth-child(1)";
				IWebElement el2 = driver.ClickElement(By.CssSelector(videoLinkTabSelector), 1, 3);
				string copyVideoLinkSelector = "button.html5-vpl_btn:nth-child(2)";
				IWebElement el3 = driver.ClickElement(By.CssSelector(copyVideoLinkSelector), 1, 3);
				if (el2 != null && el3 != null)
				{
					linkCopied = true;
				}
				if (attempts == 10)
				{
					logger.Error((object)"Невозможно копировать ссылку на видео! Отмена дальнейших действий...");
					status.IsStopping = true;
					break;
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex3 = ex;
			logger.Error((object)ex3);
		}
		ProcessedAccount loadedAccount = null;
		if (loadedStatistics != null)
		{
			loadedAccount = loadedStatistics.ProcessedAccounts.FirstOrDefault((ProcessedAccount a) => a.Name == currentlyProcessedAccount.Name);
		}
		if (loadedStatistics == null || (loadedStatistics != null && loadedStatistics.SermonID != stat.SermonID) || (loadedStatistics != null && loadedStatistics.SermonID == stat.SermonID && loadedAccount == null) || (loadedStatistics != null && loadedStatistics.SermonID == stat.SermonID && loadedAccount != null && !loadedAccount.IsFullyProcessed))
		{
			foreach (KeyValuePair<string, string> group in groupDictionary)
			{
				if (status.IsStopping)
				{
					break;
				}
				if (loadedAccount != null && loadedAccount.Groups.FirstOrDefault((ProcessedGroup g) => g.Id == group.Key) != null)
				{
					logger.Info((object)("Пропуск группы https://ok.ru/group/" + group.Key));
					continue;
				}
				await driver.GoToUrl("https://ok.ru/group/" + group.Key);
				driver.ClickElement(By.CssSelector(".pf-head_itx_a"), 3, 2);
				IWebElement createNewPost = driver.ClickElement(By.CssSelector(".posting_itx > div:nth-child(1)"), 3, 2);
				await Task.Delay(2000);
				new Actions(driver).KeyDown(Keys.Control).SendKeys("v").KeyUp(Keys.Control)
					.Perform();
				await Task.Delay(9000);
				IWebElement share = driver.ClickElement(By.CssSelector(":is(button[title='Поделиться'],button[title='На модерацию'])"), 3, 2);
				await Task.Delay(1000);
				IWebElement isErrorHappened = driver.Find(By.CssSelector("div[class='posting_e js-submit-error']"));
				IWebElement shareThatMustbeNull = driver.Find(By.CssSelector(":is(button[title='Поделиться'],button[title='На модерацию'])"));
				if (isErrorHappened != null || shareThatMustbeNull != null)
				{
					logger.Error((object)("Проблемы при публикации в группе '" + group.Value + "' (https://ok.ru/group/" + group.Key + ")"));
					if (!status.IsStarted || !status.ShowStatistics)
					{
					}
				}
				if (createNewPost != null && share != null && isErrorHappened == null && shareThatMustbeNull == null)
				{
					currentlyProcessedAccount.Groups.Add(new ProcessedGroup
					{
						Id = group.Key,
						Name = group.Value,
						ProcessedDate = DateTime.Now
					});
				}
				else
				{
					currentlyProcessedAccount.FaultyGroups.Add(new ProcessedGroup
					{
						Id = group.Key,
						Name = group.Value,
						ProcessedDate = DateTime.Now,
						Message = "createPost or share button is null."
					});
				}
				if (createNewPost != null && share != null && isErrorHappened == null && shareThatMustbeNull == null)
				{
					logger.Info((object)$"Успешно опубликовано в группе '{group.Value}' (https://ok.ru/group/{group.Key}). {currentlyProcessedAccount.Groups.Count}/{currentlyProcessedAccount.TotalGroupCount}");
					if (!status.IsStarted || !status.ShowStatistics)
					{
					}
				}
			}
			if (!status.IsStopping)
			{
				currentlyProcessedAccount.IsFullyProcessed = true;
			}
		}
		else
		{
			logger.Info((object)("Аккаунт <" + currentlyProcessedAccount.Name + "> обработан в предыдущий раз. Пропуск."));
			if (!status.IsStarted || !status.ShowStatistics)
			{
			}
		}
	}

	private static void HandleSermonTitleAndId(IWebDriver driver)
	{
		IWebElement sermonTitleWeb = driver.Find(By.CssSelector("div.feed-list > div:nth-child(1) > div > div.feed_cnt > div.feed_b > div > div > div.media-video > div > div.vid-card_cnt"));
		if (sermonTitleWeb == null)
		{
			return;
		}
		try
		{
			stat.SermonID = sermonTitleWeb.GetDomAttribute("data-movie-id");
			stat.SermonTitle = driver.FindElements(By.CssSelector(".video-card_n-w>a"))[0].GetDomAttribute("title");
			if (!string.IsNullOrEmpty(stat.SermonTitle) && !string.IsNullOrEmpty(stat.SermonID))
			{
				logger.Info((object)("Продвижение проповеди:'" + stat.SermonTitle + "' Id:" + stat.SermonID));
				if (!status.ShowStatistics)
				{
				}
			}
			else
			{
				logger.Info((object)"Невозможно получить название и id проповеди.");
			}
		}
		catch (Exception ex)
		{
			logger.Error((object)ex);
		}
	}

	private static async Task ShareNewsForFriends(IWebDriver driver, string newsFromGroup, ProcessedAccount currentlyProcessedAccount)
	{
		if (status.IsStopping)
		{
			return;
		}
		if (status.ShowStatistics)
		{
		}
		currentlyProcessedAccount.Friends = new List<KeyValuePair<string, string>>();
		IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
		IWebElement element = driver.FindElement(By.CssSelector("body"));
		string attribPID = element.GetAttribute("data-l");
		logger.Info((object)attribPID);
		string pid = attribPID.Split(',').Last();
		string friendsPageUrl = "https://ok.ru/profile/" + pid + "/friends";
		await driver.GoToUrl(friendsPageUrl);
		js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_MyFriendsNewPageMRB > div:nth-child(1) > div:nth-child(2)\");if (elementToRemove){elementToRemove.remove();}");
		js.ExecuteScript("const elementToRemove2 = document.querySelector(\"div[class^='friends-promo-banner-portlet']\");if (elementToRemove2){elementToRemove2.remove();}");
		js.ExecuteScript("const elementToRemove3 = document.querySelector(\".portlet_b\");if (elementToRemove3){elementToRemove3.remove();}");
		HashSet<string> friends = new HashSet<string>();
		new Dictionary<string, string>();
		string friendsCountAsText = driver.FindElement(By.CssSelector("a.mctc_navMenuSec:nth-child(1) > span:nth-child(1)")).Text;
		friendsCountAsText = friendsCountAsText.Replace(" ", "");
		int friendsCount = int.Parse(friendsCountAsText);
		while (friendsCount != friends.Count)
		{
			bool friendsCountIncreased = false;
			js.ExecuteScript("window.scrollBy(0,400)");
			ReadOnlyCollection<IWebElement> friendList = driver.FindElements(By.CssSelector("li.ugrid_i > div:nth-child(1) > div > div:nth-child(1) > a:nth-child(1)"));
			foreach (IWebElement item in friendList)
			{
				string friendID = item.GetDomAttribute("href").Split('/').Last();
				if (friends.Add(item.Text))
				{
					logger.Info((object)("\t Имя: " + item.Text + ", ID: " + friendID));
					friendsCountIncreased = true;
					friendsDictionary.Add(friendID, item.Text);
				}
			}
			if (!friendsCountIncreased)
			{
				break;
			}
		}
		await driver.GoToUrl(newsFromGroup);
		string text = driver.Find(By.CssSelector("#hook_Block_UserProfileInfo > div > span > h1"))?.Text;
		if (text == "Заблокированный пользователь")
		{
			logger.Info((object)("Заблокировали страницу '" + newsFromGroup + "'. Нужно изменить в настройках на действительную."));
			status.IsStopping = true;
			return;
		}
		js.ExecuteScript("window.scrollBy(0,100)");
		js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_TipBlock\");if (elementToRemove){elementToRemove.remove();}");
		await Task.Delay(rnd.Next(1500, 3000));
		string shareBtnSelector2 = "div> div.feed-list > div:nth-child(1) > div > div:nth-child(2)>div:nth-child(3)>ul>li:nth-child(2)>div>div>button";
		IEnumerable<IEnumerable<KeyValuePair<string, string>>> batchedFriendLists = friendsDictionary.Batch(30);
		foreach (IEnumerable<KeyValuePair<string, string>> batch in batchedFriendLists)
		{
			driver.ClickElement(By.CssSelector(shareBtnSelector2));
			driver.ClickElement(By.CssSelector("div[id^=\"block_ShortcutMenu_null\"] > ul > div > a:nth-child(3) > div"));
			List<KeyValuePair<string, string>> tempFriendsList = new List<KeyValuePair<string, string>>();
			foreach (KeyValuePair<string, string> friendItem in batch)
			{
				driver.ClickElement(By.CssSelector("#reshare\\.wfid-input"));
				js.ExecuteScript("const element = document.querySelector(\"input[id='reshare.wfid-input']\");if (element){element.value += '" + friendItem.Value.Trim() + "';}");
				await Task.Delay(rnd.Next(500, 1000));
				await Task.Delay(rnd.Next(500, 1000));
				driver.ClickElement(By.CssSelector("li>.__selected"));
				driver.ClickElement(By.CssSelector("div[aria-label='Сервисные уведомления']>.tag_del_w>i[title='Удалить']"), 1, 1);
				tempFriendsList.Add(friendItem);
			}
			driver.ClickElement(By.CssSelector("button[title='Поделиться']"));
			currentlyProcessedAccount.Friends.AddRange(tempFriendsList);
			await Task.Delay(rnd.Next(1500));
		}
		friends.Clear();
		friendsDictionary.Clear();
	}

	private static async Task ShareNewsInGroups(IWebDriver driver, string newsFromGroup, ProcessedAccount currentlyProcessedAccount)
	{
		if (status.IsStopping)
		{
			return;
		}
		if (status.ShowStatistics)
		{
		}
		IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
		string pid = driver.FindElement(By.CssSelector("body")).GetAttribute("data-l").Split(',')
			.ToList()
			.Last();
		logger.Info((object)("Profile ID:" + pid));
		string groupsPageUrl = "https://ok.ru/profile/" + pid + "/groups/mine";
		await driver.GoToUrl(groupsPageUrl);
		js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_PopularGroupsListBlock\");if (elementToRemove){elementToRemove.remove();}");
		WebDriverWait wait33 = new WebDriverWait(driver, TimeSpan.FromSeconds(10.0));
		IWebElement groupCountAsText = wait33.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("span.filter_count")));
		int groupCount = int.Parse(groupCountAsText.Text);
		while (groupNamesHash.Count != groupCount)
		{
			bool groupIncreased = false;
			js.ExecuteScript("window.scrollBy(0,400)");
			ReadOnlyCollection<IWebElement> groupsList = driver.FindElements(By.CssSelector("#hook_Loader_UserGroupsSectionBlockLoader > div > div > div> div:nth-child(2) > a"));
			if (!groupsList.Any())
			{
				groupsList = driver.FindElements(By.CssSelector("#listBlockPanelUserGroupsSectionBlock > div > div > div > div > div > div.caption > a"));
			}
			foreach (IWebElement item in groupsList)
			{
				string groupID = item.GetDomAttribute("hrefattrs").Split('&').First((string e) => e.Contains("groupId"))
					.Split('=')[1];
				if (groupNamesHash.Add(groupID))
				{
					groupIncreased = true;
					groupDictionary.Add(groupID, item.Text);
					logger.Info((object)item.Text);
				}
				while (true)
				{
					IWebElement showMoreBtn = driver.Find(By.CssSelector("#hook_Loader_UserGroupsSectionBlockLoader > div.loader-controls.loader-controls-bottom > a"));
					try
					{
						if (showMoreBtn?.Displayed ?? false)
						{
							showMoreBtn.Click();
						}
					}
					catch (Exception ex)
					{
						Exception ex2 = ex;
						logger.Error((object)ex2);
						continue;
					}
					break;
				}
			}
			if (!groupIncreased)
			{
				break;
			}
		}
		groupNames = groupDictionary.Values.ToList();
		foreach (string item2 in groupsToRemoveFromSharing)
		{
			groupNames.Remove(item2);
		}
		foreach (string item3 in processedGroups)
		{
			groupNames.Remove(item3);
		}
		await driver.GoToUrl(newsFromGroup);
		string text = driver.Find(By.CssSelector("#hook_Block_UserProfileInfo > div > span > h1"))?.Text;
		if (text == "Заблокированный пользователь")
		{
			logger.Info((object)("Заблокировали страницу '" + newsFromGroup + "'. Нужно изменить в настройках на действительную."));
			status.IsStopping = true;
			return;
		}
		js.ExecuteScript("window.scrollBy(0,100)");
		js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_TipBlock\");if (elementToRemove){elementToRemove.remove();}");
		groupDictionary = groupDictionary.RemoveItemsByValue(groupsToRemoveFromSharing);
		int groupNumber = 0;
		IWebElement sermonTitleWeb = driver.Find(By.CssSelector("div.feed-list > div:nth-child(1) > div > div.feed_cnt > div.feed_b > div > div > div > div > div.video-card_n-w>a"));
		if (sermonTitleWeb == null)
		{
			sermonTitleWeb = driver.Find(By.CssSelector("div > div.feed-list > div:nth-child(1) > div > div.feed_cnt > div.feed_b > div > div > div.media-block.media-text > div > div"));
		}
		if (sermonTitleWeb == null)
		{
			sermonTitleWeb = driver.Find(By.CssSelector("div.feed-list > div:nth-child(1) > div > div.feed_cnt > div.feed_b > div > div > div.media-video > div > div.vid-card_cnt"));
		}
		if (sermonTitleWeb != null)
		{
			stat.SermonTitle = sermonTitleWeb.GetDomAttribute("title");
			stat.SermonID = sermonTitleWeb.GetDomAttribute("href");
		}
		ProcessedAccount loadedAccount = null;
		if (loadedStatistics != null)
		{
			loadedAccount = loadedStatistics.ProcessedAccounts.FirstOrDefault((ProcessedAccount a) => a.Name == currentlyProcessedAccount.Name);
		}
		if (loadedStatistics == null || (loadedStatistics != null && loadedStatistics.SermonID != stat.SermonID) || (loadedStatistics != null && loadedStatistics.SermonID == stat.SermonID && loadedAccount == null) || (loadedStatistics != null && loadedStatistics.SermonID == stat.SermonID && loadedAccount != null && !loadedAccount.IsFullyProcessed))
		{
			currentlyProcessedAccount.TotalGroupCount = groupDictionary.Count;
			foreach (KeyValuePair<string, string> group in groupDictionary)
			{
				if (loadedAccount != null && loadedAccount.Groups.FirstOrDefault((ProcessedGroup g) => g.Id == group.Key) != null)
				{
					continue;
				}
				int shareRetryCount = 0;
				groupNumber++;
				stat.CurrentlyProcessedGroup = group;
				Console.ForegroundColor = ConsoleColor.Yellow;
				logger.Info((object)$"{groupNumber}/{groupDictionary.Count} '{group.Value}'");
				while (true)
				{
					Console.ResetColor();
					shareRetryCount++;
					if (shareRetryCount == 10)
					{
						logger.Info((object)"10 неудачных попыток при опубликовании проповеди. Выход из цикла для списка групп...");
						if (await IsConnectedToInternetAsync())
						{
							await CheckLoading(driver);
							await driver.GoToUrl(settings.PageForSharing);
						}
						currentlyProcessedAccount.FaultyGroups.Add(new ProcessedGroup
						{
							Id = group.Key,
							Name = group.Value,
							ProcessedDate = DateTime.Now,
							Message = "10 неудачных попыток при опубликовании проповеди.Выход из цикла для списка групп..."
						});
						break;
					}
					try
					{
						logger.Info((object)("\n\t<" + stat.CurrentlyProcessedAccount + "> Попытка опубликовать в группе '" + group.Value + "' ..."));
						string shareBtnSelector = "button[aria-label=\"Поделиться\"]";
						WebDriverWait wait34 = new WebDriverWait(driver, TimeSpan.FromSeconds(10.0));
						IWebElement shareBtn = wait34.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(shareBtnSelector)));
						await Task.Delay(1000);
						if (driver.Url.Contains("anonymMain"))
						{
							logger.Info((object)"Выбило из аккаунта. Переход к следующему аккаунту.");
							break;
						}
						if (shareBtn == null)
						{
							logger.Info((object)("Элемент не найден: " + shareBtnSelector));
							shareBtnSelector = "div > div.feed-list > div:nth-child(2) > div > div.feed_cnt > div.feed_f > ul > li:nth-child(2) > div > div > button";
							shareBtn = wait34.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(shareBtnSelector)));
						}
						shareBtn.Click();
					}
					catch (ElementClickInterceptedException ex3)
					{
						logger.Error((object)(ex3.Message + "\n" + HandleStackTraceString(ex3.StackTrace)));
						if (settings.TakeScreenShotsOnErrors)
						{
							driver.TakeScreenshot();
						}
						IWebElement errMessagePopUp = driver.Find(By.CssSelector("#reshare > div.posting_content-actions-w > div > div > div > div > div.posting_content-actions_floating > div[class='posting_e js-submit-error']"));
						if (errMessagePopUp != null)
						{
							if (settings.TakeScreenShotsOnErrors)
							{
								driver.TakeScreenshot();
							}
							ReadOnlyCollection<IWebElement> twoSpans = errMessagePopUp.FindElements(By.TagName("span"));
							logger.Error((object)("Сообщение об ошибке:'" + twoSpans[0].Text + "'"));
							currentlyProcessedAccount.FaultyGroups.Add(new ProcessedGroup
							{
								Id = group.Key,
								Name = group.Value,
								ProcessedDate = DateTime.Now,
								Message = twoSpans[0].Text
							});
							if (twoSpans[0].Text == "Пожалуйста, укажите группу")
							{
								logger.Info((object)"Не найдена группа для рассылки. Переход к следующей...");
								try
								{
									js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_TipBlock\");if (elementToRemove){elementToRemove.remove();}");
								}
								catch (Exception ex)
								{
									Exception ex4 = ex;
									logger.Error((object)ex4);
								}
								await Task.Delay(2000);
							}
							else
							{
								while (true)
								{
									IWebElement closePopUpButton = driver.Find(By.CssSelector("#hook_Block_PopLayerOver > div > div.modal-new_hld > div.modal-new_close > span"));
									try
									{
										closePopUpButton?.Click();
									}
									catch (StaleElementReferenceException ex5)
									{
										StaleElementReferenceException ex6 = ex5;
										logger.Error((object)ex6);
										continue;
									}
									break;
								}
							}
							goto IL_18ad;
						}
						while (true)
						{
							IWebElement closePopUpButton2 = driver.Find(By.CssSelector("#hook_Block_PopLayerOver > div > div.modal-new_hld > div.modal-new_close > span"));
							try
							{
								closePopUpButton2?.Click();
							}
							catch (StaleElementReferenceException ex5)
							{
								StaleElementReferenceException ex7 = ex5;
								logger.Error((object)ex7);
								continue;
							}
							break;
						}
						continue;
					}
					catch (StaleElementReferenceException ex8)
					{
						logger.Error((object)(ex8.Message + "\n" + HandleStackTraceString(ex8.StackTrace)));
						if (settings.TakeScreenShotsOnErrors)
						{
							driver.TakeScreenshot();
						}
						continue;
					}
					catch (WebDriverTimeoutException ex9)
					{
						logger.Error((object)ex9);
						if (await CheckLoading(driver))
						{
							await Task.Delay(10000);
						}
						continue;
					}
					catch (Exception ex10)
					{
						logger.Error((object)ex10);
						if (settings.TakeScreenShotsOnErrors)
						{
							driver.TakeScreenshot();
						}
					}
					try
					{
						WebDriverWait wait35 = new WebDriverWait(driver, TimeSpan.FromSeconds(10.0));
						IWebElement shareInGroups = wait35.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("div[id^=\"block_ShortcutMenu_null\"] > ul > div > a:nth-child(6) > div")));
						shareInGroups.Click();
					}
					catch (Exception ex11)
					{
						logger.Error((object)ex11);
						continue;
					}
					while (true)
					{
						await Task.Delay(rnd.Next(1500, 3000));
						try
						{
							IWebElement shareBtn2 = driver.ClickElement(By.CssSelector("#reshare_XpostGroupNameInput"));
							js.ExecuteScript("const element = document.querySelector(\"#reshare_XpostGroupNameInput\");if (element){element.value = '" + group.Value.Trim() + "';}");
							await Task.Delay(rnd.Next(500, 1000));
							shareBtn2.SendKeys(" ");
							shareBtn2.SendKeys(Keys.Backspace);
						}
						catch (JavaScriptException ex12)
						{
							JavaScriptException ex13 = ex12;
							logger.Error((object)ex13);
							currentlyProcessedAccount.FaultyGroups.Add(new ProcessedGroup
							{
								Id = group.Key,
								Name = group.Value,
								ProcessedDate = DateTime.Now,
								Message = ex13.Message
							});
							break;
						}
						catch (Exception ex)
						{
							Exception ex14 = ex;
							logger.Error((object)ex14);
							continue;
						}
						await Task.Delay(rnd.Next(1500, 3000));
						while (true)
						{
							List<IWebElement> groupListTest = driver.FindElements(By.CssSelector("#reshare_XpostGroupSuggestItems > li")).ToList();
							if (!groupListTest.Any())
							{
								break;
							}
							groupListTest.RemoveAt(groupListTest.Count - 1);
							try
							{
								if (groupListTest.Count == 1)
								{
									groupListTest[0].Click();
									break;
								}
								foreach (IWebElement webElement in groupListTest)
								{
									string groupId = webElement.GetDomAttribute("id").Split('_')[2];
									if (groupId == group.Key)
									{
										webElement.Click();
										break;
									}
								}
							}
							catch (StaleElementReferenceException ex5)
							{
								StaleElementReferenceException ex15 = ex5;
								logger.Error((object)ex15);
								continue;
							}
							catch (ElementNotInteractableException ex16)
							{
								ElementNotInteractableException ex17 = ex16;
								logger.Error((object)ex17);
								continue;
							}
							break;
						}
						await Task.Delay(rnd.Next(1500, 3000));
						string sel = "#reshare > div.posting_footer.js-posting-footer.__simple.__collapsable > div > div > div > div > div.posting_f_ac > button";
						WebDriverWait wait36 = new WebDriverWait(driver, TimeSpan.FromSeconds(10.0));
						IWebElement publishToGroupButton = wait36.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(sel)));
						publishToGroupButton.Click();
						Console.ForegroundColor = ConsoleColor.Green;
						currentlyProcessedAccount.Groups.Add(new ProcessedGroup
						{
							Id = group.Key,
							Name = group.Value,
							ProcessedDate = DateTime.Now
						});
						logger.Info((object)("\tУспешно опубликовано в группе (https://ok.ru/group/" + group.Key + ")!"));
						if (status.IsStarted && status.ShowStatistics)
						{
						}
						if (status.IsStopping)
						{
							goto end_IL_1899;
						}
						Console.ResetColor();
						processedGroups.Add(group.Value);
						js.ExecuteScript("const elementToRemove = document.querySelector(\"#hook_Block_TipBlock\");if (elementToRemove){elementToRemove.remove();}");
						break;
					}
					goto IL_18ad;
					continue;
					end_IL_1899:
					break;
				}
				break;
				IL_18ad:;
			}
		}
		groupNamesHash.Clear();
		groupNames.Clear();
		groupDictionary.Clear();
	}

	private static void SaveIntermediateResultOnDisk()
	{
		string interContent = JsonConvert.SerializeObject((object)stat);
		File.WriteAllText(Path.Combine(AssemblyDirectory(), "save.json"), interContent);
	}

	private static Statistics LoadIntermediateResultFromDisk()
	{
		string savePath = Path.Combine(AssemblyDirectory(), "save.json");
		if (File.Exists(savePath))
		{
			string save = File.ReadAllText(savePath);
			return JsonConvert.DeserializeObject<Statistics>(save);
		}
		return null;
	}

	private static Statistics CheckIntermediateResult(Statistics intermediateStat)
	{
		_ = intermediateStat.SermonTitle != stat.SermonTitle;
		return null;
	}

	private static Settings ReadSettings()
	{
		string settingsAsJsonText = File.ReadAllText(Path.Combine(AssemblyDirectory(), "settings.json"));
		return JsonConvert.DeserializeObject<Settings>(settingsAsJsonText);
	}

	public static string AssemblyDirectory()
	{
		string codeBase = Assembly.GetExecutingAssembly().CodeBase;
		UriBuilder uri = new UriBuilder(codeBase);
		string path = Uri.UnescapeDataString(uri.Path);
		return Path.GetDirectoryName(path);
	}

	private static ILog ConfigureLogging(bool showLogInConsole = true)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		XmlConfigurator.Configure();
		ILog logger = LogManager.GetLogger(typeof(Program));
		if (!showLogInConsole)
		{
			Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
			IAppender consoleAppender = hierarchy.Root.GetAppender("ConsoleAppender");
			if (consoleAppender != null)
			{
				hierarchy.Root.RemoveAppender(consoleAppender);
			}
			((LoggerRepositorySkeleton)hierarchy).Configured = true;
		}
		return logger;
	}

	private static void TuneLogger(bool showLogInConsole = true)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		ILogger logger = ((ILoggerWrapper)LogManager.GetLogger(typeof(Program))).Logger;
		IAppenderAttachable appenderAttachable = (IAppenderAttachable)((logger is IAppenderAttachable) ? logger : null);
		if (appenderAttachable == null)
		{
			return;
		}
		IAppender appenderToRemove = null;
		foreach (IAppender appender in appenderAttachable.Appenders)
		{
			if (appender.Name == "ConsoleAppender")
			{
				appenderToRemove = appender;
				break;
			}
		}
		if (appenderToRemove != null && !showLogInConsole)
		{
			appenderAttachable.RemoveAppender(appenderToRemove);
		}
	}

	private static void ShutDownPC()
	{
		logger.Info((object)"Завершение работы ПК...");
		Process.Start("shutdown", "/s /f /t 0");
	}

	public static async Task<bool> IsConnectedToInternetAsync(string hostNameOrAddress = "8.8.8.8")
	{
		try
		{
			using Ping pinger = new Ping();
			return (await pinger.SendPingAsync(hostNameOrAddress, 4000)).Status == IPStatus.Success;
		}
		catch (PingException)
		{
			logger.Info((object)"Нет подключения к интернету.");
			return false;
		}
	}

	internal static string HandleStackTraceString(string stackMessage)
	{
		string[] split = stackMessage.Split(' ');
		return split[split.Length - 2] + " " + split[split.Length - 1];
	}

	internal static void SendStatisticsToBot()
	{
		throw new NotImplementedException();
	}

	internal static void StopPromoting()
	{
	}
}
