namespace Valve.VR;

public struct Compositor_CumulativeStats
{
	public uint m_nPid;

	public uint m_nNumFramePresents;

	public uint m_nNumDroppedFrames;

	public uint m_nNumReprojectedFrames;

	public uint m_nNumFramePresentsOnStartup;

	public uint m_nNumDroppedFramesOnStartup;

	public uint m_nNumReprojectedFramesOnStartup;

	public uint m_nNumLoading;

	public uint m_nNumFramePresentsLoading;

	public uint m_nNumDroppedFramesLoading;

	public uint m_nNumReprojectedFramesLoading;

	public uint m_nNumTimedOut;

	public uint m_nNumFramePresentsTimedOut;

	public uint m_nNumDroppedFramesTimedOut;

	public uint m_nNumReprojectedFramesTimedOut;
}
