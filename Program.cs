using System.Diagnostics.CodeAnalysis;
using AngleSharp;

var checkPriviledgedStocks = true;
File.Delete("output.txt");

var bigCompanies = await GetCompanies("https://smart-lab.ru/q/index_stocks/IMOEX/order_by_issue_capitalization/desc/", checkPriviledgedStocks);
PrintCompanies(bigCompanies, "Big companies:");

var allCompanies = await GetCompanies("https://smart-lab.ru/q/index_stocks/MOEXBMI/order_by_issue_capitalization/desc/", checkPriviledgedStocks);
PrintCompanies(allCompanies, "All companies:");

var exceptCompanies = allCompanies.Except(bigCompanies, new CompanyComparer()).ToList();
PrintCompanies(exceptCompanies, "Except companies:");

double? prevIndex = 0;
PrintCompanies(exceptCompanies.Select(x => {
    var isHole = x.Index - prevIndex > 1;
    prevIndex = x.Index;
    return new { Company = x, IsHole = isHole};  
    }).Where(x => x.IsHole).Select(x => x.Company).ToList(), "Increment except companies holes:");

PrintCompanies(bigCompanies.Where(x => Math.Abs(x.PercentDiff) >= 0.005).OrderByDescending(x => x.PercentDiff).ToList(), "Big companies percent diff:");
PrintCompanies(allCompanies.Where(x => Math.Abs(x.PercentDiff) >= 0.005).OrderByDescending(x => x.PercentDiff).ToList(), "All companies percent diff:");

Console.ReadLine();

static async Task<List<Company>> GetCompanies(string url, bool checkPriviledgedStocks)
{
    var document = await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(url);

    var titles = document.QuerySelectorAll("table tr td:nth-child(3)").Select(m => m.TextContent).ToList();
    var tickers = document.QuerySelectorAll("table tr td:nth-child(3) a").Select(m => m.GetAttribute("Href").Replace("/forum/", "")).ToList();
    var percents = document.QuerySelectorAll("table tr td:nth-child(4)").Select(m => m.TextContent).ToList();
    var prices = document.QuerySelectorAll("table tr td:nth-child(7)").Select(m => m.TextContent).ToList();
    var caps = document.QuerySelectorAll("table tr td:nth-child(12)").Select(m => m.TextContent).ToList();

    HttpClient client = new HttpClient();

    var list = titles.Select((x, i) => new
    {
        Title = x,
        Cap = double.Parse(caps[i].Replace(" ", "")),
        Percent = double.Parse(percents[i].Replace("%", "")) / 100,
        Ticker = tickers[i],
        Price = double.Parse(prices[i])
    }).ToList();

    var capSum = list.GroupBy(x => x.Cap).Sum(x => x.Key);

    var companies = list.GroupBy(x => x.Cap).Select((x, i) => new
            Company(
                i + 1,
                string.Join(", ", x.Select(y => x.Count() > 1 ? $"{y.Title} ({y.Percent:P2}, {y.Price}Р)" : y.Title)),
                checkPriviledgedStocks && client.GetStringAsync($"https://www.tinkoff.ru/invest/stocks/{x.First().Ticker}P/").Result.Contains("Купить акции") ? $"{x.First().Ticker}P" : x.First().Ticker,
                x.Key,
                x.Last().Price,
                x.Sum(s => s.Percent),
                x.Key / capSum,
                x.Key / capSum - x.Sum(s => s.Percent)
                )).ToList();

    return companies;
}

static void PrintCompanies(List<Company> companies, string title)
{
    Console.WriteLine(title);
    Console.WriteLine();

    File.AppendAllText("output.txt", title + "\n\n");

    foreach (var company in companies)
    {
        PrintCompany(company);
    }
    Console.WriteLine();
    Console.WriteLine();
    File.AppendAllText("output.txt", "\n\n");
}

static void PrintCompany(Company company)
{
    var line = $"{company.Index}\t{company.NewPercent:P2}\t{company.Percent:P2}\t{company.PercentDiff:+0.00%;-0.00%}\t{company.Cap:0.00}\thttps://www.tinkoff.ru/invest/stocks/{company.Ticker}\t{company.Price/1000:0.000}\t{company.Title}";
    Console.WriteLine(line);
    File.AppendAllText("output.txt", line + "\n");
}

public record Company(int Index, string Title, string Ticker, double Cap, double Price, double Percent, double NewPercent, double PercentDiff);

public class CompanyComparer : IEqualityComparer<Company>
{
    public bool Equals(Company? x, Company? y)
    {
        return x?.Ticker == y?.Ticker;
    }

    public int GetHashCode([DisallowNull] Company obj)
    {
        return obj.Ticker.GetHashCode();
    }
}
