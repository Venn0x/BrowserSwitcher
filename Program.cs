using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MultiBrowserC
{
    class Browser
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string StartArguments { get; set; }
    }

    class Rule
    {
        public int Id { get; set; }
        public string Hostname { get; set; }
        public string BrowserName { get; set; }
        public int Priority { get; set; }
    }


    class Program
    {
        static List<Rule> rules = new List<Rule>();
        static List<Browser> browsers = new List<Browser>();
        static string dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json");
        static int nextRuleId = 1;

        static void Main(string[] args)
        {
            LoadData();
        
            if (args.Length > 0 && args[0].StartsWith("http"))
            {
                HandleUrl(args[0]);
            }
            else
            {
                Console.WriteLine("Welcome to BrowserSwitcher app! Below are the avaliable commands");
                ShowHelp();
                while (true)
                {
                    Console.Write("> ");
                    string input = Console.ReadLine();
                    HandleCommand(input);
                }
            }
        }

        static void HandleCommand(string input)
        {
            var parts = input.Split(' ');
            switch (parts[0].ToLower())
            {
                case "help":
                    ShowHelp();
                    break;
                case "rule":
                    HandleRuleCommand(parts);
                    break;
                case "browser":
                    HandleBrowserCommand(parts);
                    break;
                default:
                    Console.WriteLine("Unknown command. Type 'help' to see available commands.");
                    break;
            }
        }

        static void HandleBrowserCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Invalid browser command. Type 'help' for more details.");
                return;
            }

            switch (parts[1].ToLower())
            {
                case "list":
                    ListBrowsers();
                    break;
                case "add":
                    AddBrowser();
                    break;
                case "remove":
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Usage: browser remove <name>");
                    }
                    else
                    {
                        RemoveBrowser(parts[2]);
                    }
                    break;
                case "edit":
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Usage: browser edit <name>");
                    }
                    else
                    {
                        EditBrowser(parts[2]);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown browser command.");
                    break;
            }
        }

        static void ListBrowsers()
        {
            if (browsers.Count == 0)
            {
                Console.WriteLine("No browsers available.");
                return;
            }
            Console.WriteLine("Available browsers:");
            foreach (var browser in browsers)
            {
                Console.WriteLine($"Name: {browser.Name}, Path: {browser.Path}, StartArguments: {browser.StartArguments}");
            }
        }

        static void AddBrowser()
        {
            Console.Write("Browser display name? ");
            string name = Console.ReadLine();

            Console.Write("Browser path? ");
            string path = Console.ReadLine();

            Console.Write("Browser start arguments (optional)? ");
            string arguments = Console.ReadLine();

            var browser = new Browser { Name = name, Path = path, StartArguments = arguments };
            browsers.Add(browser);
            SaveData();
            Console.WriteLine("Browser added.");
        }

        static void RemoveBrowser(string name)
        {
            var browser = browsers.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (browser != null)
            {
                browsers.Remove(browser);
                SaveData();
                Console.WriteLine($"Browser '{name}' removed.");
            }
            else
            {
                Console.WriteLine($"Browser '{name}' not found.");
            }
        }

        static void EditBrowser(string name)
        {
            var browser = browsers.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (browser == null)
            {
                Console.WriteLine($"Browser '{name}' not found.");
                return;
            }

            Console.Write($"New browser path (current: {browser.Path})? ");
            string newPath = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newPath))
            {
                browser.Path = newPath;
            }

            Console.Write($"New start arguments (current: {browser.StartArguments})? ");
            string newArgs = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newArgs))
            {
                browser.StartArguments = newArgs;
            }

            SaveData();
            Console.WriteLine($"Browser '{name}' updated.");
        }

        static void HandleRuleCommand(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No rule command provided.");
                return;
            }

            switch (args[1])
            {
                case "list":
                    bool prioritize = args.Length > 2 && args[2] == "-p";
                    ListRules(prioritize);
                    break;

                case "add":
                    if (args.Length < 5)
                    {
                        Console.WriteLine("Usage: rule add <hostname> <browser_name> <priority>\nPriority logic: smaller numbers have priority over the bigger ones");
                    }
                    else
                    {
                        AddRule(args[2], args[3], int.Parse(args[4]));
                    }
                    break;

                case "remove":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Usage: rule remove <id>");
                    }
                    else
                    {
                        RemoveRule(int.Parse(args[1]));
                    }
                    break;

                case "edit":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Usage: rule edit <id> <hostname/browser/priority> <new_value>");
                    }
                    else
                    {
                        EditRule(int.Parse(args[2]), args[3], args[4]);
                    }
                    break;

                default:
                    Console.WriteLine("Unknown rule command.");
                    break;
            }
        }

        static void ListRules(bool prioritize = false)
        {
            var sortedRules = prioritize ? rules.OrderBy(r => r.Priority).ToList() : rules;

            foreach (var rule in sortedRules)
            {
                Console.WriteLine($"ID: {rule.Id}, Hostname: {rule.Hostname}, Browser: {rule.BrowserName}, Priority: {rule.Priority}");
            }
        }

        // Add a new rule
        static void AddRule(string hostname, string browser, int priority)
        {
            var browserEntry = browsers.FirstOrDefault(b => b.Name.Equals(browser, StringComparison.OrdinalIgnoreCase));
            if (browserEntry == null)
            {
                Console.WriteLine("Browser not found");
                return;
            }
            rules.Add(new Rule { Id = nextRuleId++, Hostname = hostname, BrowserName = browser, Priority = priority });
            Console.WriteLine("Rule added.");
            SaveData(); // Save changes to file
        }

        // Remove a rule by ID
        static void RemoveRule(int id)
        {
            var rule = rules.FirstOrDefault(r => r.Id == id);
            if (rule != null)
            {
                rules.Remove(rule);
                Console.WriteLine("Rule removed.");
                SaveData(); // Save changes to file
            }
            else
            {
                Console.WriteLine("Rule not found.");
            }
        }

        // Edit an existing rule by ID
        static void EditRule(int id, string field, string newValue)
        {
            var rule = rules.FirstOrDefault(r => r.Id == id);
            if (rule == null)
            {
                Console.WriteLine("Rule not found.");
                return;
            }

            switch (field)
            {
                case "hostname":
                    rule.Hostname = newValue;
                    Console.WriteLine("Hostname updated.");
                    break;

                case "browser":
                    var browserEntry = browsers.FirstOrDefault(b => b.Name.Equals(newValue, StringComparison.OrdinalIgnoreCase));
                    if (browserEntry == null)
                    {
                        Console.WriteLine("Browser not found");
                        return;
                    }
                    rule.BrowserName = newValue;
                    Console.WriteLine("Browser updated.");
                    break;

                case "priority":
                    rule.Priority = int.Parse(newValue);
                    Console.WriteLine("Priority updated.");
                    break;

                default:
                    Console.WriteLine("Unknown field.");
                    break;
            }

            SaveData(); // Save changes to file
        }

    

        static void HandleUrl(string url)
        {
            // Existing URL handling logic with modified browser selection:
            var uri = new Uri(url);
            var rule = rules.OrderBy(r => r.Priority).FirstOrDefault(r => MatchHostname(r.Hostname, uri.Host));
            if (rule != null)
            {
                var browser = browsers.FirstOrDefault(b => b.Name == rule.BrowserName);
                if (browser != null)
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = browser.Path,
                        Arguments = $"{browser.StartArguments} {url}",
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
                }
                else
                {
                    Console.WriteLine($"Browser '{rule.BrowserName}' not found.");
                }
            }
            else
            {
                Console.WriteLine("No matching rule found.");
            }
        }

        static bool MatchHostname(string pattern, string hostname)
        {
            if (pattern == "*")
                return true;

            if (pattern.StartsWith("*."))
            {
                string domain = pattern.Substring(2); // Get the domain after "*."
                return hostname.EndsWith(domain);
            }

            return pattern == hostname;
        }

        static void SaveData()
        {
            var data = new { browsers, rules };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(dataFilePath, json);
        }

        static void LoadData()
        {
            if (File.Exists(dataFilePath))
            {
                string json = File.ReadAllText(dataFilePath);
                var data = JsonSerializer.Deserialize<dynamic>(json);
                browsers = JsonSerializer.Deserialize<List<Browser>>(data.GetProperty("browsers").ToString()) ?? new List<Browser>();
                rules = JsonSerializer.Deserialize<List<Rule>>(data.GetProperty("rules").ToString()) ?? new List<Rule>();
                if (rules.Count > 0)
                {
                    nextRuleId = rules.Max(r => r.Id) + 1;
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  help                 - Display this help message");
            Console.WriteLine("  rule list            - List all rules");
            Console.WriteLine("  rule add             - Add a new rule");
            Console.WriteLine("  rule remove <id>     - Remove a rule");
            Console.WriteLine("  rule edit <id>       - Edit a rule");
            Console.WriteLine("  browser list         - List all browsers");
            Console.WriteLine("  browser add          - Add a new browser");
            Console.WriteLine("  browser remove <name>- Remove a browser");
            Console.WriteLine("  browser edit <name>  - Edit a browser");
        }
    }
}
