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


    public class DebugLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string ThreadId { get; set; }
        public string StackTrace { get; set; }
    }

    public class LogProcessor
    {
        private readonly ConcurrentBag<DebugLogEntry> _debugs = new();
        AppConfig AppConfig { get; set; }


        public List<DebugLogEntry> ProcessLogFile(string filePath, int maxThreads, AppConfig appConfig)
        {
            AppConfig = appConfig;
            var chunks = SplitFileIntoChunks(filePath, maxThreads).ToList();

            Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, chunk =>
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(fs);
                fs.Seek(chunk.Start, SeekOrigin.Begin);

                string line;
                while (fs.Position < chunk.End && (line = reader.ReadLine()) != null)
                {
                    var entry = ParseLogLine(line);
                    //if (entry is ErrorLogEntry error) _errors.Add(error);
                    //else if (entry is InfoLogEntry info) _infos.Add(info);
                     if (entry is DebugLogEntry debug) _debugs.Add(debug);
                }
            });

            return _debugs.ToList();
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

        public object? ParseLogLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            return line switch
            {
                _ when line.StartsWith("[DEBUG]") && parts.Length >= 3 => new DebugLogEntry
                {
                    // 修复索引越界问题
                    Timestamp = DateTime.TryParse(parts[0], out var ts) ? ts : DateTime.MinValue,
                    ThreadId = parts.Length >= 3 ? parts[1] : "UNKNOWN",
                    //StackTrace = parts.Length > 3 ? string.Join('|', parts.Skip(3)) : "No stack trace"
                },
                _ => null
            };

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