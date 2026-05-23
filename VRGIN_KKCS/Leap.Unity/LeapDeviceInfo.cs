namespace Leap.Unity;

public struct LeapDeviceInfo
{
	public LeapDeviceType type;

	public bool isEmbedded;

	public float baseline;

	public float focalPlaneOffset;

	public float horizontalViewAngle;

	public float verticalViewAngle;

	public float trackingRange;

	public string serialID;

	public LeapDeviceInfo(LeapDeviceType initialization = LeapDeviceType.Invalid)
	{
		type = initialization;
		switch (type)
		{
		case LeapDeviceType.Peripheral:
			isEmbedded = false;
			baseline = 0.04f;
			focalPlaneOffset = 0.07f;
			horizontalViewAngle = 132.00002f;
			verticalViewAngle = 115.00002f;
			trackingRange = 0.47f;
			serialID = "";
			break;
		case LeapDeviceType.Dragonfly:
			isEmbedded = false;
			baseline = 0.064f;
			focalPlaneOffset = 0.08f;
			horizontalViewAngle = 132.00002f;
			verticalViewAngle = 115.00002f;
			trackingRange = 0.47f;
			serialID = "";
			break;
		default:
			isEmbedded = false;
			baseline = 0f;
			focalPlaneOffset = 0f;
			horizontalViewAngle = 0f;
			verticalViewAngle = 0f;
			trackingRange = 0f;
			serialID = "";
			break;
		}
	}
}
