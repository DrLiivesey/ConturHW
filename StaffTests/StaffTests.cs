using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace StaffTests;

public class Tests
{
    public IWebDriver driver;
    public WebDriverWait wait;

    [SetUp]
    public void Setup()
    {   
        driver = new ChromeDriver();
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        driver.Manage().Window.Maximize();
    }

    [TearDown]
    public void Teardown()
    {
        driver?.Quit();
        driver?.Dispose(); 
    }

    private void Authorization()
    {
        driver.Navigate().GoToUrl("https://staff-testing.testkontur.ru/");
        driver.FindElement(By.Id("Username")).SendKeys("Ruslan.Bikchurkin@urfu.me");
        driver.FindElement(By.Id("Password")).SendKeys("Ochenslognopotomychto47!");
        driver.FindElement(By.Name("button")).Click();

        wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-tid='Title']")));
    }

    private void DeletingDescriptionField()
    {
        driver.Navigate().GoToUrl("https://staff-testing.testkontur.ru/profile/settings/edit");

        var textarea = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("textarea.react-ui-r3t2bi")));
        textarea.Click();
        textarea.SendKeys(Keys.Control + "a" + Keys.Backspace);

        // Принудительно вызываем событие change
        var js = (IJavaScriptExecutor)driver;
        js.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", textarea);

        // Сохранение изменений
        wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.sc-juXuNZ.kVHSha"))).Click();
        var descriptionElements = driver.FindElements(By.XPath("//div[contains(@class, 'sc-bdnxRM') and contains(text(), 'Bikchurkin')]"));

        Assert.That(descriptionElements, Is.Empty, "Описание не было удалено");
    }


    [Test]
    // Создание папки
    public void CreateFolder()
    {
        Authorization();
        driver.Navigate().GoToUrl("https://staff-testing.testkontur.ru/files");
        
        //Добавить
        driver.FindElement(By.CssSelector(".react-ui-1h7jhqn")).Click();
        //Папку
        driver.FindElement(By.CssSelector("[data-tid='CreateFolder']")).Click();
        //Ввел название
        driver.FindElement(By.CssSelector("[data-tid='Input']")).SendKeys("Bikchurkin");
        //Сохранил
        driver.FindElement(By.CssSelector("[data-tid='SaveButton']")).Click();
        //Проверя, что папка создана
        var createdFolder = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[text()='Bikchurkin']")));

        Assert.That(createdFolder.Displayed, Is.True, "Созданная папка не отображается");
        
        //Простите, что оставил удаление на ручной труд.....
    }


    [Test]
    // Переход на страницу "Новости" при нажатии кнопки в хэдере (три головы)
    public void HeaderButtonRedirectToNewsPage()
    {
        Authorization();
        driver.Navigate().GoToUrl("https://staff-testing.testkontur.ru/files");

        //Нажатие кнопки в хэдере
        driver.FindElement(By.CssSelector("[alt='Логотип']")).Click();
        //Проверяю, что нахожусь на страницу "Новости"
        var pageTitle = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='Title']")));

        Assert.That(pageTitle.Text, Is.EqualTo("Новости"), "Заголовок страницы не соответствует ожидаемому");
    }


    [Test]
    // Редактирование профиля пользователя работает корректно
    public void UserEditHisProfile()
    {
        Authorization();
        driver.Navigate().GoToUrl("https://staff-testing.testkontur.ru/profile/settings/edit");

        // Добавить в описание профиля что-нибудь
        driver.FindElement(By.CssSelector("textarea.react-ui-r3t2bi")).SendKeys("Bikchurkin");
        // Нажать сохранить
        driver.FindElement(By.CssSelector("button.sc-juXuNZ.kVHSha")).Click();
        //Проверим, что данные добавлены и сохранены. Прошу прощения, что через xPath, но по-другому не смог найти((
        var description = wait.Until(ExpectedConditions.ElementIsVisible
        (By.XPath("//div[contains(@class, 'sc-bdnxRM') and contains(text(), 'Bikchurkin')]")));

        Assert.That(description.Text, Is.EqualTo("Bikchurkin"), "Текст описания профиля не сохранился.");

        // Удаление добавленных данных
        DeletingDescriptionField();
        
        // Тут я отчаялся.... Не смог разобраться почему так долго закрывается окно после теста....
        // Я пробовал через явное ожидание, но тогда почему-то не ищется элемент...
    }


    [Test]
    // В разделе "Сообщества" во вкладке "Я участник" отображаются сообщества только с подписью "я участник"
    public void VerifyCommunitiesWithMemberFilter()
    {
        Authorization();
        driver.Navigate().GoToUrl("https://staff-testing.testkontur.ru/communities?activeTab=isMember");

        // Проверяем, что находимся во вкладке я участник
        var activeTab = wait.Until(d => d.FindElements(By.CssSelector("a[data-tid='Item']"))
        .FirstOrDefault(e => e.Text == "Я участник"));

        Assert.That(activeTab, Is.Not.Null, "Вкладка 'Я участник' не найдена");
        Assert.That(activeTab.GetAttribute("aria-current"), Is.EqualTo("page"), "Вкладка 'Я участник' не активна");

        // Находим все блоки сообществ
        var communities = driver.FindElements(By.CssSelector(".sc-knSFqH"));

        Assert.That(communities.Count, Is.GreaterThan(0), "Не найдено ни одного сообщества");

        foreach (var community in communities)
        {
            // Проверка, что у сообщества есть подпись "я участник"
            var memberBadge = wait.Until(d => community.FindElements(By.XPath(".//*[contains(text(), 'Я участник')]"))
            .FirstOrDefault());

            Assert.That(memberBadge, Is.Not.Null, $"Не найдена метка 'Я участник' в сообществе: {community.Text}");
        }
    }   


    [Test]
    // Панель фильтров у мероприятий можно открыть и закрыть
    public void FilterPanelCanBeClosed()
    {
        Authorization();
        driver.Navigate().GoToUrl("https://staff-testing.testkontur.ru/events");
        
        // Убираем неявные ожидания (на время теста)
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        // Кнопка фильтра
        new WebDriverWait(driver, TimeSpan.FromSeconds(3))
        .Until(d => d.FindElement(By.CssSelector("span.sc-zHacW.bPOSbT"))).Click();
        
        bool isPanelClosed = new WebDriverWait(driver, TimeSpan.FromSeconds(3)).Until(d =>
            {
                var elements = d.FindElements(By.CssSelector("div.sc-hDlsYP"));
                return elements.Count == 0 || !elements[0].Displayed;
            });

        Assert.That(isPanelClosed, Is.True, "Панель фильтров не закрылась");
    }
}