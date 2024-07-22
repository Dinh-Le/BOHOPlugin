using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BOHO.Application.Models;
using BOHO.Application.Util;
using BOHO.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private BOHOViewItemManager _viewItemManager;
        private readonly ILogger<BOHOViewItemWpfUserControl> _logger;
        private Core.EventListener _eventListener;
        private Guid? _boundingBoxShapesOverlayGuid;
        private Guid? _ruleShapesOverlayGuid;

        private DispatcherTimer _timer;
        private List<object> _messageRegisterObjects;
        private object _setDeviceMessageObject;
        private Core.Entities.BOHOEventArgs _eventMetadata;

        private bool _boundingBoxVisible;
        private bool BoundingBoxVisible
        {
            get => this._boundingBoxVisible;
            set
            {
                this._boundingBoxVisible = value;
                this._viewItemManager.SetProperty("bounding_box_visible", value.ToString());
            }
        }

        private bool _ruleVisible;
        private bool RuleVisible
        {
            get => this._ruleVisible;
            set
            {
                this._ruleVisible = value;
                this._viewItemManager.SetProperty("rule_visible", value.ToString());
            }
        }

        private bool _ruleNameVisible;
        private bool RuleNameVisible
        {
            get => this._ruleNameVisible;
            set => this._viewItemManager.SetProperty("rule_name_visible", value.ToString());
        }

        private Core.Entities.Device _selectedDevice;
        private Core.Entities.Device SelectedDevice
        {
            get => this._selectedDevice;
            set
            {
                this._selectedDevice = value;
                this._viewItemManager.SetProperty(
                    "selected_device",
                    JsonConvert.SerializeObject(value)
                );
            }
        }

        private IEnumerable<Core.Entities.Rule> _rules;
        private IEnumerable<Core.Entities.Rule> Rules
        {
            get => _rules;
            set
            {
                this._rules = value;
                this._viewItemManager.SetProperty("rules", JsonConvert.SerializeObject(value));
            }
        }

        private Item _selectedCamera;
        private Item SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                this._selectedCamera = value;
                this._viewItemManager.SetProperty(
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
        public BOHOViewItemWpfUserControl(BOHOViewItemManager viewItemManager)
        {
            this._viewItemManager = viewItemManager;
            this._eventListener = RootContainer.Get<EventListener>();
            this._logger = RootContainer.Get<ILogger<BOHOViewItemWpfUserControl>>();

            InitializeComponent();

            this._timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            this._messageRegisterObjects = [];

            if (
                _viewItemManager.GetProperty(ClientControl.EmbeddedCameraFQIDProperty) is
                { } fqdiString
            )
            {
                this._selectedCamera = Configuration.Instance.GetItem(new FQID(fqdiString));
            }
            this._rules = JsonConvert.DeserializeObject<IEnumerable<Core.Entities.Rule>>(
                this._viewItemManager.GetProperty("rules")
            );
            this._selectedDevice = JsonConvert.DeserializeObject<Core.Entities.Device>(
                this._viewItemManager.GetProperty("selected_device")
            );
            this._ruleNameVisible =
                bool.TryParse(
                    this._viewItemManager.GetProperty("rule_name_visible"),
                    out bool value
                ) && value;
            this._ruleVisible =
                bool.TryParse(this._viewItemManager.GetProperty("rule_visible"), out value)
                && value;
            this._boundingBoxVisible =
                bool.TryParse(this._viewItemManager.GetProperty("bouding_box_visible"), out value)
                && value;
        }

        private void SetUpApplicationEventListeners()
        {
            //set up ViewItem event listeners
            this._viewItemManager.PropertyChangedEvent += new EventHandler(
                ViewItemManagerPropertyChangedEvent
            );

            this._eventListener.EventReceived += OnEventReceived;
            this._timer.Tick += OnEventTimeout;
            this._setDeviceMessageObject = EnvironmentManager.Instance.RegisterReceiver(
                OnDeviceChanged,
                new MessageIdFilter("/device")
            );
            this._imageViewer.SizeChanged += new SizeChangedEventHandler(OnResize);
        }

        private void RemoveApplicationEventListeners()
        {
            //remove ViewItem event listeners
            _viewItemManager.PropertyChangedEvent -= new EventHandler(
                ViewItemManagerPropertyChangedEvent
            );
            this._eventListener.EventReceived -= OnEventReceived;
            this._timer.Tick -= OnEventTimeout;

            EnvironmentManager.Instance.UnRegisterReceiver(this._setDeviceMessageObject);
            this._messageRegisterObjects.ForEach(obj =>
                EnvironmentManager.Instance.UnRegisterReceiver(obj)
            );
        }

        /// <summary>
        /// Method that is called immediately after the view item is displayed.
        /// </summary>
        public override void Init()
        {
            SetUpApplicationEventListeners();

            this.Update();
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

        void ViewItemManagerPropertyChangedEvent(object sender, EventArgs e) { }

        private void OnResize(object sender, EventArgs args)
        {
            if (this._eventMetadata is not null)
            {
                RenderBoundingBox();
            }

            this.RenderRules();
        }

        private void OnEventReceived(object sender, Core.Entities.BOHOEventArgs eventMetadata)
        {
            if (!this.BoundingBoxVisible || this.SelectedDevice?.ID != eventMetadata.DeviceId)
            {
                return;
            }

            this._eventMetadata = eventMetadata;
            this._logger.LogInformation("Received an event: {@Event}", this._eventMetadata);
            this.RenderBoundingBox();
        }

        private void OnEventTimeout(object sender, EventArgs args)
        {
            this._imageViewer.ShapesOverlayRemove((Guid)this._boundingBoxShapesOverlayGuid);
            this._boundingBoxShapesOverlayGuid = null;
            this._eventMetadata = null;
            this._timer.Stop();
        }

        private object OnBoundingBoxVisibilityChanged(Message message, FQID sender, FQID related)
        {
            this.BoundingBoxVisible = (bool)message.Data;
            return null;
        }

        private object OnRuleVisibilityChanged(Message message, FQID sender, FQID related)
        {
            this.RuleVisible = (bool)message.Data;
            this.RenderRules();

            return null;
        }

        private object OnRuleNameVisibilityChanged(Message message, FQID sender, FQID related)
        {
            this.RuleNameVisible = (bool)message.Data;
            this.RenderRules();

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

            var cameraList = Configuration
                .Instance.GetItemsByKind(Kind.Camera)
                .SelectMany(item => LoadCameras(item));
            var camera = cameraList.FirstOrDefault(x => x.FQID.Contains(data.Device.Guid));
            if (camera == default(Item))
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Không tìm thấy camera với GUID {data.Device.Guid}",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                });
                return null;
            }

            this.SelectedCamera = camera;
            this.SelectedDevice = data.Device;
            this.Rules = data.Rules;

            this.Update();

            return null;
        }

        private List<Item> LoadCameras(Item item)
        {
            return item.FQID.Kind == Kind.Camera && item.FQID.ObjectId != Kind.Camera
                ? new List<Item> { item }
                : item.GetChildren().SelectMany(child => LoadCameras(child)).ToList();
        }

        private void RenderBoundingBox()
        {
            if (this._boundingBoxShapesOverlayGuid != null)
            {
                this._imageViewer.ShapesOverlayRemove((Guid)this._boundingBoxShapesOverlayGuid);
            }

            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var shapes = this
                ._eventMetadata.BoundingBoxes.SelectMany(box =>
                {
                    var x = box.X * this._imageViewer.ActualWidth;
                    var y = box.Y * this._imageViewer.ActualHeight;
                    var width = box.Width * this._imageViewer.ActualWidth;
                    var height = box.Height * this._imageViewer.ActualHeight;
                    var boundingBoxPath = new Path
                    {
                        Data = new RectangleGeometry(
                            new Rect
                            {
                                X = x,
                                Y = y,
                                Width = width,
                                Height = height
                            }
                        ),
                        Stroke = Brushes.Red,
                        StrokeThickness = 2
                    };
                    var textPath = ShapeUtil.FromText(
                        box.ObjectName,
                        new Point(x, y - 14),
                        pixelsPerDip
                    );
                    return new List<Shape> { boundingBoxPath, textPath };
                })
                .ToList();
            if (!shapes.Any())
            {
                return;
            }

            this._logger.LogDebug("Draw bounding boxes");
            var parameters = new ShapesOverlayRenderParameters
            {
                FollowDigitalZoom = true,
                ZOrder = 1
            };
            this._boundingBoxShapesOverlayGuid = this._imageViewer.ShapesOverlayAdd(
                shapes,
                parameters
            );

            this._timer.Stop();
            this._timer.Start();
        }

        private void RenderRules()
        {
            if (this._ruleShapesOverlayGuid is not null)
            {
                this._imageViewer.ShapesOverlayRemove((Guid)this._ruleShapesOverlayGuid);
                this._ruleShapesOverlayGuid = null;
            }

            if (this.RuleVisible)
            {
                var shapes = this
                    .Rules.SelectMany(rule =>
                    {
                        double scaleX = this._imageViewer.ActualWidth / Constants.AI_IMAGE_WIDTH;
                        double scaleY = this._imageViewer.ActualHeight / Constants.AI_IMAGE_HEIGHT;

                        var shapes = new List<Shape>();

                        var ruleShape = ShapeUtil.FromRule(rule, scaleX, scaleY);
                        shapes.Add(ruleShape);

                        if (this.RuleNameVisible)
                        {
                            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                            var ruleNameShape = ShapeUtil.FromText(
                                rule.Name,
                                new Point
                                {
                                    X = rule.Points[0][0] * scaleX,
                                    Y = rule.Points[0][1] * scaleY
                                },
                                pixelsPerDip
                            );
                            shapes.Add(ruleNameShape);
                        }

                        return shapes;
                    })
                    .ToList();

                this._ruleShapesOverlayGuid = this._imageViewer.ShapesOverlayAdd(
                    shapes,
                    new ShapesOverlayRenderParameters { FollowDigitalZoom = true, ZOrder = 2 }
                );
            }
        }

        private void Update()
        {
            this._messageRegisterObjects.ForEach(EnvironmentManager.Instance.UnRegisterReceiver);
            this._messageRegisterObjects.Clear();

            if (this.SelectedDevice != null)
            {
                foreach (
                    var channel in new List<(string Name, MessageReceiver Handler)>
                    {
                        new("rule_visibility", OnRuleVisibilityChanged),
                        new("rule_name_visibility", OnRuleNameVisibilityChanged),
                        new("bounding_visibility", OnBoundingBoxVisibilityChanged)
                    }
                )
                {
                    var topic = $"/device/{this.SelectedDevice.ID}/{channel.Name}";
                    var messageFiler = new MessageIdFilter(topic);
                    var _messageRegisterObject = EnvironmentManager.Instance.RegisterReceiver(
                        channel.Handler,
                        messageFiler
                    );
                    this._messageRegisterObjects.Add(_messageRegisterObject);
                }
            }

            if (this.SelectedCamera != null)
            {
                this._imageViewer.Disconnect();
                this._imageViewer.CameraFQID = this.SelectedCamera.FQID;
                this._imageViewer.EnableVisibleLiveIndicator = true;
                this._imageViewer.EnableVisibleHeader = true;
                this._imageViewer.MaintainImageAspectRatio = true;
                this._imageViewer.EnableMouseControlledPtz = true;
                this._imageViewer.EnableDigitalZoom = true;
                this._imageViewer.Initialize();
                this._imageViewer.Connect();
            }

            this.RenderRules();
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
