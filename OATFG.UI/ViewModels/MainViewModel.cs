using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OATFG.Core.DataModels;
using OATFG.CSXUnitGenerator;
using OATFG.CSXUnitGenerator.DataModels;
using OATFG.Generators;
using OATFG.UI.Services;

namespace OATFG.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        ThemeManager = ThemeManager.Instance;
        ThemeManager.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeManager.CurrentTheme))
            {
                OnPropertyChanged(nameof(CurrentTheme));
            }
        };
    }

    public ThemeManager ThemeManager { get; }

    public IEnumerable<AuthenticationTypes> AuthTypes => Enum.GetValues<AuthenticationTypes>();

    [ObservableProperty]
    private string _currentTheme = ThemeManager.Instance.CurrentTheme;

    partial void OnCurrentThemeChanged(string value) => ThemeManager.CurrentTheme = value;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    private string _specPath = string.Empty;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _environmentName = "Development";

    [ObservableProperty]
    private AuthenticationTypes _authenticationType = AuthenticationTypes.None;

    [ObservableProperty]
    private string _outputRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "oatfg-output");

    [ObservableProperty]
    private bool _generateCSharp = true;

    [ObservableProperty]
    private bool _generatePython = true;

    [ObservableProperty]
    private bool _generateRobotFramework = true;

    [ObservableProperty]
    private bool _generateJavaScript = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    public ObservableCollection<string> GeneratedFiles { get; } = [];

    public ObservableCollection<LintResult> LintResults { get; } = [];

    [RelayCommand]
    private void BrowseSpec()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an OpenAPI specification",
            Filter = "OpenAPI specifications (*.json;*.yaml;*.yml)|*.json;*.yaml;*.yml|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            SpecPath = dialog.FileName;
        }
    }

    private bool CanGenerate() => !IsBusy && !string.IsNullOrWhiteSpace(SpecPath);

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        IsBusy = true;
        StatusMessage = "Parsing specification…";
        try
        {
            GeneratedFiles.Clear();
            LintResults.Clear();

            var transformer = new OpenAPItoCSXTransformer();
            string? projectName = string.IsNullOrWhiteSpace(ProjectName) ? null : ProjectName.Trim();
            API api = await transformer.CreateCSXAPIFromOpenAPIDocument(
                SpecPath.Trim(), EnvironmentName, projectName, AuthenticationType);

            Directory.CreateDirectory(OutputRoot);
            var files = new List<string>();

            if (GenerateCSharp)
            {
                api.RootFolderName = OutputRoot;
                new CSXFileAndFolderGenerator(api).GenerateFrameworkFiles();
                string solutionFolder = Path.Combine(OutputRoot, api.SolutionFolder);
                files.AddRange(Directory.EnumerateFiles(solutionFolder, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")));
            }

            if (GeneratePython)
            {
                files.AddRange(PythonGenerator.Generate(api, OutputRoot));
            }

            if (GenerateRobotFramework)
            {
                files.AddRange(RobotGenerator.Generate(api, OutputRoot));
            }

            if (GenerateJavaScript)
            {
                files.AddRange(JavaScriptGenerator.Generate(api, OutputRoot));
            }

            foreach (string file in files)
            {
                GeneratedFiles.Add(file);
            }

            // syntax-lint the generated suites with the per-language toolchains
            var lintTasks = new List<Task<LintResult>>();
            foreach (string file in files)
            {
                if (GeneratePython && file.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                {
                    lintTasks.Add(LintService.LintAsync(file, LintTarget.Python));
                }
                else if (GenerateJavaScript && file.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase))
                {
                    lintTasks.Add(LintService.LintAsync(file, LintTarget.JavaScript));
                }
                else if (GenerateRobotFramework && file.EndsWith(".robot", StringComparison.OrdinalIgnoreCase))
                {
                    lintTasks.Add(LintService.LintAsync(file, LintTarget.Robot));
                }
            }

            foreach (LintResult result in await Task.WhenAll(lintTasks))
            {
                LintResults.Add(result);
            }

            int issues = LintResults.Count(r => !r.Passed);
            StatusMessage = issues == 0
                ? $"Generated {files.Count} files for {api.Endpoints.Count} endpoints — lint clean."
                : $"Generated {files.Count} files for {api.Endpoints.Count} endpoints — {issues} lint issue(s), see below.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}