using System;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity;

[Serializable]
public class ProximityEvent : UnityEvent<GameObject>
{
}
