using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Forms;
using IE = Interop.SHDocVw;
using mshtml;

namespace Billbank.IEPlugin {
    /// <summary>
    /// Add-in Express .NET for Internet Explorer Module
    /// </summary>
    [ComVisible(true), Guid("F9B61629-31E7-41A5-9963-2D9E9409BFBB")]
    public class IEModule : AddinExpress.IE.ADXIEModule {

        private bool initialized = false;

        public IEModule() {
            InitializeComponent();
        }

        internal AddinExpress.IE.ADXIEBarItem barItem;

        public IEModule(IContainer container) {
            container.Add(this);

            InitializeComponent();
        }

        #region Component Designer generated code
        /// <summary>
        /// Required by designer
        /// </summary>
        private System.ComponentModel.IContainer components;

        /// <summary>
        /// Required by designer support - do not modify
        /// the following method
        /// </summary>
        private void InitializeComponent() {
            this.OnConnect += new AddinExpress.IE.ADXIEConnect_EventHandler(IEModule_OnConnect);
            this.components = new System.ComponentModel.Container();
            this.barItem = new AddinExpress.IE.ADXIEBarItem(this.components);
            // 
            // barItem
            // 
            this.barItem.BarType = "Billbank.IEPlugin.SideBar";
            this.barItem.HelpText = String.Format("{0} {1}", Resources.DisplayPanel, Settings.Default.PluginTitle);
            this.barItem.MenuText = Settings.Default.PluginTooltip;
            this.barItem.Title = Settings.Default.PluginTitle;
            this.barItem.ToolButtonDefaultVisible = true;
            this.barItem.AddToolButton = true;
            this.barItem.LoadAtStartup = false;
            this.barItem.ToolButtonActiveIcon = "Icons.Plugin";
            this.barItem.ToolButtonInactiveIcon = "Icons.Plugin";
            this.barItem.MinSize = 190;
            // 
            // IEModule
            // 
            this.Bars.Add(this.barItem);
            this.ModuleName = Settings.Default.PluginTitle;
            this.DocumentComplete += new AddinExpress.IE.ADXIEDocumentComplete_EventHandler(IEModule_DocumentComplete);
        }

        private void IEModule_DocumentComplete(object pDisp, string url) {
            if (Util.IsFirstExecution()) {
                Util.ResetFirstExecutionFlag();
                Util.OpenUrl(IEApp, Settings.Default.FirstExecutionLink);
            }
        }

        private void IEModule_OnConnect(object sender, int threadId) {
            // reset value only during the first module initialization i.e. ONLY once
            if (initialized == false && GetModuleIndex() == 0 && GetModuleCount() == 1) {
                initialized = true;
                Util.SetCurrentPaymentInfo("");
                Util.SetUpdatedPaymentInfo("");
            }
        }

        #endregion

        #region ADX automatic code

        // Required by Add-in Express - do not modify
        // the methods within this region

        public override System.ComponentModel.IContainer GetContainer() {
            if (components == null)
                components = new System.ComponentModel.Container();
            return components;
        }

        [ComRegisterFunctionAttribute]
        public static void RegisterIEModule(Type t) {
            AddinExpress.IE.ADXIEModule.RegisterIEModuleInternal(t);
        }

        [ComUnregisterFunctionAttribute]
        public static void UnregisterIEModule(Type t) {
            AddinExpress.IE.ADXIEModule.UnregisterIEModuleInternal(t);
        }

        [ComVisible(true)]
        public class IECustomContextMenuCommands :
            AddinExpress.IE.ADXIEModule.ADXIEContextMenuCommandDispatcher {
        }

        [ComVisible(true)]
        public class IECustomCommands :
            AddinExpress.IE.ADXIEModule.ADXIECommandDispatcher {
        }

        #endregion

        public IE.WebBrowser IEApp {
            get {
                return (this.IEObj as IE.WebBrowser);
            }
        }

        public mshtml.HTMLDocument HTMLDocument {
            get {
                return (this.HTMLDocumentObj as mshtml.HTMLDocument);
            }
        }
    }
}