using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BOHO.Application.Models;
using BOHO.Application.Util;
using BOHO.Core;
using BOHO.Core.Entities;
using BOHO.Core.Interfaces;
using Microsoft.Extensions.Logging;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;

namespace BOHO.Client
{
    /// <summary>
    /// The ViewItemWpfUserControl is the WPF version of the ViewItemUserControl. It is instantiated for every position it is created on the current visible view. When a user select another View or ViewLayout, this class will be disposed.  No permanent settings can be saved in this class.
    /// The Init() method is called when the class is initiated and handle has been created for the UserControl. Please perform resource initialization in this method.
    /// <br>
    /// If Message communication is performed, register the MessageReceivers during the Init() method and UnRegister the receivers during the Close() method.
    /// <br>
    /// The Close() method can be used to Dispose resources in a controlled manor.
    /// <br>
    /// Mouse events not used by this control, should be passed on to the Smart Client by issuing the following methods:<br>
    /// FireClickEvent() for single click<br>
    ///	FireDoubleClickEvent() for double click<br>
    /// The single click will be interpreted by the Smart Client as a selection of the item, and the double click will be interpreted to expand the current viewitem to fill the entire View.
    /// </summary>
    public partial class BOHOViewItemWpfUserControl : ViewItemWpfUserControl
    {
        #region Component private class variables
        private class BoundingBoxOverlay(int trackingNumber, Guid id, DateTimeOffset timestamp)
        {
            public Guid Id { get; private set; } = id;
            public int TrackingNumber { get; private set; } = trackingNumber;
            public DateTimeOffset Timestamp { get; set; } = timestamp;
        }

        private static readonly TimeSpan _boundingBoxTimeout = TimeSpan.FromSeconds(1);
        private static readonly ShapesOverlayRenderParameters _boundingBoxOverlayRenderParams =
            new() { FollowDigitalZoom = true, ZOrder = 1 };
        private static readonly ShapesOverlayRenderParameters _ruleOverlayRenderParams =
            new() { FollowDigitalZoom = true, ZOrder = 2 };

        private readonly BOHOViewItemManager _viewItemManager;
        private readonly ILogger<BOHOViewItemWpfUserControl> _logger;
        private readonly IMessageService _messageService;
        private readonly IEventListener _eventListener;

        private readonly DispatcherTimer _timer = new() { Interval = _boundingBoxTimeout };
        private Guid _ruleOverlayId = Guid.Empty;
        private Dictionary<int, BoundingBoxOverlay> _boundingBoxOverlays = [];
        private readonly List<object> _messageRegisterObjects = [];
        private object _setDeviceMessageObject;

        private bool _boundingBoxVisible;
        private bool BoundingBoxVisible
        {
            get => _boundingBoxVisible;
            set
            {
                _boundingBoxVisible = value;
                _viewItemManager.SetProperty(nameof(BoundingBoxVisible), value.ToString());
                _viewItemManager.SaveAllProperties();
            }
        }

        private bool _ruleVisible;
        private bool RuleVisible
        {
            get => _ruleVisible;
            set
            {
                _ruleVisible = value;
                _viewItemManager.SetProperty(nameof(RuleVisible), value.ToString());
                _viewItemManager.SaveAllProperties();
            }
        }

        private bool _ruleNameVisible;
        private bool RuleNameVisible
        {
            get => _ruleNameVisible;
            set
            {
                _ruleNameVisible = value;
                _viewItemManager.SetProperty(nameof(RuleNameVisible), value.ToString());
                _viewItemManager.SaveAllProperties();
            }
        }

        private Device _selectedDevice;
        private Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                _viewItemManager.SetProperty(nameof(SelectedDevice), value.SerializeToJson());
                _viewItemManager.SaveAllProperties();
            }
        }

        private IEnumerable<Rule> _rules;
        private IEnumerable<Rule> Rules
        {
            get => _rules;
            set
            {
                _rules = value;
                _viewItemManager.SetProperty(nameof(Rules), value.SerializeToJson());
                _viewItemManager.SaveAllProperties();
            }
        }

        private Item _selectedCamera;
        private Item SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                _selectedCamera = value;
                _viewItemManager.SetProperty(
                    ClientControl.EmbeddedCameraFQIDProperty,
                    value.FQID.ToXmlNode().OuterXml
                );
            }
        }
        #endregion

        #region Component constructors + dispose

        /// <summary>
        /// Constructs a BOHOViewItemUserControl instance
        /// </summary>
        public BOHOViewItemWpfUserControl(
            ILogger<BOHOViewItemWpfUserControl> logger,
            IMessageService messageService,
            IEventListener eventListener,
            BOHOViewItemManager viewItemManager
        )
        {
            _logger = logger;
            _messageService = messageService;
            _eventListener = eventListener;
            _viewItemManager = viewItemManager;

            if (
                _viewItemManager.GetProperty(ClientControl.EmbeddedCameraFQIDProperty) is
                { } fqdiString
            )
            {
                _selectedCamera = Configuration.Instance.GetItem(new FQID(fqdiString));
            }

            _rules =
                _viewItemManager.GetProperty(nameof(Rules)).Deserialize<IEnumerable<Rule>>() ?? [];
            _selectedDevice = _viewItemManager
                .GetProperty(nameof(SelectedDevice))
                .Deserialize<Device>();
            _ruleNameVisible = _viewItemManager.GetProperty(nameof(RuleNameVisible)).ToBool();
            _ruleVisible = _viewItemManager.GetProperty(nameof(RuleVisible)).ToBool();
            _boundingBoxVisible = _viewItemManager.GetProperty(nameof(BoundingBoxVisible)).ToBool();

            InitializeComponent();
        }

        private void SetUpApplicationEventListeners()
        {
            _eventListener.EventReceived += OnEventReceived;
            _setDeviceMessageObject = EnvironmentManager.Instance.RegisterReceiver(
                OnDeviceChanged,
                new MessageIdFilter("/device")
            );
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void RemoveApplicationEventListeners()
        {
            _eventListener.EventReceived -= OnEventReceived;
            _messageRegisterObjects
                .Append(_setDeviceMessageObject)
                .ToList()
                .ForEach(EnvironmentManager.Instance.UnRegisterReceiver);
            _timer.Tick -= OnTick;
        }

        /// <summary>
        /// Method that is called immediately after the view item is displayed.
        /// </summary>
        public override void Init()
        {
            SetUpApplicationEventListeners();
            Update();
        }

        /// <summary>
        /// Method that is called when the view item is closed. The view item should free all resources when the method is called.
        /// Is called when userControl is not displayed anymore. Either because of
        /// user clicking on another View or Item has been removed from View.
        /// </summary>
        public override void Close()
        {
            RemoveApplicationEventListeners();
        }

        #endregion

        #region Print method

        /// <summary>
        /// Method that is called when print is activated while the content holder is selected.
        /// </summary>
        public override void Print()
        {
            Print("Name of this item", "Some extra information");
        }

        #endregion

        #region Component events

        private void ViewItemWpfUserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                FireClickEvent();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                FireRightClickEvent(e);
            }
        }

        private void ViewItemWpfUserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                FireDoubleClickEvent();
            }
        }

        /// <summary>
        /// Signals that the form is right clicked
        /// </summary>
        public event EventHandler RightClickEvent;

        /// <summary>
        /// Activates the RightClickEvent
        /// </summary>
        /// <param name="e">Event args</param>
        protected virtual void FireRightClickEvent(EventArgs e)
        {
            if (RightClickEvent != null)
            {
                RightClickEvent(this, e);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            List<Guid> outDatedOverlayIds = _boundingBoxOverlays
                .Values.Where(overlay =>
                    DateTimeOffset.Now.Subtract(overlay.Timestamp) > _timer.Interval
                )
                .Select(overlay => overlay.Id)
                .ToList();

            _boundingBoxOverlays = _boundingBoxOverlays
                .Values.Where(overlay => !outDatedOverlayIds.Contains(overlay.Id))
                .ToDictionary(overlay => overlay.TrackingNumber, overlay => overlay);

            Dispatcher.Invoke(() => outDatedOverlayIds.ForEach(_imageViewer.ShapesOverlayRemove));
        }

        private void OnEventReceived(object sender, BOHOEventArgs args)
        {
            if (this.SelectedDevice?.ID == args.DeviceId && BoundingBoxVisible)
            {
                Dispatcher.Invoke(() => DrawBoundingBoxes(args.BoundingBoxes));
            }
        }

        private object OnBoundingBoxVisibilityChanged(Message message, FQID sender, FQID related)
        {
            BoundingBoxVisible = (bool)message.Data;

            if (!BoundingBoxVisible)
            {
                Dispatcher.Invoke(RemoveBoundingBoxes);
            }

            return null;
        }

        private object OnRuleVisibilityChanged(Message message, FQID sender, FQID related)
        {
            RuleVisible = (bool)message.Data;

            if (RuleVisible)
            {
                Dispatcher.Invoke(ShowRules);
            }
            else
            {
                Dispatcher.Invoke(HideRules);
            }

            return null;
        }

        private object OnRuleNameVisibilityChanged(Message message, FQID sender, FQID related)
        {
            RuleNameVisible = (bool)message.Data;

            if (RuleVisible)
            {
                Dispatcher.Invoke(ShowRules);
            }
            else
            {
                Dispatcher.Invoke(HideRules);
            }

            return null;
        }

        private object OnDeviceChanged(Message message, FQID sender, FQID related)
        {
            List<Item> mipViewItems = WindowInformation.ViewAndLayoutItem.GetChildren();
            Item viewItemInstance = mipViewItems[int.Parse(_viewItemManager.FQID.ObjectIdString)];

            var data = message.Data as SetDeviceEventArgs;

            if (
                !data.ViewItemInstanceFQID.Equals(viewItemInstance.FQID)
                || !data.WindowFQID.Equals(WindowInformation.Window.FQID)
            )
            {
                return null;
            }

            static IEnumerable<Item> LoadCameras(Item item) =>
                item.FQID.Kind == Kind.Camera && item.FQID.ObjectId != Kind.Camera
                    ? [item]
                    : item.GetChildren().SelectMany(child => LoadCameras(child));

            IEnumerable<Item> cameraList = Configuration
                .Instance.GetItemsByKind(Kind.Camera)
                .SelectMany(item => LoadCameras(item));

            Item camera = cameraList.FirstOrDefault(x => x.FQID.Contains(data.Device.Guid));
            if (camera == default)
            {
                Dispatcher.Invoke(() =>
                {
                    _messageService.ShowError(
                        "Lỗi",
                        $"Không tìm thấy camera với GUID {data.Device.Guid}"
                    );
                });

                return null;
            }

            SelectedCamera = camera;
            SelectedDevice = data.Device;
            Rules = data.Rules;
            Update();

            return null;
        }

        private void RemoveBoundingBoxes()
        {
            foreach (Guid id in _boundingBoxOverlays.Values.Select(x => x.Id))
            {
                _imageViewer.ShapesOverlayRemove(id);
            }

            _boundingBoxOverlays.Clear();
        }

        private void DrawBoundingBoxes(IEnumerable<BoundingBox> boxes)
        {
            IEnumerable<Shape> BoundingBoxToShapes(BoundingBox box)
            {
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                Size imageSize = GetActualImageSize();

                double x = box.X * imageSize.Width;
                double y = box.Y * imageSize.Height;
                double width = box.Width * imageSize.Width;
                double height = box.Height * imageSize.Height;

                yield return new Path
                {
                    Data = new RectangleGeometry(
                        new Rect
                        {
                            X = x,
                            Y = y,
                            Width = width,
                            Height = height,
                        }
                    ),
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                };

                yield return ShapeUtil.FromText(
                    $"{box.ObjectName} - track id {box.TrackingNumber}",
                    new Point(x, y - 14),
                    pixelsPerDip
                );
            }

            foreach (BoundingBox box in boxes)
            {
                if (
                    _boundingBoxOverlays.TryGetValue(
                        box.TrackingNumber,
                        out BoundingBoxOverlay overlay
                    )
                )
                {
                    _imageViewer.ShapesOverlayUpdate(overlay.Id, BoundingBoxToShapes(box).ToList());
                    _boundingBoxOverlays[box.TrackingNumber] = new BoundingBoxOverlay(
                        box.TrackingNumber,
                        overlay.Id,
                        DateTimeOffset.Now
                    );
                }
                else
                {
                    Guid overlayId = _imageViewer.ShapesOverlayAdd(
                        BoundingBoxToShapes(box).ToList(),
                        _boundingBoxOverlayRenderParams
                    );
                    _boundingBoxOverlays[box.TrackingNumber] = new BoundingBoxOverlay(
                        box.TrackingNumber,
                        overlayId,
                        DateTimeOffset.Now
                    );
                }
            }
        }

        private void HideRules()
        {
            if (_ruleOverlayId != Guid.Empty)
            {
                _imageViewer.ShapesOverlayRemove(_ruleOverlayId);
                _ruleOverlayId = Guid.Empty;
            }
        }

        private void ShowRules()
        {
            HideRules();

            if (!Rules.Any())
            {
                return;
            }

            Size imageSize = GetActualImageSize();
            double scaleX = imageSize.Width / Constants.AI_IMAGE_WIDTH;
            double scaleY = imageSize.Height / Constants.AI_IMAGE_HEIGHT;

            IEnumerable<Shape> ConvertRuleToShapes(Rule rule)
            {
                yield return ShapeUtil.FromRule(rule, scaleX, scaleY);

                if (RuleNameVisible)
                {
                    var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                    yield return ShapeUtil.FromText(
                        rule.Name,
                        new Point
                        {
                            X = rule.Points[0][0] * scaleX,
                            Y = rule.Points[0][1] * scaleY,
                        },
                        pixelsPerDip
                    );
                }
            }

            _ruleOverlayId = _imageViewer.ShapesOverlayAdd(
                Rules.SelectMany(ConvertRuleToShapes).ToList(),
                _ruleOverlayRenderParams
            );
        }

        private void Update()
        {
            _messageRegisterObjects.ForEach(EnvironmentManager.Instance.UnRegisterReceiver);
            _messageRegisterObjects.Clear();

            if (SelectedDevice != null)
            {
                foreach (
                    var channel in new List<(string Name, MessageReceiver Handler)>
                    {
                        new("rule_visibility", OnRuleVisibilityChanged),
                        new("rule_name_visibility", OnRuleNameVisibilityChanged),
                        new("bounding_visibility", OnBoundingBoxVisibilityChanged),
                    }
                )
                {
                    string topic = $"/device/{SelectedDevice.ID}/{channel.Name}";
                    MessageIdFilter messageFiler = new(topic);
                    object _messageRegisterObject = EnvironmentManager.Instance.RegisterReceiver(
                        channel.Handler,
                        messageFiler
                    );
                    _messageRegisterObjects.Add(_messageRegisterObject);
                }
            }

            if (SelectedCamera != null)
            {
                _imageViewer.Disconnect();
                _imageViewer.CameraFQID = SelectedCamera.FQID;
                _imageViewer.EnableVisibleLiveIndicator = true;
                _imageViewer.EnableVisibleHeader = false;
                _imageViewer.MaintainImageAspectRatio = true;
                _imageViewer.EnableMouseControlledPtz = false;
                _imageViewer.EnableDigitalZoom = false;
                _imageViewer.Initialize();
                _imageViewer.Connect();
            }

            ShowRules();
        }

        private Size GetActualImageSize()
        {
            double aspectRatio =
                (double)_imageViewer.ImageSize.Width / (double)_imageViewer.ImageSize.Height;
            double actualWidth = _imageViewer.ActualWidth;
            double actualHeight = _imageViewer.ActualHeight;
            double actualAspectRatio = actualWidth / actualHeight;

            if (actualAspectRatio > aspectRatio)
            {
                actualWidth = actualHeight * aspectRatio;
            }
            else
            {
                actualHeight = actualWidth / aspectRatio;
            }

            return new Size(actualWidth, actualHeight);
        }
        #endregion

        #region Component properties

        /// <summary>
        /// Gets boolean indicating whether the view item can be maximized or not. <br/>
        /// The content holder should implement the click and double click events even if it is not maximizable.
        /// </summary>
        public override bool Maximizable
        {
            get { return true; }
        }

        /// <summary>
        /// Tell if ViewItem is selectable
        /// </summary>
        public override bool Selectable
        {
            get { return true; }
        }
        #endregion
    }
}
