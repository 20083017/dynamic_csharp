using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{


    public class AppConfig
    {
        // 基础配置属性
        public string LogPath { get; }
        public double RangeScale { get; }
        public bool ShouldCenterLaser { get; }
        public Point LDSPosiMM { get; }
        public double XOffset { get; }
        public double AngleRotate { get; }

        // 时间与激光参数
        public int StartTimestamp { get; }
        public int EndTimestamp { get; }
        public double TimeScale { get; }
        public double MinLaserDegree { get; }
        public double MaxLaserDegree { get; }
        public double MinExclusiveLaserDegree { get; }
        public double MaxExclusiveLaserDegree { get; }
        public bool EnableExclusive { get; }

        // 高级功能配置
        public bool FitSlamLaser { get; }
        public int FollowWallAngleHoldDistance { get; }

        // 构造函数
        public AppConfig(
            string logPath,
            double rangeScale,
            bool shouldCenterLaser,
            Point ldsPosiMM,
            double xOffset,
            double angleRotate,
            int startTimestamp,
            int endTimestamp,
            double timeScale,
            double minLaserDegree,
            double maxLaserDegree,
            double minExclusiveLaserDegree,
            double maxExclusiveLaserDegree,
            bool enableExclusive,
            bool fitSlamLaser,
            int followWallAngleHoldDistance)
        {
            LogPath = logPath;
            RangeScale = rangeScale;
            ShouldCenterLaser = shouldCenterLaser;
            LDSPosiMM = ldsPosiMM;
            XOffset = xOffset;
            AngleRotate = angleRotate;
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            TimeScale = timeScale;
            MinLaserDegree = minLaserDegree;
            MaxLaserDegree = maxLaserDegree;
            MinExclusiveLaserDegree = minExclusiveLaserDegree;
            MaxExclusiveLaserDegree = maxExclusiveLaserDegree;
            EnableExclusive = enableExclusive;
            FitSlamLaser = fitSlamLaser;
            FollowWallAngleHoldDistance = followWallAngleHoldDistance;
        }

        public static AppConfig LoadFromXml(string configPath, string[] commandLineArgs = null)
        {
            var doc = XDocument.Load(configPath);

            string logPath;
            if (commandLineArgs.GetLength(0) > 1)
            {
                logPath = commandLineArgs[1];
            }
            else
            {
                logPath = GetXElementValue<string>(doc, "Log");
            }

            return new AppConfig(
                logPath,
                GetXElementValue<double>(doc, "RangeScale"),
                GetXElementValue<bool>(doc, "CenterLaser"),
                new Point(
                    GetXElementValue<double>(doc, "LDSPosi_x_mm"),
                    GetXElementValue<double>(doc, "LDSPosi_y_mm")
                ),
                GetXElementValue<double>(doc, "LDSPosi_x_mm"),
                GetXElementValue<double>(doc, "AngleRotate", 0.0) * Math.PI / 180,
                GetXElementValue<int>(doc, "StartTimestamp"),
                GetXElementValue<int>(doc, "EndTimestamp"),
                GetXElementValue<double>(doc, "TimeScale"),
                GetXElementValue<double>(doc, "MinLaserDegree"),
                GetXElementValue<double>(doc, "MaxLaserDegree"),
                GetXElementValue<double>(doc, "MinExclusiveLaserDegree"),
                GetXElementValue<double>(doc, "MaxExclusiveLaserDegree"),
                GetXElementValue<bool>(doc, "EnableExclusive"),
                GetXElementValue<bool>(doc, "FitSlamLaser"),
                GetXElementValue<int>(doc, "FollowWallAngleHoldDistance")
            );
        }

        // 泛型方法统一解析 XML 值
        private static T GetXElementValue<T>(XDocument doc, string elementName, T defaultValue = default)
        {
            var element = doc.Descendants(elementName).FirstOrDefault();
            return element != null
                ? (T)Convert.ChangeType(element.Value, typeof(T))
                : defaultValue;
        }
    }

    //public class DebugLogEntry
    //{
    //    public DateTime Timestamp { get; set; }
    //    public string ThreadId { get; set; }
    //    public string StackTrace { get; set; }
    //}

    //public class LogProcessor
    //{
    //    private readonly ConcurrentBag<DebugLogEntry> _debugs = new();
    //    AppConfig AppConfig { get; set; }


    //    public List<DebugLogEntry> ProcessLogFile(string filePath, int maxThreads, AppConfig appConfig)
    //    {
    //        AppConfig = appConfig;
    //        var chunks = SplitFileIntoChunks(filePath, maxThreads).ToList();

    //        Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, chunk =>
    //        {
    //            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    //            using var reader = new StreamReader(fs);
    //            fs.Seek(chunk.Start, SeekOrigin.Begin);

    //            string line;
    //            while (fs.Position < chunk.End && (line = reader.ReadLine()) != null)
    //            {
    //                var entry = ParseLogLine(line);
    //                //if (entry is ErrorLogEntry error) _errors.Add(error);
    //                //else if (entry is InfoLogEntry info) _infos.Add(info);
    //                 if (entry is DebugLogEntry debug) _debugs.Add(debug);
    //            }
    //        });

    //        return _debugs.ToList();
    //    }

    //    public IEnumerable<(long Start, long End)> SplitFileIntoChunks(string filePath, int chunkCount)
    //    {
    //        var fileInfo = new FileInfo(filePath);
    //        long chunkSize = fileInfo.Length / chunkCount;
    //        long position = 0;

    //        for (int i = 0; i < chunkCount; i++)
    //        {
    //            long end = (i == chunkCount - 1) ? fileInfo.Length : position + chunkSize;

    //            // 确保块结束在换行符处，避免截断行
    //            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    //            stream.Seek(end, SeekOrigin.Begin);
    //            using var reader = new StreamReader(stream);
    //            while (!reader.EndOfStream && reader.Read() != '\n') { }
    //            end = stream.Position;

    //            yield return (position, end);
    //            position = end;
    //        }
    //    }

    //    public object? ParseLogLine(string line)
    //    {
    //        if (string.IsNullOrEmpty(line)) return null;

    //        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    //        if (parts.Length < 2) return null;

    //        return line switch
    //        {
    //            _ when line.StartsWith("[DEBUG]") && parts.Length >= 3 => new DebugLogEntry
    //            {
    //                // 修复索引越界问题
    //                Timestamp = DateTime.TryParse(parts[0], out var ts) ? ts : DateTime.MinValue,
    //                ThreadId = parts.Length >= 3 ? parts[1] : "UNKNOWN",
    //                //StackTrace = parts.Length > 3 ? string.Join('|', parts.Skip(3)) : "No stack trace"
    //            },
    //            _ => null
    //        };

    //    }

    //}

    public class LogProcessor
    {
        private Rect realGridMapRange = new Rect();
        private SLAMPose lastSLAMPose = new SLAMPose();
        private AppConfig AppConfig { get; set; }
        private HashSet<string> _checkboxNames = new HashSet<string>();
        private List<ConcurrentBag<KeyValuePair<uint, List<LaserPoint>>>> AllLaserList;
        private List<List<KeyValuePair<uint, List<LaserPoint>>>> AllResultLaserList;

        const int stIndSlamPoseMarkerForRRLoader = 2;
        const string slamPoseMarkerForRRLoador = @"slamPose";

        private const UInt16 LDS_FLAG_INVALID = 0x8000;
        private const UInt16 LDS_FLAG_FILTER = 0x2000;
        private const UInt16 LDS_ALL_TAG = 0x0FFF;

        public void ProcessLogFile(string filePath, int maxThreads, AppConfig appConfig)
        {
            AppConfig = appConfig;
            var chunks = SplitFileIntoChunks(filePath, maxThreads).ToList();

            // 初始化 AllLaserList
            ParseAllLinesNames(appConfig.StartTimestamp, appConfig.EndTimestamp);
            AllLaserList = _checkboxNames
                .Select(_ => new ConcurrentBag<KeyValuePair<uint, List<LaserPoint>>>())
                .ToList();
            AllResultLaserList = _checkboxNames
                .Select(_ => new List<KeyValuePair<uint, List<LaserPoint>>>())
                .ToList();

            // 并行处理日志文件
            Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, chunk =>
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(fs);
                fs.Seek(chunk.Start, SeekOrigin.Begin);

                string line;
                while (fs.Position < chunk.End && (line = reader.ReadLine()) != null)
                {
                    ParseLogLine(line);
                }
            });

            // 对 AllLaserList 中的每个 ConcurrentBag 进行排序
            SortAllLaserList();
        }

        public IEnumerable<(long Start, long End)> SplitFileIntoChunks(string filePath, int chunkCount)
        {
            var fileInfo = new FileInfo(filePath);
            long chunkSize = fileInfo.Length / chunkCount;
            long position = 0;

            for (int i = 0; i < chunkCount; i++)
            {
                long end = (i == chunkCount - 1) ? fileInfo.Length : position + chunkSize;

                // 确保块结束在换行符处，避免截断行
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                stream.Seek(end, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream && reader.Read() != '\n') { }
                end = stream.Position;

                yield return (position, end);
                position = end;
            }
        }

        private uint ParseTimestamp(string line)
        {
            try
            {
                var str = line.Substring(0, line.IndexOf(' '));
                if (str.IndexOf('.') >= 0)
                {
                    return (uint)(double.Parse(str) * 1000);
                }
                else
                {
                    return uint.Parse(str);
                }
            }
            catch
            {
                return 0;
            }
        }

        private bool TryParseLaserLineNames(string line)
        {
            var nums = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (nums.Length > 1 && nums[1].Contains("Laser") && !nums[1].Contains("Multi"))
            {
                _checkboxNames.Add(nums[1]);
                return _checkboxNames.Count == 9 || _checkboxNames.Count == 3;
            }
            return false;
        }

        private void ParseAllLinesNames(int startTimestamp, int endTimestamp)
        {
            using var streamReader = new StreamReader(AppConfig.LogPath);
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                uint timestamp = ParseTimestamp(line);
                if (timestamp >= startTimestamp && TryParseLaserLineNames(line))
                {
                    if (timestamp > endTimestamp && endTimestamp >= 0)
                    {
                        break;
                    }
                }
            }
        }

        private bool TryParseLaserLine(string line, out Dictionary<string, List<LaserPoint>> dicts)
        {
            dicts = new Dictionary<string, List<LaserPoint>>();
            var nums = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (nums.Length < 2) return false;

            string laserName = nums[1];
            if (_checkboxNames.Contains(laserName))
            {
                for (int i = 5; i + 2 < nums.Length; i += 3)
                {
                    int rawIntensity = int.Parse(nums[i + 2]);
                    if ((rawIntensity & LDS_FLAG_INVALID) == 0 && (rawIntensity & LDS_FLAG_FILTER) == 0)
                    {
                        var laserPoint = new LaserPoint
                        {
                            Bearing = double.Parse(nums[i]) + AppConfig.AngleRotate,
                            Range = double.Parse(nums[i + 1]) * 1000,
                            Intensity = rawIntensity & LDS_ALL_TAG,
                            ScanSLAMPose = lastSLAMPose
                        };

                        if (!dicts.ContainsKey(laserName))
                        {
                            dicts[laserName] = new List<LaserPoint>();
                        }
                        dicts[laserName].Add(laserPoint);
                    }
                }

                return true;
            }

            int index = line.IndexOf(" ");

            if ((string.Compare(line, index + 1, slamPoseMarkerForRRLoador, 0, slamPoseMarkerForRRLoador.Length) == 0))
            {
                //var nums = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                double x = double.Parse(nums[stIndSlamPoseMarkerForRRLoader]) * 1000;
                double y = double.Parse(nums[stIndSlamPoseMarkerForRRLoader + 1]) * 1000;
                double bearing = double.Parse(nums[stIndSlamPoseMarkerForRRLoader + 2]);

                this.lastSLAMPose = new SLAMPose(x, y, bearing);
                if (lastSLAMPose.X < realGridMapRange.StartPoint.X)
                {
                    realGridMapRange.StartPoint = new Point(lastSLAMPose.X, realGridMapRange.StartPoint.Y);
                }

                if (lastSLAMPose.Y < realGridMapRange.StartPoint.Y)
                {
                    realGridMapRange.StartPoint = new Point(realGridMapRange.StartPoint.X, lastSLAMPose.Y);
                }

                if (lastSLAMPose.X > realGridMapRange.EndPoint.X)
                {
                    realGridMapRange.EndPoint = new Point(lastSLAMPose.X, realGridMapRange.EndPoint.Y);
                }

                if (lastSLAMPose.Y > realGridMapRange.EndPoint.Y)
                {
                    realGridMapRange.EndPoint = new Point(realGridMapRange.EndPoint.X, lastSLAMPose.Y);
                }
            }


            return false;
        }

        public void ParseLogLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;

            uint timestamp = ParseTimestamp(line);
            if (timestamp < AppConfig.StartTimestamp || (timestamp > AppConfig.EndTimestamp && AppConfig.EndTimestamp >= 0))
            {
                return;
            }

            if (TryParseLaserLine(line, out var dicts))
            {
                foreach (var laserName in _checkboxNames)
                {
                    if (dicts.TryGetValue(laserName, out var laserPoints) && laserPoints.Count > 0)
                    {
                        int index = _checkboxNames.ToList().IndexOf(laserName);
                        AllLaserList[index].Add(new KeyValuePair<uint, List<LaserPoint>>(timestamp, laserPoints));
                    }
                }
            }
        }

        private void SortAllLaserList()
        {
            for (int i = 0; i < AllLaserList.Count; i++)
            {
                // 将 ConcurrentBag 转换为 List 并排序
                var sortedList = AllLaserList[i].OrderBy(pair => pair.Key).ToList();

                // 替换原来的 ConcurrentBag
                AllResultLaserList[i] = new List<KeyValuePair<uint, List<LaserPoint>>>(sortedList);
            }
        }

        public List<List<KeyValuePair<uint, List<LaserPoint>>>> GetLaserData()
        {
            return AllResultLaserList;
        }
        public HashSet<string> getCheckNames()
        {
            return _checkboxNames;
        }
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly ObservableCollection<CanvasController> _controllers =
               new ObservableCollection<CanvasController>();


        private async Task<IEnumerable<CanvasController>> GenerateInitialData()
        {
            // 模拟数据生成的延迟
            await Task.Delay(1000);

            // 生成初始数据
            var initialData = new List<CanvasController>();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                initialData.Add(new CanvasController
                {
                    DisplayName = "初始画布1",
                    TargetCanvas = new Canvas
                    {
                        Background = new SolidColorBrush(Colors.LightBlue),
                        Width = 300,
                        Height = 200,
                        Visibility = Visibility.Collapsed
                    },
                    IsVisible = false
                });

                initialData.Add(new CanvasController
                {
                    DisplayName = "初始画布2",
                    TargetCanvas = new Canvas
                    {
                        Background = new SolidColorBrush(Colors.LightGreen),
                        Width = 300,
                        Height = 200,
                        Visibility = Visibility.Collapsed
                    },
                    IsVisible = false
                });

                // 添加至可视化树
                if (Content is Panel rootPanel)
                {
                    rootPanel.Children.Add(initialData[0].TargetCanvas);
                    rootPanel.Children.Add(initialData[1].TargetCanvas);
                }
            });

            return initialData;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var initialData = await Task.Run(() => GenerateInitialData());
            //AddRange(_controllers,initialData);
            _controllers.AddRange(initialData);

            // 创建容器并添加到主界面
            var builder = new CanvasLayerBuilder();
            // 设置 NameScope
            NameScope.SetNameScope(MainGrid, new NameScope());

            var container = builder.BuildContainer();
            
            MainGrid.Children.Add(container);
            MainGrid.RegisterName(container.Name, container);

        }

        public MainWindow()
        {
            InitializeComponent();
            ControllerHost.ItemsSource = _controllers;

            // 初始化预设Canvas控制器
            AddCanvasController("主画布", MainCanvas);
            AddCanvasController("标注层", AnnotationLayer);
            AddCanvasController("网格系统", GridSystem);

            Loaded += MainWindow_Loaded;

        }

        private void AddCanvasController(string name, Canvas canvas)
        {
            _controllers.Add(new CanvasController
            {
                DisplayName = name,
                TargetCanvas = canvas,
                IsVisible = false // 默认隐藏
            });
        }


        private void AddController_Click(object sender, RoutedEventArgs e)
        {

            // 动态访问示例
            var container = MainGrid.FindName("CanvasContainer") as Grid;
            if (container != null)
            {
                var laserPanel = container.FindName("LaserPanel0") as Canvas;
                if (laserPanel != null)
                {
                    // 进行操作
                    laserPanel.Background = new SolidColorBrush(Colors.Red);
                    return;
                }
            }

            // 创建新Canvas（根据需求可选）
            var newCanvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(
                    (byte)Random.Shared.Next(256),
                    (byte)Random.Shared.Next(256),
                    (byte)Random.Shared.Next(256))),
                Width = 300,
                Height = 200,
                Visibility = Visibility.Collapsed
            };

            // 添加至可视化树
            if (Content is Panel rootPanel)
            {
                rootPanel.Children.Add(newCanvas);
            }

            // 创建关联控制器
            AddCanvasController($"画布{_controllers.Count + 1}", newCanvas);


        }
    }

    // 数据模型类
    public class CanvasController : INotifyPropertyChanged
    {
        private bool _isVisible;
        public string DisplayName { get; set; }
        public Canvas TargetCanvas { get; set; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                TargetCanvas.Visibility = value ?
                    Visibility.Visible :
                    Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> col, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                col.Add(item);
            }
        }
    }


    public class CanvasLayerBuilder
    {
        // 配置层定义（可从配置文件加载）
        private static readonly LayerConfig[] _layerConfigs =
        {
            new LayerConfig(5, "LaserPanel", isTopLayer: true),
            new LayerConfig(4, "LaserPanel4"),
            new LayerConfig(3, "LaserPanel3"),
            new LayerConfig(2, "LaserPanel2"),
            new LayerConfig(1, "LaserPanel1"),
            new LayerConfig(0, "LaserPanel0", isBaseLayer: true)
        };

        // 动态创建容器
        public Grid BuildContainer()
        {
            var container = new Grid
            {
                Name = "CanvasContainer",
                Width = 1024,
                Height = 800
            };

            // 设置 NameScope
            NameScope.SetNameScope(container, new NameScope());

            // 按ZIndex降序添加保证渲染顺序
            foreach (var config in _layerConfigs.OrderByDescending(c => c.ZIndex))
            {
                var canvas = CreateLayerCanvas(config);
                container.Children.Add(canvas);
                //NameScope.SetNameScope(canvas, new NameScope());
                container.RegisterName(canvas.Name, canvas);
            }

            return container;
        }

        // 画布层工厂方法
        private Canvas CreateLayerCanvas(LayerConfig config)
        {
            var canvas = new Canvas
            {
                Name = config.LayerName,
                Background = Brushes.AliceBlue,
                IsHitTestVisible = false,
                Visibility = Visibility.Visible
            };

            // 设置附加属性
            Panel.SetZIndex(canvas, config.ZIndex);

            // 特殊层处理
            if (config.IsTopLayer) ConfigureTopLayer(canvas);
            if (config.IsBaseLayer) ConfigureBaseLayer(canvas);

            return canvas;
        }

        // 顶层特殊配置
        private void ConfigureTopLayer(Canvas canvas)
        {
            canvas.Tag = "HighPriorityLayer";
            canvas.RenderTransform = new ScaleTransform();
        }

        // 底层特殊配置
        private void ConfigureBaseLayer(Canvas canvas)
        {
            canvas.AllowDrop = true;
            canvas.ClipToBounds = true;
        }

        // 配置参数类
        private class LayerConfig
        {
            public int ZIndex { get; }
            public string LayerName { get; }
            public bool IsTopLayer { get; }
            public bool IsBaseLayer { get; }

            public LayerConfig(int zIndex, string layerName, bool isTopLayer = false, bool isBaseLayer = false)
            {
                ZIndex = zIndex;
                LayerName = layerName;
                IsTopLayer = isTopLayer;
                IsBaseLayer = isBaseLayer;
            }
        }
    }

}