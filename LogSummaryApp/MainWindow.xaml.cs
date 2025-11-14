using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LogSummaryApp.Models;
using Ookii.Dialogs.Wpf;

namespace LogSummaryApp
{
    public partial class MainWindow : Window
    {
        private List<string> selectedFolders = new List<string>();
        public ObservableCollection<TableRowData> TableRows { get; set; } = new ObservableCollection<TableRowData>();
        private Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();
        private HashSet<string> vbuckRangeNames = new HashSet<string> { "1-50", "51-100", "100-200", "200-500", "500-2000", "2000-5000", "5000+" };
        private HashSet<string> skinRangeNames = new HashSet<string> { "6-20", "20-50", "50-100", "100-150", "150-200", "200+" };
        private BitmapImage? placeholderIcon;
        private BitmapImage? vbucksIcon;
        private BitmapImage? skinRangeIcon;
        private const string iconDirectory = "skin_icons";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Load placeholder icon
            placeholderIcon = CreatePlaceholderIcon();

            // Load special icons
            string vbucksPath = Path.Combine(iconDirectory, "vbucks_icon.png");
            string skinRangePath = Path.Combine(iconDirectory, "skin_range_icon.png");

            vbucksIcon = LoadIconFromPath(vbucksPath) ?? placeholderIcon;
            skinRangeIcon = LoadIconFromPath(skinRangePath) ?? placeholderIcon;

            // Check if skin_icons directory exists
            if (!Directory.Exists(iconDirectory))
            {
                Console.WriteLine($"Warning: {iconDirectory} directory not found. Icons will not be displayed.");
            }
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFolders.Clear();
            SelectFolder();
        }

        private void SelectFolder()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select a folder with TXT log files",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.SelectedPath;
                if (!selectedFolders.Contains(selectedPath))
                {
                    selectedFolders.Add(selectedPath);
                }

                StatusLabel.Text = $"{selectedFolders.Count} folder(s) selected.";

                var result = MessageBox.Show(
                    "Do you want to select another folder?",
                    "Multiple Folder Selection",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SelectFolder();
                }
                else
                {
                    StartProcessing();
                }
            }
            else
            {
                if (selectedFolders.Count == 0)
                {
                    StatusLabel.Text = "Operation cancelled. No folders were selected.";
                }
            }
        }

        private async void StartProcessing()
        {
            SelectFolderButton.IsEnabled = false;
            SelectFolderButton.Content = "Processing...";
            StatusLabel.Text = $"Processing {selectedFolders.Count} folder(s), please wait...";

            try
            {
                LogSummary summary = await Task.Run(() => ProcessLogs(selectedFolders));

                Dispatcher.Invoke(() =>
                {
                    PopulateUI(summary);
                    string outputDir = Path.Combine(selectedFolders[0], "skins_output");
                    StatusLabel.Text = $"Processing complete! Results saved to: {outputDir}";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusLabel.Text = $"Error: {ex.Message}";
                });
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    SelectFolderButton.IsEnabled = true;
                    SelectFolderButton.Content = "Select Folder(s) and Start Sorting";
                });
            }
        }

        private LogSummary ProcessLogs(List<string> folderList)
        {
            // Define rare items
            var rareSkins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Skull Trooper", "Wildcat", "Black Knight", "Galaxy", "IKONIK", "Sparkle Specialist",
                "World Warrior", "Travis Scott", "Glow", "Stealth Reflex", "Double Helix",
                "Blue Squire", "The Reaper", "Royale Bomber", "Psycho Bandit", "Elite Agent",
                "Trailblazer", "Dark Vertex", "Special Forces", "Neo Versa", "Rogue Spider Knight",
                "Dark Skully", "Dark Voyager", "Kratos", "Omega", "Huntmaster Saber",
                "Rose Team Leader", "Rogue Agent", "Polo Prodigy", "Renegade Raider", "Ghoul Trooper"
            };

            var rarePickaxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Merry Mint Axe", "Leviathan Axe"
            };

            var rareGliders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Mako"
            };

            // VBuck ranges
            var vbuckRanges = new Dictionary<string, (int min, int max)>
            {
                { "1-50", (1, 50) },
                { "51-100", (51, 100) },
                { "100-200", (100, 200) },
                { "200-500", (200, 500) },
                { "500-2000", (500, 2000) },
                { "2000-5000", (2000, 5000) },
                { "5000+", (5000, int.MaxValue) }
            };

            // Skin ranges
            var skinRanges = new Dictionary<string, (int min, int max)>
            {
                { "6-20", (6, 20) },
                { "20-50", (20, 50) },
                { "50-100", (50, 100) },
                { "100-150", (100, 150) },
                { "150-200", (150, 200) },
                { "200+", (200, int.MaxValue) }
            };

            // Output directory
            string outputDir = Path.Combine(folderList[0], "skins_output");
            Directory.CreateDirectory(outputDir);

            // Allowed files
            var allowedFiles = new HashSet<string> { "skins0to10.txt", "skins10to50.txt", "skins50plus.txt" };

            // Find all valid files
            var allFiles = new List<string>();
            foreach (var folder in folderList)
            {
                if (Directory.Exists(folder))
                {
                    var files = Directory.GetFiles(folder, "*.txt")
                        .Where(f => allowedFiles.Contains(Path.GetFileName(f)))
                        .ToList();
                    allFiles.AddRange(files);
                }
            }

            if (allFiles.Count == 0)
            {
                throw new FileNotFoundException("No valid log files found in any of the selected directories.");
            }

            // Parse files
            var accounts = new Dictionary<string, AccountData>();

            var loginRegex = new Regex(@"([\w\.\-]+@[\w\.\-]+:[^\s]+)");
            var characterRegex = new Regex(@"Character:\s*([^\n\r]+)");
            var pickaxeRegex = new Regex(@"Pickaxe:\s*([^\n\r]+)");
            var gliderRegex = new Regex(@"Glider:\s*([^\n\r]+)");
            var inactiveRegex = new Regex(@"Inactive:\s*(true|false)", RegexOptions.IgnoreCase);
            var vbucksRegex = new Regex(@"VBucks:\s*(\d+)");
            var skinsRegex = new Regex(@"Skins:\s*(\d+)");

            foreach (var filePath in allFiles)
            {
                string content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                var splitRegex = new Regex(@"\n(?=[\w\.\-]+@[\w\.\-]+:)");
                var blocks = splitRegex.Split(content);

                foreach (var block in blocks)
                {
                    if (string.IsNullOrWhiteSpace(block))
                        continue;

                    var loginMatch = loginRegex.Match(block);
                    if (!loginMatch.Success)
                        continue;

                    string login = loginMatch.Groups[1].Value;

                    if (!accounts.ContainsKey(login))
                    {
                        accounts[login] = new AccountData();
                    }

                    var accountData = accounts[login];

                    // Extract Character
                    var characterMatch = characterRegex.Match(block);
                    if (characterMatch.Success)
                    {
                        var characters = characterMatch.Groups[1].Value
                            .Split(',')
                            .Select(c => c.Trim().ToLower())
                            .ToList();

                        foreach (var rareSkin in rareSkins)
                        {
                            if (characters.Any(c => c.Equals(rareSkin.ToLower(), StringComparison.OrdinalIgnoreCase)))
                            {
                                accountData.FoundItems.Add(rareSkin);
                            }
                        }
                    }

                    // Extract Pickaxe
                    var pickaxeMatch = pickaxeRegex.Match(block);
                    if (pickaxeMatch.Success)
                    {
                        var pickaxes = pickaxeMatch.Groups[1].Value
                            .Split(',')
                            .Select(p => p.Trim().ToLower())
                            .ToList();

                        foreach (var rarePickaxe in rarePickaxes)
                        {
                            if (pickaxes.Any(p => p.Equals(rarePickaxe.ToLower(), StringComparison.OrdinalIgnoreCase)))
                            {
                                accountData.FoundItems.Add(rarePickaxe);
                            }
                        }
                    }

                    // Extract Glider
                    var gliderMatch = gliderRegex.Match(block);
                    if (gliderMatch.Success)
                    {
                        var gliders = gliderMatch.Groups[1].Value
                            .Split(',')
                            .Select(g => g.Trim().ToLower())
                            .ToList();

                        foreach (var rareGlider in rareGliders)
                        {
                            if (gliders.Any(g => g.Contains(rareGlider.ToLower())))
                            {
                                accountData.FoundItems.Add(rareGlider);
                            }
                        }
                    }

                    // Extract Inactive
                    var inactiveMatch = inactiveRegex.Match(block);
                    if (inactiveMatch.Success)
                    {
                        string inactiveValue = inactiveMatch.Groups[1].Value.ToLower();
                        if (inactiveValue == "false")
                        {
                            accountData.SeenActive = true;
                        }
                        else if (inactiveValue == "true")
                        {
                            accountData.SeenInactive = true;
                        }
                    }

                    // Extract VBucks
                    var vbucksMatch = vbucksRegex.Match(block);
                    if (vbucksMatch.Success)
                    {
                        int vbucks = int.Parse(vbucksMatch.Groups[1].Value);
                        accountData.VBucks = Math.Max(accountData.VBucks, vbucks);
                    }

                    // Extract Skins
                    var skinsMatch = skinsRegex.Match(block);
                    if (skinsMatch.Success)
                    {
                        int skinCount = int.Parse(skinsMatch.Groups[1].Value);
                        accountData.DeclaredSkinCounts.Add(skinCount);
                    }
                }
            }

            // Determine status for each account
            foreach (var account in accounts.Values)
            {
                if (account.SeenActive)
                {
                    account.Status = "active";
                }
                else if (account.SeenInactive)
                {
                    account.Status = "inactive";
                }
                else
                {
                    account.Status = "active";
                }
            }

            // Initialize count dictionaries
            var itemCounts = new Dictionary<string, CategoryCount>();
            foreach (var item in rareSkins.Concat(rarePickaxes).Concat(rareGliders))
            {
                itemCounts[item] = new CategoryCount();
            }

            var vbucksCounts = new Dictionary<string, CategoryCount>();
            foreach (var range in vbuckRanges.Keys)
            {
                vbucksCounts[range] = new CategoryCount();
            }

            var skinrangeCounts = new Dictionary<string, CategoryCount>();
            foreach (var range in skinRanges.Keys)
            {
                skinrangeCounts[range] = new CategoryCount();
            }

            // Initialize file sets
            var itemFiles = new Dictionary<string, HashSet<string>>();
            foreach (var item in rareSkins.Concat(rarePickaxes).Concat(rareGliders))
            {
                itemFiles[item] = new HashSet<string>();
            }

            var vbucksFiles = new Dictionary<string, HashSet<string>>();
            foreach (var range in vbuckRanges.Keys)
            {
                vbucksFiles[range] = new HashSet<string>();
            }

            var skinrangeFiles = new Dictionary<string, HashSet<string>>();
            foreach (var range in skinRanges.Keys)
            {
                skinrangeFiles[range] = new HashSet<string>();
            }

            // Count and collect logins
            foreach (var kvp in accounts)
            {
                string login = kvp.Key;
                var account = kvp.Value;

                // Count items
                foreach (var item in account.FoundItems)
                {
                    if (account.Status == "active")
                    {
                        itemCounts[item].Active++;
                    }
                    else
                    {
                        itemCounts[item].Inactive++;
                    }
                    itemFiles[item].Add(login);
                }

                // Count VBucks
                if (account.VBucks > 0)
                {
                    foreach (var range in vbuckRanges)
                    {
                        if (account.VBucks >= range.Value.min && account.VBucks <= range.Value.max)
                        {
                            if (account.Status == "active")
                            {
                                vbucksCounts[range.Key].Active++;
                            }
                            else
                            {
                                vbucksCounts[range.Key].Inactive++;
                            }
                            vbucksFiles[range.Key].Add(login);
                            break;
                        }
                    }
                }

                // Count skin ranges
                int skinCount = account.DeclaredSkinCounts.Count > 0
                    ? account.DeclaredSkinCounts.Max()
                    : account.FoundItems.Count;

                foreach (var range in skinRanges)
                {
                    if (skinCount >= range.Value.min && skinCount <= range.Value.max)
                    {
                        if (account.Status == "active")
                        {
                            skinrangeCounts[range.Key].Active++;
                        }
                        else
                        {
                            skinrangeCounts[range.Key].Inactive++;
                        }
                        skinrangeFiles[range.Key].Add(login);
                        break;
                    }
                }
            }

            // Write output files
            foreach (var kvp in itemFiles)
            {
                if (kvp.Value.Count > 0)
                {
                    string filename = SanitizeFilename(kvp.Key);
                    string filePath = Path.Combine(outputDir, filename);
                    var sortedLogins = kvp.Value.OrderBy(l => l).ToList();
                    File.WriteAllLines(filePath, sortedLogins);
                }
            }

            foreach (var kvp in vbucksFiles)
            {
                if (kvp.Value.Count > 0)
                {
                    string filename = $"vbucks_{kvp.Key.Replace("+", "plus").Replace(" ", "")}.txt";
                    string filePath = Path.Combine(outputDir, filename);
                    var sortedLogins = kvp.Value.OrderBy(l => l).ToList();
                    File.WriteAllLines(filePath, sortedLogins);
                }
            }

            foreach (var kvp in skinrangeFiles)
            {
                if (kvp.Value.Count > 0)
                {
                    string filename = $"skins_{kvp.Key.Replace("+", "plus").Replace(" ", "")}.txt";
                    string filePath = Path.Combine(outputDir, filename);
                    var sortedLogins = kvp.Value.OrderBy(l => l).ToList();
                    File.WriteAllLines(filePath, sortedLogins);
                }
            }

            // Create summary
            var summary = new LogSummary
            {
                TotalAccounts = accounts.Count,
                TotalActive = accounts.Values.Count(a => a.Status == "active"),
                TotalInactive = accounts.Values.Count(a => a.Status == "inactive"),
                SkinCounts = itemCounts,
                VbucksCounts = vbucksCounts,
                SkinrangeCounts = skinrangeCounts
            };

            // Write summary.json
            string summaryPath = Path.Combine(outputDir, "summary.json");
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string jsonContent = JsonSerializer.Serialize(summary, jsonOptions);
            File.WriteAllText(summaryPath, jsonContent);

            return summary;
        }

        private void PopulateUI(LogSummary summary)
        {
            TableRows.Clear();
            TotalActiveLabel.Text = $"Total Active: {summary.TotalActive}";
            TotalInactiveLabel.Text = $"Total Inactive: {summary.TotalInactive}";

            // Combine all categories
            var allCategories = new List<(string name, int active, int inactive, int priority)>();

            foreach (var kvp in summary.SkinCounts)
            {
                if (kvp.Value.Active > 0 || kvp.Value.Inactive > 0)
                {
                    allCategories.Add((kvp.Key, kvp.Value.Active, kvp.Value.Inactive, GetCategoryPriority(kvp.Key)));
                }
            }

            foreach (var kvp in summary.VbucksCounts)
            {
                if (kvp.Value.Active > 0 || kvp.Value.Inactive > 0)
                {
                    allCategories.Add((kvp.Key, kvp.Value.Active, kvp.Value.Inactive, GetCategoryPriority(kvp.Key)));
                }
            }

            foreach (var kvp in summary.SkinrangeCounts)
            {
                if (kvp.Value.Active > 0 || kvp.Value.Inactive > 0)
                {
                    allCategories.Add((kvp.Key, kvp.Value.Active, kvp.Value.Inactive, GetCategoryPriority(kvp.Key)));
                }
            }

            // Sort by priority then by total count descending
            var sortedCategories = allCategories
                .OrderBy(c => c.priority)
                .ThenByDescending(c => c.active + c.inactive)
                .ToList();

            // Create table rows
            int index = 1;
            foreach (var category in sortedCategories)
            {
                var row = new TableRowData
                {
                    Index = index++,
                    Count = category.active + category.inactive,
                    Category = category.name,
                    Active = category.active,
                    Inactive = category.inactive,
                    Icon = GetIconForCategory(category.name)
                };
                TableRows.Add(row);
            }
        }

        private int GetCategoryPriority(string categoryName)
        {
            if (vbuckRangeNames.Contains(categoryName))
                return 1;
            if (skinRangeNames.Contains(categoryName))
                return 2;
            return 0;
        }

        private BitmapImage? GetIconForCategory(string categoryName)
        {
            if (vbuckRangeNames.Contains(categoryName))
                return vbucksIcon;
            if (skinRangeNames.Contains(categoryName))
                return skinRangeIcon;

            string filename = SanitizeFilename(categoryName);

            if (imageCache.ContainsKey(filename))
                return imageCache[filename];

            string filePath = Path.Combine(iconDirectory, filename);
            var icon = LoadIconFromPath(filePath);

            if (icon != null)
            {
                imageCache[filename] = icon;
                return icon;
            }

            return placeholderIcon;
        }

        private BitmapImage? LoadIconFromPath(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(Path.GetFullPath(filePath));
                    bitmap.DecodePixelWidth = 20;
                    bitmap.DecodePixelHeight = 20;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        private string SanitizeFilename(string name)
        {
            string result = name.ToLower();
            result = Regex.Replace(result, @"\s+", "_");
            result = Regex.Replace(result, @"[^\w\-_\.]", "");
            return result + ".png";
        }

        private BitmapImage CreatePlaceholderIcon()
        {
            int size = 64;
            var drawingVisual = new DrawingVisual();

            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                // Create a white star shape
                var points = new PointCollection();
                int numPoints = 10;
                double angleStep = 2 * Math.PI / numPoints;
                double centerX = size / 2.0;
                double centerY = size / 2.0;
                double outerRadius = size / 2.5;
                double innerRadius = size / 6.0;

                for (int i = 0; i < numPoints; i++)
                {
                    double radius = (i % 2 == 0) ? outerRadius : innerRadius;
                    double angle = i * angleStep - Math.PI / 2;
                    double x = centerX + radius * Math.Cos(angle);
                    double y = centerY + radius * Math.Sin(angle);
                    points.Add(new Point(x, y));
                }

                var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = true };
                for (int i = 1; i < points.Count; i++)
                {
                    pathFigure.Segments.Add(new LineSegment(points[i], true));
                }

                var pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                dc.DrawGeometry(Brushes.White, null, pathGeometry);
            }

            var renderBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
    }
}
