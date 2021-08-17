using UnityEngine;
[System.Serializable]
public class BoolParameter : Parameter {
    public BoolParameter() {
        value.type = ParameterValue.ParameterType.Bool;
    }
    public override object GetValue() {
        return value.BoolValue;
    }
    public override void SetValue(ParameterValue value) {
        this.value = value;
    }
}