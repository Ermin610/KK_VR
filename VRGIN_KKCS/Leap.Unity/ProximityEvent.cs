using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Events;

namespace Leap.Unity;

[Serializable]
public class ProximityEvent : UnityEvent<GameObject>
{
}
