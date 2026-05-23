using UnityEngine;

namespace Leap.Unity;

public class HandFader : MonoBehaviour
{
	public float confidenceSmoothing = 10f;

	public AnimationCurve confidenceCurve;

	protected HandModel _handModel;

	protected float _smoothedConfidence;

	protected Renderer _renderer;

	protected MaterialPropertyBlock _fadePropertyBlock;

	private const float EPISLON = 0.005f;

	protected virtual float GetUnsmoothedConfidence()
	{
		return _handModel.GetLeapHand().Confidence;
	}

	protected virtual void Awake()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		_handModel = ((Component)this).GetComponent<HandModel>();
		_renderer = ((Component)this).GetComponentInChildren<Renderer>();
		_fadePropertyBlock = new MaterialPropertyBlock();
		_renderer.GetPropertyBlock(_fadePropertyBlock);
		_fadePropertyBlock.SetFloat("_Fade", 0f);
		_renderer.SetPropertyBlock(_fadePropertyBlock);
	}

	protected virtual void Update()
	{
		float unsmoothedConfidence = GetUnsmoothedConfidence();
		_smoothedConfidence += (unsmoothedConfidence - _smoothedConfidence) / confidenceSmoothing;
		float num = confidenceCurve.Evaluate(_smoothedConfidence);
		_renderer.enabled = num > 0.005f;
		_renderer.GetPropertyBlock(_fadePropertyBlock);
		_fadePropertyBlock.SetFloat("_Fade", confidenceCurve.Evaluate(_smoothedConfidence));
		_renderer.SetPropertyBlock(_fadePropertyBlock);
	}
}
