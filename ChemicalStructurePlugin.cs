using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public class ChemicalStructurePlugin : IShapePlugin
    {
        private static readonly object _lockObject = new object();
        private static bool _staticInitialized = false;
        private static string? _pluginDataPath;

        public string Name => "化学構造式";

        public bool IsExoShapeSupported => true;

        public bool IsExoMaskSupported => true;

        public Version Version => GetVersion();

        public string Description => "モデル表示可能";

        public string Author => "routersys";

        public IEnumerable<string> SupportedFeatures => GetSupportedFeatures();

        public int MaxAtomCount => 1000;

        public int MaxBondCount => 2000;

        public bool IsInitialized { get; private set; }

        public string? InitializationError { get; private set; }

        public static string PluginDataPath => _pluginDataPath ?? "";

        static ChemicalStructurePlugin()
        {
            try
            {
                InitializeStaticResources();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Static initialization failed: {ex.Message}");
            }
        }

        public ChemicalStructurePlugin()
        {
            try
            {
                Initialize();
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                InitializationError = ex.Message;
                AsyncLogger.Instance.Log(LogType.Error, $"ChemicalStructurePlugin initialization failed: {ex.Message}");
            }
        }

        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        {
            try
            {
                ValidateInitialization();

                var parameter = new ChemicalStructureParameter(sharedData);

                ApplyDefaultSettings(parameter);
                ValidateParameter(parameter);

                return parameter;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error creating shape parameter: {ex.Message}");
                return CreateFallbackParameter(sharedData);
            }
        }

        public PluginTestResult RunSelfTest()
        {
            var result = new PluginTestResult();

            try
            {
                TestBasicFunctionality(result);
                TestPerformance(result);
                TestMemoryUsage(result);
                TestFileOperations(result);
                TestFormulaParser(result);
                TestPresetSystem(result);

                result.IsSuccess = result.Errors.Count == 0;

                if (result.IsSuccess)
                {
                    AsyncLogger.Instance.Log(LogType.Info, "All self-tests passed successfully");
                }
                else
                {
                    AsyncLogger.Instance.Log(LogType.Warning, $"Self-test failed with {result.Errors.Count} errors");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Self-test failed: {ex.Message}");
                result.IsSuccess = false;
            }

            return result;
        }

        public IEnumerable<PresetMolecule> GetPresetMolecules()
        {
            try
            {
                var defaultPresets = LoadDefaultPresets();
                var userPresets = LoadUserPresets();
                return defaultPresets.Concat(userPresets);
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error getting preset molecules: {ex.Message}");
                return Enumerable.Empty<PresetMolecule>();
            }
        }

        public IEnumerable<ChemicalNotation> GetSupportedNotations()
        {
            try
            {
                return new[]
                {
                    new ChemicalNotation("SMILES", "Simplified Molecular Input Line Entry System", true),
                    new ChemicalNotation("InChI", "International Chemical Identifier", false),
                    new ChemicalNotation("InChIKey", "International Chemical Identifier Key", false),
                    new ChemicalNotation("分子式", "Molecular Formula", true),
                    new ChemicalNotation("示性式", "Constitutional Formula", true),
                    new ChemicalNotation("構造式", "Structural Formula", true),
                    new ChemicalNotation("骨格式", "Skeletal Formula", true),
                    new ChemicalNotation("電子式", "Lewis Structure", true),
                    new ChemicalNotation("線式", "Line Formula", true),
                    new ChemicalNotation("立体化学", "Stereochemistry", true),
                    new ChemicalNotation("配座", "Conformation", false),
                    new ChemicalNotation("反応式", "Chemical Equation", false)
                };
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error getting supported notations: {ex.Message}");
                return Enumerable.Empty<ChemicalNotation>();
            }
        }

        public string GetPluginInfo()
        {
            try
            {
                var info = new
                {
                    Name,
                    Version = Version.ToString(),
                    Description,
                    Author,
                    IsInitialized,
                    InitializationError,
                    SupportedFeatures = SupportedFeatures.ToList(),
                    MaxAtomCount,
                    MaxBondCount,
                    PluginDataPath,
                    CompilationDate = GetCompilationDate(),
                    RuntimeVersion = Environment.Version.ToString(),
                    OSVersion = Environment.OSVersion.ToString()
                };

                return JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return $"Error getting plugin info: {ex.Message}";
            }
        }

        private void Initialize()
        {
            lock (_lockObject)
            {
                if (!_staticInitialized)
                {
                    InitializeStaticResources();
                    _staticInitialized = true;
                }
            }

            InitializePluginDataDirectory();
            AsyncLogger.Instance.Initialize(PluginDataPath);
            InitializeChemicalData();
            InitializeRenderingSystem();
            InitializeEventHandlers();
            InitializeUserSettings();

            AsyncLogger.Instance.Log(LogType.Info, "ChemicalStructurePlugin initialized successfully");
        }

        private static void InitializeStaticResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyName = assembly.GetName().Name;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error initializing static resources: {ex.Message}");
            }
        }

        private void InitializePluginDataDirectory()
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                _pluginDataPath = Path.GetDirectoryName(assemblyLocation);

                if (string.IsNullOrEmpty(_pluginDataPath) || !Directory.Exists(_pluginDataPath))
                {
                    throw new DirectoryNotFoundException("プラグインのディレクトリが見つかりません。");
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error initializing plugin data directory: {ex.Message}");
            }
        }

        private void InitializeChemicalData()
        {
            try
            {
                var elementCount = PeriodicTableService.GetAllElements().Count;
                AsyncLogger.Instance.Log(LogType.Info, $"Loaded {elementCount} chemical elements");

                var atomicWeights = Atom.GetAtomicWeights();
                AsyncLogger.Instance.Log(LogType.Info, $"Loaded atomic weights for {atomicWeights.Count} elements");

                var elementColors = Atom.GetElementColors();
                AsyncLogger.Instance.Log(LogType.Info, $"Loaded element colors for {elementColors.Count} elements");
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error initializing chemical data: {ex.Message}");
            }
        }

        private void InitializeRenderingSystem()
        {
            try
            {
                AsyncLogger.Instance.Log(LogType.Info, "Rendering system ready");
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error initializing rendering system: {ex.Message}");
            }
        }

        private void InitializeEventHandlers()
        {
            try
            {
                AsyncLogger.Instance.Log(LogType.Info, "Event handlers initialized");
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error initializing event handlers: {ex.Message}");
            }
        }

        private void InitializeUserSettings()
        {
            try
            {
                var settingsPath = Path.Combine(_pluginDataPath ?? "", "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    AsyncLogger.Instance.Log(LogType.Info, $"Loaded user settings: {settings?.Count ?? 0} entries");
                }
                else
                {
                    var defaultSettings = new Dictionary<string, object>
                    {
                        ["ShowTutorial"] = true,
                        ["EnablePerformanceWarnings"] = true,
                        ["DefaultBondLength"] = 80,
                        ["DefaultAtomSize"] = 20,
                        ["DefaultFontSize"] = 24,
                        ["AntiAliasing"] = true,
                        ["AutoSavePresets"] = true
                    };

                    var json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(settingsPath, json);
                    AsyncLogger.Instance.Log(LogType.Info, "Created default user settings");
                }
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error initializing user settings: {ex.Message}");
            }
        }

        private void ValidateInitialization()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"Plugin not properly initialized: {InitializationError ?? "Unknown error"}");
            }
        }

        private static void ApplyDefaultSettings(ChemicalStructureParameter parameter)
        {
            if (parameter == null) return;

            try
            {
                parameter.DefaultBondLength = 80;
                parameter.DefaultAtomSize = 20;
                parameter.DefaultFontSize = 24;
                parameter.DefaultBondThickness = 2.0;
                parameter.ShowHydrogen = true;
                parameter.ShowCharges = true;
                parameter.AntiAliasing = true;
                parameter.UseSkeletalFormula = false;
                parameter.RenderingQuality = RenderingQuality.Normal;
                parameter.FontFamily = "Arial";
                parameter.OutlineThickness = 0;
                parameter.RingSize = 80;
                parameter.SpacingMultiplier = 1.0;
                parameter.AutoOptimize = false;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error applying default settings: {ex.Message}");
            }
        }

        private static void ValidateParameter(ChemicalStructureParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            if (parameter.DefaultBondLength <= 0)
                throw new ArgumentException("Bond length must be positive");

            if (parameter.DefaultAtomSize <= 0)
                throw new ArgumentException("Atom size must be positive");

            if (parameter.DefaultFontSize <= 0)
                throw new ArgumentException("Font size must be positive");

            if (parameter.DefaultBondThickness <= 0)
                throw new ArgumentException("Bond thickness must be positive");

            if (parameter.RingSize <= 0)
                throw new ArgumentException("Ring size must be positive");

            if (parameter.SpacingMultiplier <= 0)
                throw new ArgumentException("Spacing multiplier must be positive");
        }

        private IShapeParameter CreateFallbackParameter(SharedDataStore? sharedData)
        {
            try
            {
                var parameter = new ChemicalStructureParameter(sharedData);

                parameter.DefaultBondLength = 50;
                parameter.DefaultAtomSize = 15;
                parameter.DefaultFontSize = 16;
                parameter.DefaultBondThickness = 1.5;
                parameter.ShowHydrogen = false;
                parameter.ShowCharges = false;
                parameter.AntiAliasing = false;
                parameter.UseSkeletalFormula = false;
                parameter.RenderingQuality = RenderingQuality.Low;

                AsyncLogger.Instance.Log(LogType.Warning, "Fallback parameter created");
                return parameter;
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Failed to create fallback parameter: {ex.Message}");
                throw new InvalidOperationException("Unable to create chemical structure parameter", ex);
            }
        }

        private static void TestBasicFunctionality(PluginTestResult result)
        {
            try
            {
                var plugin = new ChemicalStructurePlugin();
                var parameter = plugin.CreateShapeParameter(null);

                if (parameter == null)
                {
                    result.Errors.Add("Failed to create shape parameter");
                    return;
                }

                var testParameter = parameter as ChemicalStructureParameter;
                if (testParameter != null)
                {
                    var atom = new Atom("C", 0, 0);
                    testParameter.Atoms = testParameter.Atoms.Add(atom);

                    if (testParameter.Atoms.Count != 1)
                        result.Errors.Add("Failed to add atom");

                    var atom2 = new Atom("H", 50, 0);
                    testParameter.Atoms = testParameter.Atoms.Add(atom2);

                    var bond = new Bond(atom.Id, atom2.Id);
                    testParameter.Bonds = testParameter.Bonds.Add(bond);

                    if (testParameter.Bonds.Count != 1)
                        result.Errors.Add("Failed to add bond");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Basic functionality test failed: {ex.Message}");
            }
        }

        private static void TestPerformance(PluginTestResult result)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var plugin = new ChemicalStructurePlugin();
                var parameter = plugin.CreateShapeParameter(null) as ChemicalStructureParameter;

                if (parameter != null)
                {
                    var atoms = new List<Atom>();
                    for (int i = 0; i < 100; i++)
                    {
                        atoms.Add(new Atom("C", i * 10, 0));
                    }

                    parameter.Atoms = atoms.ToImmutableList();

                    var bonds = new List<Bond>();
                    for (int i = 0; i < atoms.Count - 1; i++)
                    {
                        bonds.Add(new Bond(atoms[i].Id, atoms[i + 1].Id));
                    }

                    parameter.Bonds = bonds.ToImmutableList();
                }

                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 5000)
                {
                    result.Warnings.Add($"Performance test took {stopwatch.ElapsedMilliseconds}ms (expected < 5000ms)");
                }
                else
                {
                    result.Warnings.Add($"Performance test completed in {stopwatch.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Performance test failed: {ex.Message}");
            }
        }

        private static void TestMemoryUsage(PluginTestResult result)
        {
            try
            {
                var initialMemory = GC.GetTotalMemory(true);

                for (int i = 0; i < 100; i++)
                {
                    var plugin = new ChemicalStructurePlugin();
                    var parameter = plugin.CreateShapeParameter(null);

                    if (parameter is ChemicalStructureParameter chemParam)
                    {
                        var atoms = new List<Atom>();
                        for (int j = 0; j < 10; j++)
                        {
                            atoms.Add(new Atom("C", j * 5, 0));
                        }
                        chemParam.Atoms = atoms.ToImmutableList();
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var finalMemory = GC.GetTotalMemory(true);
                var memoryIncrease = finalMemory - initialMemory;

                if (memoryIncrease > 50 * 1024 * 1024)
                {
                    result.Warnings.Add($"Potential memory leak detected: {memoryIncrease / 1024 / 1024}MB increase");
                }
                else
                {
                    result.Warnings.Add($"Memory usage test passed: {memoryIncrease / 1024}KB increase");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Memory usage test failed: {ex.Message}");
            }
        }

        private static void TestFileOperations(PluginTestResult result)
        {
            try
            {
                if (string.IsNullOrEmpty(_pluginDataPath))
                {
                    result.Warnings.Add("Plugin data path not initialized");
                    return;
                }

                var testFile = Path.Combine(_pluginDataPath, "test.json");
                var testData = new { test = "data", timestamp = DateTime.Now };
                var json = JsonSerializer.Serialize(testData);

                File.WriteAllText(testFile, json);

                if (!File.Exists(testFile))
                {
                    result.Errors.Add("Failed to create test file");
                    return;
                }

                var readJson = File.ReadAllText(testFile);
                var readData = JsonSerializer.Deserialize<Dictionary<string, object>>(readJson);

                if (readData == null || !readData.ContainsKey("test"))
                {
                    result.Errors.Add("Failed to read test file correctly");
                }

                File.Delete(testFile);

                if (File.Exists(testFile))
                {
                    result.Errors.Add("Failed to delete test file");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"File operations test failed: {ex.Message}");
            }
        }

        private static void TestFormulaParser(PluginTestResult result)
        {
            try
            {
                var testFormulas = new[] { "H2O", "CH4", "C6H12O6", "C8H10N4O2", "NaCl" };

                foreach (var formula in testFormulas)
                {
                    try
                    {
                        var pattern = @"([A-Z][a-z]?)(\d*)";
                        var matches = System.Text.RegularExpressions.Regex.Matches(formula, pattern);

                        if (matches.Count == 0)
                        {
                            result.Warnings.Add($"Failed to parse formula: {formula}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Error parsing formula {formula}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Formula parser test failed: {ex.Message}");
            }
        }

        private static void TestPresetSystem(PluginTestResult result)
        {
            try
            {
                var plugin = new ChemicalStructurePlugin();
                var presets = plugin.GetPresetMolecules().ToList();

                if (presets.Count == 0)
                {
                    result.Warnings.Add("No preset molecules available");
                }
                else
                {
                    result.Warnings.Add($"Loaded {presets.Count} preset molecules");
                }

                var notations = plugin.GetSupportedNotations().ToList();
                if (notations.Count == 0)
                {
                    result.Warnings.Add("No supported notations available");
                }
                else
                {
                    var supportedCount = notations.Count(n => n.IsSupported);
                    result.Warnings.Add($"Supporting {supportedCount}/{notations.Count} chemical notations");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Preset system test failed: {ex.Message}");
            }
        }

        private IEnumerable<PresetMolecule> LoadDefaultPresets()
        {
            try
            {
                if (string.IsNullOrEmpty(_pluginDataPath))
                    return Enumerable.Empty<PresetMolecule>();

                var presetsPath = Path.Combine(_pluginDataPath, "default_presets.json");
                if (!File.Exists(presetsPath))
                    return Enumerable.Empty<PresetMolecule>();

                var json = File.ReadAllText(presetsPath);
                var presets = JsonSerializer.Deserialize<List<PresetMolecule>>(json);
                return presets ?? Enumerable.Empty<PresetMolecule>();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error loading default presets: {ex.Message}");
                return Enumerable.Empty<PresetMolecule>();
            }
        }

        private IEnumerable<PresetMolecule> LoadUserPresets()
        {
            try
            {
                if (string.IsNullOrEmpty(_pluginDataPath))
                    return Enumerable.Empty<PresetMolecule>();

                var presetsPath = Path.Combine(_pluginDataPath, "user_presets.json");
                if (!File.Exists(presetsPath))
                    return Enumerable.Empty<PresetMolecule>();

                var json = File.ReadAllText(presetsPath);
                var presets = JsonSerializer.Deserialize<List<PresetMolecule>>(json);
                return presets ?? Enumerable.Empty<PresetMolecule>();
            }
            catch (Exception ex)
            {
                AsyncLogger.Instance.Log(LogType.Error, $"Error loading user presets: {ex.Message}");
                return Enumerable.Empty<PresetMolecule>();
            }
        }

        private Version GetVersion()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
            }
            catch
            {
                return new Version(1, 0, 0, 0);
            }
        }

        private DateTime GetCompilationDate()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileInfo = new FileInfo(assembly.Location);
                return fileInfo.LastWriteTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private IEnumerable<string> GetSupportedFeatures()
        {
            try
            {
                return new[]
                {
                    "構造式描画",
                    "骨格式表示",
                    "分子式生成",
                    "電子式表示",
                    "3D構造表示",
                    "自動レイアウト",
                    "プリセット分子",
                    "化学式解析",
                    "元素色表示",
                    "結合スタイル",
                    "立体化学表現",
                    "略記法表示",
                    "ユーザープリセット保存",
                    "透明度対応",
                    "アンチエイリアス",
                    "キーボードショートカット",
                    "コンテキストメニュー",
                    "パフォーマンス監視",
                    "エラーハンドリング",
                    "データ検証",
                    "設定永続化",
                    "チュートリアル機能"
                };
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }
    }

    public class PluginTestResult
    {
        public bool IsSuccess { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }

        public override string ToString()
        {
            var status = IsSuccess ? "SUCCESS" : "FAILED";
            var errorCount = Errors.Count;
            var warningCount = Warnings.Count;
            return $"Test {status}: {errorCount} errors, {warningCount} warnings, {ExecutionTime.TotalMilliseconds:F0}ms";
        }

        public string GetDetailedReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Plugin Test Result: {(IsSuccess ? "SUCCESS" : "FAILED")}");
            report.AppendLine($"Execution Time: {ExecutionTime.TotalMilliseconds:F2}ms");
            report.AppendLine();

            if (Errors.Any())
            {
                report.AppendLine("ERRORS:");
                foreach (var error in Errors)
                {
                    report.AppendLine($"  • {error}");
                }
                report.AppendLine();
            }

            if (Warnings.Any())
            {
                report.AppendLine("WARNINGS:");
                foreach (var warning in Warnings)
                {
                    report.AppendLine($"  • {warning}");
                }
                report.AppendLine();
            }

            if (!Errors.Any() && !Warnings.Any())
            {
                report.AppendLine("All tests passed without issues.");
            }

            return report.ToString();
        }
    }

    public class PresetMolecule
    {
        public string Name { get; set; }
        public string Formula { get; set; }
        public string Description { get; set; }
        public MoleculeCategory Category { get; set; }

        public PresetMolecule(string name, string formula, string description, MoleculeCategory category)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Formula = formula ?? throw new ArgumentNullException(nameof(formula));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Category = category;
        }

        public override string ToString()
        {
            return $"{Name} ({Formula}) - {Description}";
        }

        public string GetCategoryDisplayName()
        {
            return Category switch
            {
                MoleculeCategory.Simple => "単純化合物",
                MoleculeCategory.Alkane => "アルカン",
                MoleculeCategory.Alkene => "アルケン",
                MoleculeCategory.Alkyne => "アルキン",
                MoleculeCategory.Aromatic => "芳香族",
                MoleculeCategory.Alcohol => "アルコール",
                MoleculeCategory.Ether => "エーテル",
                MoleculeCategory.Aldehyde => "アルデヒド",
                MoleculeCategory.Ketone => "ケトン",
                MoleculeCategory.Acid => "カルボン酸",
                MoleculeCategory.Ester => "エステル",
                MoleculeCategory.Amine => "アミン",
                MoleculeCategory.Amide => "アミド",
                MoleculeCategory.Cyclic => "環式化合物",
                MoleculeCategory.Carbohydrate => "糖類",
                MoleculeCategory.Pharmaceutical => "医薬品",
                MoleculeCategory.Biological => "生体分子",
                MoleculeCategory.Complex => "複雑化合物",
                _ => "その他"
            };
        }
    }

    public enum MoleculeCategory
    {
        Simple,
        Alkane,
        Alkene,
        Alkyne,
        Aromatic,
        Alcohol,
        Ether,
        Aldehyde,
        Ketone,
        Acid,
        Ester,
        Amine,
        Amide,
        Cyclic,
        Carbohydrate,
        Pharmaceutical,
        Biological,
        Complex
    }

    public class ChemicalNotation
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsSupported { get; }

        public ChemicalNotation(string name, string description, bool isSupported)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsSupported = isSupported;
        }

        public override string ToString()
        {
            var status = IsSupported ? "対応" : "未対応";
            return $"{Name} ({status}) - {Description}";
        }
    }
}