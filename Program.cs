using AngleSharp;

var config = Configuration.Default.WithDefaultLoader();
var address = "https://smart-lab.ru/q/index_stocks/IMOEX/order_by_issue_capitalization/desc/";
var context = BrowsingContext.New(config);
var document = await context.OpenAsync(address);
var cellSelector = "table tr td:nth-child(12)";
var cells = document.QuerySelectorAll(cellSelector);
var caps = cells.Select(m => m.TextContent).ToList();

var cellSelector1 = "table tr td:nth-child(3)";
var cells1 = document.QuerySelectorAll(cellSelector1);
var titles = cells1.Select(m => m.TextContent).ToList();

var cellSelector2 = "table tr td:nth-child(4)";
var cells2 = document.QuerySelectorAll(cellSelector2);
var old = cells2.Select(m => m.TextContent).ToList();

var list = titles.Select((x, i) => new { Title = x, Cap = double.Parse(caps[i].Replace(" ", "")), Percent = double.Parse(old[i].Replace("%", ""))/100}).ToList();
var groupedList = list.GroupBy(x => x.Cap).Select((x, i) => new
        {
            Key = x.Key,
            Index = i + 1,
            Cap = x.First().Cap,
            Title = string.Join(", ", x.Select(y => x.Count() > 1 ? $"{y.Title} ({y.Percent:P2})" : y.Title)),
            Percent = x.Sum(s => s.Percent),
        }).ToList();

var sum = groupedList.Sum(x => x.Cap);

foreach(var item in groupedList)
{
    var newPercent = item.Cap/sum;
    Console.WriteLine($"{item.Index}\t{newPercent:P2}\t{item.Percent:P2}\t{newPercent - item.Percent:P2}\t{item.Cap}\t\t{item.Title}");
}
