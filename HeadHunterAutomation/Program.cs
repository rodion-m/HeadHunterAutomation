using System.Diagnostics;
using Microsoft.Playwright;

var hh = new HhPages("https://hh.ru");
var credentials = new
{
    Login = "", Password = new HiddenPassword(File.ReadAllText("p.txt"))
};

using var playwright = await Playwright.CreateAsync();
await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false, 
    SlowMo = 1000,
    Timeout = 10_000,
    Env = new KeyValuePair<string, string>[]{ new("DEBUG", "pw:browser,pw:api") }
});

//Прикидываемся хромом:
BrowserNewContextOptions device = playwright.Devices["Desktop Chrome"];
await using IBrowserContext context = await browser.NewContextAsync(device);
IPage page = await context.NewPageAsync();
await Login();

Console.ReadKey();
await page.GotoAsync(hh.Search("asp.net core"));
await page.ScreenshotAsync(new() { Path = "screenshot.png" });
Console.ReadKey();

async Task Login()
{
    await page.GotoAsync(hh.Login);
    await page.ClickAsync("button[data-qa='expand-login-by-password']");
    await page.ClickAsync(hh.ButtonQa("expand-login-by-password"));
    await page.TypeAsync(hh.InputQa("login-input-username"), credentials.Login);
    await page.TypeAsync(hh.InputQa("login-input-password"), credentials.Password.Get());
    await page.ClickAsync(hh.ButtonQa("account-login-submit"));
    var incorrectPassword = await page.IsVisibleAsync(hh.DivQa("account-login-error"));
    if (incorrectPassword)
    {
        throw new Exception("Incorrect password");
    }

    //Сохранить куки
    //await page.Context.CookiesAsync();
}

class HhPages
{
    public string Host { get; }

    public HhPages(string host)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
    }
    
    public string Login => $"{Host}/account/login";
    public string Resume => $"{Host}/resume";
    public string SearchVacancies => $"{Host}/search/vacancy";
    
    public string Vacancy(int id) => $"{Host}/vacancy/{id}";
    public string Search(string query)
    {
        var escapedQuery = Uri.EscapeDataString(query);
        return $"{SearchVacancies}?text={escapedQuery}" +
               "&search_field=name&search_field=company_name&search_field=description";
    }

    public string ButtonQa(string dataQa) => $"button[data-qa='{dataQa}']";
    public string InputQa(string dataQa) => $"input[data-qa='{dataQa}']";

    public string DivQa(string dataQa) => $"div[data-qa='{dataQa}']";
}

public class HiddenPassword
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string _text;

    public HiddenPassword(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }
    
    public string Get() => _text;
}