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
        private readonly BOHOViewItemManager _viewItemManager;
        private readonly ILogger<BOHOViewItemWpfUserControl> _logger;
        private readonly IMessageService _messageService;
        private readonly IEventListener _eventListener;

        private static class ShapesOverlayKey
        {
            public const string BoundingBox = "BoundingBox";
            public const string Rule = "Rule";
        }

        private readonly Dictionary<string, Guid> _shapesOverlayIds = [];

        private List<object> _messageRegisterObjects;
        private object _setDeviceMessageObject;

        private BOHOEventArgs _eventMetadata;

        private bool _boundingBoxVisible;
        private bool BoundingBoxVisible
        {
            get => this._boundingBoxVisible;
            set
            {
                this._boundingBoxVisible = value;
                this._viewItemManager.SetProperty(nameof(BoundingBoxVisible), value.ToString());
                this._viewItemManager.SaveAllProperties();
            }
        }

        private bool _ruleVisible;
        private bool RuleVisible
        {
            get => this._ruleVisible;
            set
            {
                this._ruleVisible = value;
                this._viewItemManager.SetProperty(nameof(RuleVisible), value.ToString());
                this._viewItemManager.SaveAllProperties();
            }
        }

        private bool _ruleNameVisible;
        private bool RuleNameVisible
        {
            get => this._ruleNameVisible;
            set
            {
                this._ruleNameVisible = value;
                this._viewItemManager.SetProperty(nameof(RuleNameVisible), value.ToString());
                this._viewItemManager.SaveAllProperties();
            }
        }

        private Device _selectedDevice;
        private Device SelectedDevice
        {
            get => this._selectedDevice;
            set
            {
                this._selectedDevice = value;
                this._viewItemManager.SetProperty(nameof(SelectedDevice), value.SerializeToJson());
                this._viewItemManager.SaveAllProperties();
            }
        }

        private IEnumerable<Rule> _rules;
        private IEnumerable<Rule> Rules
        {
            get => _rules;
            set
            {
                this._rules = value;
                this._viewItemManager.SetProperty(nameof(Rules), value.SerializeToJson());
                this._viewItemManager.SaveAllProperties();
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
        public BOHOViewItemWpfUserControl(
            ILogger<BOHOViewItemWpfUserControl> logger,
            IMessageService messageService,
            IEventListener eventListener,
            BOHOViewItemManager viewItemManager
        )
        {
            this._logger = logger;
            this._messageService = messageService;
            this._eventListener = eventListener;
            this._viewItemManager = viewItemManager;

            if (
                _viewItemManager.GetProperty(ClientControl.EmbeddedCameraFQIDProperty) is
                { } fqdiString
            )
            {
                this._selectedCamera = Configuration.Instance.GetItem(new FQID(fqdiString));
            }

            this._rules = this
                ._viewItemManager.GetProperty(nameof(Rules))
                .Deserialize<IEnumerable<Rule>>();
            this._selectedDevice = this
                ._viewItemManager.GetProperty(nameof(SelectedDevice))
                .Deserialize<Device>();
            this._ruleNameVisible = this
                ._viewItemManager.GetProperty(nameof(RuleNameVisible))
                .ToBool();
            this._ruleVisible = this._viewItemManager.GetProperty(nameof(RuleVisible)).ToBool();
            this._boundingBoxVisible = this
                ._viewItemManager.GetProperty(nameof(BoundingBoxVisible))
                .ToBool();

            this.InitializeComponent();
        }

        private void SetUpApplicationEventListeners()
        {
            this._eventListener.EventReceived += OnEventReceived;

            this._setDeviceMessageObject = EnvironmentManager.Instance.RegisterReceiver(
                OnDeviceChanged,
                new MessageIdFilter("/device")
            );
        }

        private void RemoveApplicationEventListeners()
        {
            this._eventListener.EventReceived -= OnEventReceived;

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
            this.SetUpApplicationEventListeners();
            this.Update();
        }

        /// <summary>
        /// Method that is called when the view item is closed. The view item should free all resources when the method is called.
        /// Is called when userControl is not displayed anymore. Either because of
        /// user clicking on another View or Item has been removed from View.
        /// </summary>
        public override void Close()
        {
            this.RemoveApplicationEventListeners();
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

        private void OnEventReceived(object sender, BOHOEventArgs args)
        {
            //if (this.SelectedDevice?.ID != eventMetadata.DeviceId)
            //{
            //    return;
            //}

            this._eventMetadata = args;
            this.DrawBoundingBoxes();
        }

        private object OnBoundingBoxVisibilityChanged(Message message, FQID sender, FQID related)
        {
            this.BoundingBoxVisible = (bool)message.Data;
            this.DrawBoundingBoxes();

            return null;
        }

        private object OnRuleVisibilityChanged(Message message, FQID sender, FQID related)
        {
            this.RuleVisible = (bool)message.Data;
            this.DrawRules();

            return null;
        }

        private object OnRuleNameVisibilityChanged(Message message, FQID sender, FQID related)
        {
            this.RuleNameVisible = (bool)message.Data;
            this.DrawRules();

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
                    this._messageService.ShowError(
                        "Lỗi",
                        $"Không tìm thấy camera với GUID {data.Device.Guid}"
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

        private void DrawBoundingBoxes()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (this._shapesOverlayIds.TryGetValue(ShapesOverlayKey.BoundingBox, out Guid id))
                {
                    this._imageViewer.ShapesOverlayRemove(id);
                    this._shapesOverlayIds.Remove(ShapesOverlayKey.BoundingBox);
                }

                //if (!this.BoundingBoxVisible)
                //{
                //    return;
                //}

                IEnumerable<Shape> BoundingBoxToShapes(BoundingBoxInfo box)
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
                                Height = height
                            }
                        ),
                        Stroke = Brushes.Red,
                        StrokeThickness = 1
                    };

                    yield return ShapeUtil.FromText(
                        $"{box.ObjectName}",
                        new Point(x, y - 14),
                        pixelsPerDip
                    );
                }

                IEnumerable<Shape> shapes = this._eventMetadata.BoundingBoxes.SelectMany(
                    BoundingBoxToShapes
                );
                if (shapes.Any())
                {
                    ShapesOverlayRenderParameters parameters =
                        new() { FollowDigitalZoom = true, ZOrder = 1 };

                    this._shapesOverlayIds[ShapesOverlayKey.BoundingBox] =
                        this._imageViewer.ShapesOverlayAdd(shapes.ToList(), parameters);
                }
            });
        }

        private void DrawRules()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (this._shapesOverlayIds.TryGetValue(ShapesOverlayKey.Rule, out Guid id))
                {
                    this._imageViewer.ShapesOverlayRemove(id);
                    this._shapesOverlayIds.Remove(ShapesOverlayKey.Rule);
                }

                if (!this.RuleVisible)
                {
                    return;
                }

                Size imageSize = GetActualImageSize();
                double scaleX = imageSize.Width / Constants.AI_IMAGE_WIDTH;
                double scaleY = imageSize.Height / Constants.AI_IMAGE_HEIGHT;

                IEnumerable<Shape> ConvertRuleToShapes(Rule rule)
                {
                    yield return ShapeUtil.FromRule(rule, scaleX, scaleY);

                    if (this.RuleNameVisible)
                    {
                        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                        yield return ShapeUtil.FromText(
                            rule.Name,
                            new Point
                            {
                                X = rule.Points[0][0] * scaleX,
                                Y = rule.Points[0][1] * scaleY
                            },
                            pixelsPerDip
                        );
                    }
                }

                IEnumerable<Shape> shapes = this.Rules.SelectMany(ConvertRuleToShapes);
                if (!shapes.Any())
                {
                    return;
                }

                ShapesOverlayRenderParameters renderParameters =
                    new() { FollowDigitalZoom = true, ZOrder = 2 };
                this._shapesOverlayIds[ShapesOverlayKey.Rule] = this._imageViewer.ShapesOverlayAdd(
                    shapes.ToList(),
                    renderParameters
                );
            });
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
                    string topic = $"/device/{this.SelectedDevice.ID}/{channel.Name}";
                    MessageIdFilter messageFiler = new(topic);
                    object _messageRegisterObject = EnvironmentManager.Instance.RegisterReceiver(
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
                this._imageViewer.EnableVisibleHeader = false;
                this._imageViewer.MaintainImageAspectRatio = true;
                this._imageViewer.EnableMouseControlledPtz = false;
                this._imageViewer.EnableDigitalZoom = false;
                this._imageViewer.Initialize();
                this._imageViewer.Connect();
            }

            this.DrawRules();
        }

        private Size GetActualImageSize()
        {
            double aspectRatio =
                (double)this._imageViewer.ImageSize.Width
                / (double)this._imageViewer.ImageSize.Height;
            double actualWidth = this._imageViewer.ActualWidth;
            double actualHeight = this._imageViewer.ActualHeight;
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
