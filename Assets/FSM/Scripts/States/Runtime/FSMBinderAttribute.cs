using System.Collections.Generic;
using System;
using UnityEngine;
#if UNITY_EDITOR
#endif
namespace FSM {
    [AttributeUsage(AttributeTargets.Class)]
    public class FSMBinderAttribute : PropertyAttribute {
        public string MenuPath;
        public FSMBinderAttribute(string menuPath){
            MenuPath = menuPath;
        }

    }
}