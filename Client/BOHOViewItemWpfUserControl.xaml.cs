using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BOHO.Application.Util;
using BOHO.Core;
using BOHO.Core.Entities;
using BOHO.Core.Interfaces;
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
        private Core.EventListener _eventListener;
        private Guid? _currentShapesOverlayGuid;
        private Guid? _ruleShapesOverlayGuid;
        private Core.Entities.Device _selectedDevice;
        private DispatcherTimer _timer;
        private List<object> _messageRegisterObjects;
        private object _setDeviceMessageObject;

        #endregion

        #region Component constructors + dispose

        /// <summary>
        /// Constructs a BOHOViewItemUserControl instance
        /// </summary>
        public BOHOViewItemWpfUserControl(BOHOViewItemManager viewItemManager)
        {
            _viewItemManager = viewItemManager;

            InitializeComponent();

            this._eventListener = Core.RootContainer.Get<Core.EventListener>();
            this._timer = new DispatcherTimer();
            this._timer.Interval = TimeSpan.FromSeconds(3);
            this._messageRegisterObjects = new List<object>();
        }

        private void SetUpApplicationEventListeners()
        {
            //set up ViewItem event listeners
            _viewItemManager.PropertyChangedEvent += new EventHandler(
                ViewItemManagerPropertyChangedEvent
            );

            this._eventListener.EventReceived += OnEventReceived;
            this._timer.Tick += OnEventTimeout;
            this._setDeviceMessageObject = EnvironmentManager.Instance.RegisterReceiver(
                SetDeviceReceiver,
                new MessageIdFilter("SetDevice")
            );
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

        private void OnEventReceived(Core.Entities.BOHOEventData eventMetadata)
        {
            if (this._selectedDevice?.ID != eventMetadata.DeviceId)
            {
                return;
            }

            this._imageViewer.Dispatcher.Invoke(
                delegate
                {
                    if (this._currentShapesOverlayGuid != null)
                    {
                        this._imageViewer.ShapesOverlayRemove((Guid)this._currentShapesOverlayGuid);
                    }

                    var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                    var shapes = eventMetadata
                        .BoundingBoxes.SelectMany(box =>
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

                    var parameters = new ShapesOverlayRenderParameters
                    {
                        FollowDigitalZoom = true,
                        ZOrder = 1
                    };
                    this._currentShapesOverlayGuid = this._imageViewer.ShapesOverlayAdd(
                        shapes,
                        parameters
                    );

                    this._timer.Stop();
                    this._timer.Start();
                }
            );
        }

        private void OnEventTimeout(object sender, EventArgs args)
        {
            this._imageViewer.ShapesOverlayRemove((Guid)this._currentShapesOverlayGuid);
            this._currentShapesOverlayGuid = null;
            this._timer.Stop();
        }

        private object ChangeRuleVisibilityReceiver(
            VideoOS.Platform.Messaging.Message message,
            FQID sender,
            FQID related
        )
        {
            if (this._selectedDevice is null)
            {
                return null;
            }

            var ruleVisible = (bool)message.Data;
            if (ruleVisible)
            {
                Task.Run(async () =>
                    {
                        var bohoRepository = RootContainer.Get<IBOHORepository>();
                        var rules = await bohoRepository.GetRules(this._selectedDevice);

                        this._imageViewer.Dispatcher.Invoke(
                            delegate
                            {
                                var shapes = rules
                                    .Select(rule => ShapeUtil.FromRule(rule))
                                    .ToList();
                                this._ruleShapesOverlayGuid = this._imageViewer.ShapesOverlayAdd(
                                    shapes,
                                    new ShapesOverlayRenderParameters
                                    {
                                        FollowDigitalZoom = true,
                                        ZOrder = 1
                                    }
                                );
                            }
                        );
                    })
                    .Wait();
            }
            else if (this._ruleShapesOverlayGuid is not null)
            {
                this._imageViewer.ShapesOverlayRemove((Guid)this._ruleShapesOverlayGuid);
                this._ruleShapesOverlayGuid = null;
            }

            return null;
        }

        private object SetDeviceReceiver(
            VideoOS.Platform.Messaging.Message message,
            FQID sender,
            FQID related
        )
        {
            List<Item> mipViewItems = WindowInformation.ViewAndLayoutItem.GetChildren();
            Item viewItemInstance = mipViewItems[int.Parse(_viewItemManager.FQID.ObjectIdString)];

            var data = message.Data as SetDeviceMessage;
            if (data == null)
                return null;

            if (
                !data.ViewItemInstanceFQID.Equals(viewItemInstance.FQID)
                || !data.WindowFQID.Equals(WindowInformation.Window.FQID)
            )
            {
                return null;
            }

            if (this._selectedDevice?.ID != data.Device.ID)
            {
                this._messageRegisterObjects.ForEach(
                    EnvironmentManager.Instance.UnRegisterReceiver
                );
                this._messageRegisterObjects.Clear();

                var messageFiler = new MessageIdFilter(
                    $"/device/{this._selectedDevice.ID}/rule_visibility"
                );
                var _messageRegisterObject = EnvironmentManager.Instance.RegisterReceiver(
                    ChangeRuleVisibilityReceiver,
                    messageFiler
                );
                this._messageRegisterObjects.Add(_messageRegisterObject);
            }

            this._selectedDevice = data.Device;
            var cameraList = Configuration
                .Instance.GetItemsByKind(Kind.Camera)
                .SelectMany(item => LoadCameras(item));
            var camera = cameraList.FirstOrDefault(x => x.FQID.Contains(this._selectedDevice.Guid));
            if (camera == default(Item))
            {
                System.Windows.MessageBox.Show(
                    $"Không tìm thấy camera với GUID {this._selectedDevice.Guid}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                this._selectedDevice = null;
                return null;
            }

            this._imageViewer.Dispatcher.Invoke(
                delegate
                {
                    this._imageViewer.Disconnect();

                    this._imageViewer.CameraFQID = camera.FQID;
                    this._imageViewer.EnableVisibleLiveIndicator = true;
                    this._imageViewer.EnableVisibleHeader = true;
                    this._imageViewer.MaintainImageAspectRatio = true;
                    this._imageViewer.EnableMouseControlledPtz = false;
                    this._imageViewer.EnableDigitalZoom = false;
                    this._imageViewer.Initialize();
                    this._imageViewer.Connect();
                }
            );

            return null;
        }

        private List<Item> LoadCameras(Item item)
        {
            return item.FQID.Kind == Kind.Camera && item.FQID.ObjectId != Kind.Camera
                ? new List<Item> { item }
                : item.GetChildren().SelectMany(child => LoadCameras(child)).ToList();
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
