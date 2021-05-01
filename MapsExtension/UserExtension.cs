using System.Collections.Generic;
using Windows.Foundation.Collections;
using MapsExtension.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapsExtension {
    public static class Actions {
        public const string Nop = "nop";
        public const string Message = "message";
        public const string AddPlugin = "addPlugin";
    }
    
    public static class UserExtension {
        public static readonly List<ToolbarItemModel> ToolbarItems = new List<ToolbarItemModel> {
            new ToolbarItemModel("Add Map", "Assets/map48.png", AddMapCallback),
        };

        private static ValueSet AddMapCallback(ValueSet message) {
            var callbackType = (string) message["callbackType"];
            var response = new ValueSet();
            switch (callbackType) {
                case "selected":
                    response.Add("action", Actions.Nop);
                    break;
                case "deselected":
                    response.Add("action", Actions.Nop);
                    break;
                case "touch":
                    var touchEvent = (JObject) JsonConvert.DeserializeObject((string) message["callbackExtra"]);
                    if (int.Parse((string) touchEvent["ActionType"]) == 3 /* 3 = Released */) {
                        response.Add("action", Actions.AddPlugin);
                        const string type = "Windows.UI.Xaml.Controls.Maps.MapControl";
                        const double w = 500.0;
                        const double h = 500.0;
                        var x = touchEvent["Location"]["X"];
                        var y= touchEvent["Location"]["Y"];
                        response.Add("actionExtra", $"{type}| |{x}|{y}|{w}|{h}");
                    } else {
                        response.Add("action", Actions.Nop);
                    }
                    break;
            }
            return response;
        }
    }
}