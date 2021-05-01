using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace MapsExtension.Models {
    public class ToolbarItemModel {
        public readonly string Name;
        public readonly string Icon;
        public readonly Func<ValueSet, ValueSet> Callback; 

        public ToolbarItemModel(string name, string icon, Func<ValueSet, ValueSet> callback) {
            Name = name;
            Icon = icon;
            Callback = callback;
        }
    }
}