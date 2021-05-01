using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MapsExtension.Helpers;

namespace MapsExtension {
    sealed partial class App {
        public App() {
            Suspending += OnSuspending;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            // Current.Exit();
        }

        private void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        private BackgroundTaskDeferral _appServiceDeferral;
        private AppServiceConnection _connection;
        private int _currentConnectionIndex;

        private static int _connectionIndex;
        private static readonly Dictionary<int, BackgroundTaskDeferral> AppServiceDeferrals = new Dictionary<int, BackgroundTaskDeferral>();
        
        private static readonly object Lock = new object();

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args) {
            base.OnBackgroundActivated(args);
            var taskInstance = args.TaskInstance;
            if (!(taskInstance.TriggerDetails is AppServiceTriggerDetails appService)) return;
            _appServiceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnAppServicesCanceled;
            _connection = appService.AppServiceConnection;
            _connection.RequestReceived += OnAppServiceRequestReceived;
            _connection.ServiceClosed += AppServiceConnection_ServiceClosed;

            lock (Lock) {
                _connection.AppServiceName = _connectionIndex.ToString();
                AppServiceDeferrals.Add(_connectionIndex, _appServiceDeferral);
                _connectionIndex++;
            }
        }

        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
            try {
                var appService = (AppServiceTriggerDetails) sender.TriggerDetails;
                _currentConnectionIndex = int.Parse(appService.AppServiceConnection.AppServiceName);
                _appServiceDeferral = AppServiceDeferrals[_currentConnectionIndex];
                AppServiceDeferrals.Remove(_currentConnectionIndex);

                if (_appServiceDeferral == null) return;
                _appServiceDeferral.Complete();
                _appServiceDeferral = null;
            } catch (Exception ex) {
                Debug.WriteLine("OnAppServicesCanceled error: " + ex.Message);
            }
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args) {
            try {
                _currentConnectionIndex = int.Parse(sender.AppServiceName);
                _appServiceDeferral = AppServiceDeferrals[_currentConnectionIndex];
                AppServiceDeferrals.Remove(_currentConnectionIndex);

                if (_appServiceDeferral == null) return;
                _appServiceDeferral.Complete();
                _appServiceDeferral = null;
            } catch (Exception ex) {
                Debug.WriteLine("AppServiceConnection_ServiceClosed error: " + ex.Message);
            }
        }
        
        private async void OnAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args) {
            var messageDeferral = args.GetDeferral();

            try {
                var message = args.Request.Message;
                var messageType = message["type"];

                var returnMessage = new ValueSet();
                switch (messageType) {
                    case "requestToolbarItems":
                        returnMessage.Add("count", UserExtension.ToolbarItems.Count.ToString(CultureInfo.InvariantCulture));
                        for (var i = 0; i < UserExtension.ToolbarItems.Count; i++) {
                            var toolbarItemModel = UserExtension.ToolbarItems[i];

                            var imageBytes = (await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///" + toolbarItemModel.Icon))).OpenReadAsync()).AsStream().ReadAllBytes();
                            returnMessage.Add(i + "-icon", JsonConvert.SerializeObject(imageBytes));
                            returnMessage.Add(i + "-name", toolbarItemModel.Name);
                            returnMessage.Add(i + "-callbackId", i.ToString());
                        }
                        break;
                    case "toolbarItemCallback":
                        var callbackId = int.Parse((string) message["callbackId"]);
                        returnMessage = UserExtension.ToolbarItems[callbackId].Callback(message);
                        break;
                }

                await args.Request.SendResponseAsync(returnMessage);
            } finally {
                messageDeferral.Complete();
            }
        }
    }
}