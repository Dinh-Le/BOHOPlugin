using System;
using System.Collections.Generic;
using BOHO.Core;
using BOHO.Core.Interfaces;
using Microsoft.Extensions.Logging;
using VideoOS.Platform;
using VideoOS.Platform.Client;

namespace BOHO.Client
{
    /// <summary>
    /// The ViewItemManager contains the configuration for the ViewItem. <br/>
    /// When the class is initiated it will automatically recreate relevant ViewItem configuration saved in the properties collection from earlier.
    /// Also, when the viewlayout is saved the ViewItemManager will supply current configuration to the SmartClient to be saved on the server.<br/>
    /// This class is only relevant when executing in the Smart Client.
    /// </summary>
    public class BOHOViewItemManager : ViewItemManager
    {
        private Guid _someid;
        private string _someName;
        private List<Item> _configItems;

        public BOHOViewItemManager()
            : base("BOHOViewItemManager") { }

        #region Methods overridden
        /// <summary>
        /// The properties for this ViewItem is now loaded into the base class and can be accessed via
        /// GetProperty(key) and SetProperty(key,value) methods
        /// </summary>
        public override void PropertiesLoaded()
        {
            String someid = GetProperty("SelectedGUID");
            _configItems = Configuration.Instance.GetItemConfigurations(
                BOHODefinition.BOHOPluginId,
                null,
                BOHODefinition.BOHOKind
            );
            if (someid != null && _configItems != null)
            {
                SomeId = new Guid(someid); // Set as last selected
            }
        }

        ///// <summary>
        ///// Generate the UserControl containing the actual ViewItem Content.
        /////
        ///// For new plugins it is recommended to use GenerateViewItemWpfUserControl() instead. Only implement this one if support for Smart Clients older than 2017 R3 is needed.
        ///// </summary>
        ///// <returns></returns>
        //public override ViewItemUserControl GenerateViewItemUserControl()
        //{
        //	return new BOHOViewItemUserControl(this);
        //}

        /// <summary>
        /// Generate the UserControl containing the actual ViewItem Content.
        /// </summary>
        /// <returns></returns>
        public override ViewItemWpfUserControl GenerateViewItemWpfUserControl()
        {
            ILogger<BOHOViewItemWpfUserControl> logger = RootContainer.Get<
                ILogger<BOHOViewItemWpfUserControl>
            >();
            IMessageService messageService = RootContainer.Get<IMessageService>();
            IEventListener eventListener = RootContainer.Get<IEventListener>();

            return new BOHOViewItemWpfUserControl(logger, messageService, eventListener, this);
        }

        ///// <summary>
        ///// Generate the UserControl containing the property configuration.
        /////
        ///// For new plugins it is recommended to use GeneratePropertiesWpfUserControl() instead. Only implement this one if support for Smart Clients older than 2017 R3 is needed.
        ///// </summary>
        ///// <returns></returns>
        //public override PropertiesUserControl GeneratePropertiesUserControl()
        //{
        //	return new BOHOPropertiesUserControl(this);
        //}

        /// <summary>
        /// Generate the UserControl containing the property configuration.
        /// </summary>
        /// <returns></returns>
        public override PropertiesWpfUserControl GeneratePropertiesWpfUserControl()
        {
            return new BOHOPropertiesWpfUserControl(this);
        }

        internal void SaveAllProperties()
        {
            SaveProperties();
        }

        #endregion

        public List<Item> ConfigItems
        {
            get { return _configItems; }
        }

        public Guid SomeId
        {
            get { return _someid; }
            set
            {
                _someid = value;
                SetProperty("SelectedGUID", _someid.ToString());
                if (_configItems != null)
                {
                    foreach (Item item in _configItems)
                    {
                        if (item.FQID.ObjectId == _someid)
                        {
                            SomeName = item.Name;
                        }
                    }
                }
                SaveProperties();
            }
        }

        public String SomeName
        {
            get { return _someName; }
            set { _someName = value; }
        }
    }
}
