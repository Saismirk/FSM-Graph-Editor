using UnityEngine;
[System.Serializable]
public class FloatParameter : Parameter {
    public FloatParameter() {
        value.type = ParameterValue.ParameterType.Float;
    }
    public override object GetValue() {
        return value.FloatValue;
    }
    public override void SetValue(ParameterValue value) {
        this.value = value;
    }
}
