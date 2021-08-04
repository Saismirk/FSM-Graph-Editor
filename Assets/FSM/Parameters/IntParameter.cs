using UnityEngine;
[System.Serializable]
public class IntParameter : Parameter {
    public IntParameter() {
        value.type = ParameterValue.ParameterType.Int;
    }

    public override object GetValue() {
        return value.IntValue;
    }
    public override void SetValue(ParameterValue value) {
        this.value = value;
    }
}
