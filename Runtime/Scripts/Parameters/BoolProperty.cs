using System;
namespace FSM {
    [Serializable]
    public class BoolProperty : ExposedProperty {
        public BoolProperty() {
            value.type = PropertyValue.PropertyType.Bool;
        }
        public override object GetValue() => value.BoolValue;
        public void SetValue(bool value) {
            this.value.BoolValue = value;
        }
    }
}