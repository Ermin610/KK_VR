using System;
using SpeechTransport;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Core;

namespace VRGIN.Controls.Speech;

public class VoiceShortcut : IShortcut, IDisposable
{
	private SpeechResult? _LastResult;

	private int _MinID;

	private Action _Action;

	private VoiceCommand _Command;

	public VoiceShortcut(VoiceCommand command, Action action)
	{
		_Action = action;
		_Command = command;
		if ((VR.Speech != null))
		{
			VR.Speech.SpeechRecognized += OnRecognized;
		}
	}

	private void OnRecognized(object sender, SpeechRecognizedEventArgs e)
	{
		if (e.Result.ID >= _MinID)
		{
			_LastResult = e.Result;
		}
	}

	public void Dispose()
	{
		if ((VR.Speech != null))
		{
			VR.Speech.SpeechRecognized -= OnRecognized;
		}
	}

	public void Evaluate()
	{
		if (_LastResult.HasValue && _Command.Matches(_LastResult.Value.Text) && (_LastResult.Value.Confidence > 0.20000000298023224 || _LastResult.Value.Final))
		{
			VRLog.Info(_Command);
			_Action();
			_MinID = _LastResult.Value.ID + 1;
		}
		_LastResult = null;
	}
}
